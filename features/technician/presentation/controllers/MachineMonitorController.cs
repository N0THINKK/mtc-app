using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;

namespace mtc_app.features.technician.presentation.controllers
{
    public class MachineMonitorController
    {
        private readonly IMachineMonitorView _view;
        private readonly IMachineMonitorRepository _repository;
        private bool _isLoading = false;

        private readonly double[] _effectiveHours = new double[]
        {
            1.00, // Index 0
            8.0 / 9.0 * 1, // Hour 1
            8.0 / 9.0 * 2, // Hour 2
            8.0 / 9.0 * 3, // Hour 3
            8.0 / 9.0 * 4, // Hour 4
            8.0 / 9.0 * 5, // Hour 5
            8.0 / 9.0 * 6, // Hour 6
            8.0 / 9.0 * 7, // Hour 7
            8.0 / 9.0 * 8, // Hour 8
            8.00, // Hour 9  (End of regular shift)
            8.00 + (1.75 / 3.0 * 1), // Hour 10 (Overtime 1)
            8.00 + (1.75 / 3.0 * 2), // Hour 11 (Overtime 2)
            9.75  // Hour 12 (Overtime 3)
        };

        public MachineMonitorController(IMachineMonitorView view, IMachineMonitorRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            _view.SetLoadingState(true);

            try
            {
                string selectedArea = _view.SelectedArea;
                int maxShiftHours = 12;

                DateTime shiftEnd;
                bool isPastShift;
                string namaShift;
                DateTime shiftStart = GetShiftTimeRange(out shiftEnd, out isPastShift, out namaShift);

                // --- 1. Get Shift Breaks ---
                string dbShift = namaShift == "Shift Pagi" ? "Shift 1" : "Shift 2";
                int dayId = (int)shiftStart.DayOfWeek;
                if (dayId == 0) dayId = 7;
                var breakDto = await _repository.GetShiftBreaksAsync(dbShift, shiftStart, dayId);
                
                int currentHourCountTemp = isPastShift ? 12 : Math.Max(1, (int)(DateTime.Now - shiftStart).TotalHours + 1);
                int maxBreakMinutes = currentHourCountTemp > 9
                    ? (breakDto.NonOtMinutes + breakDto.OtMinutes)
                    : breakDto.NonOtMinutes;

                // --- 2. Get Machine List ---
                var machineList = (await _repository.GetMachineListAsync(selectedArea)).ToList();
                var machines = machineList.ToDictionary(m => m.MachineId);
                
                var hourFirst = new Dictionary<int, long[]>();
                var hourLast = new Dictionary<int, long[]>();
                var hourMax = new Dictionary<int, long[]>();
                
                foreach(var mId in machines.Keys)
                {
                    hourFirst[mId] = new long[maxShiftHours];
                    hourLast[mId] = new long[maxShiftHours];
                    hourMax[mId] = new long[maxShiftHours];
                    for (int i = 0; i < maxShiftHours; i++)
                    {
                        hourFirst[mId][i] = -1;
                        hourLast[mId][i] = -1;
                        hourMax[mId][i] = -1;
                    }
                }

                // --- 3. Get Process Logs ---
                var machineIds = machines.Keys.ToList();
                bool useBgCache = _view.IsBackgroundCacheReady(shiftStart, shiftEnd);
                
                IEnumerable<MachineProcessLogAggregateDto> logRows;
                if (useBgCache)
                {
                    var cache = _view.GetBackgroundCache();
                    logRows = machineIds
                        .Where(id => cache.ContainsKey(id))
                        .SelectMany(id => cache[id]);
                }
                else
                {
                    logRows = await _repository.GetProcessLogsAsync(shiftStart, shiftEnd, machineIds);
                }

                foreach (var row in logRows)
                {
                    int mId = row.MachineId;
                    if (!machines.ContainsKey(mId)) continue;

                    int hIndex = row.HourIndex;
                    if (hIndex < 0 || hIndex >= maxShiftHours) continue;

                    hourFirst[mId][hIndex] = row.FirstPieces;
                    hourLast[mId][hIndex] = row.LastPieces;
                    hourMax[mId][hIndex] = row.MaxPieces;

                    machines[mId].AutoTime = Math.Max(machines[mId].AutoTime, row.MaxAuto);
                    machines[mId].MonitorTime = Math.Max(machines[mId].MonitorTime, row.MaxMonitor);
                }

                // --- 4. Get Targets ---
                var targets = await _repository.GetMachineTargetsAsync();
                foreach (var m in machines.Values)
                {
                    if (targets.TryGetValue(m.MachineId, out int t))
                    {
                        m.TargetPerHour = t;
                    }
                }

                // --- 5. Get Downtime ---
                IEnumerable<MachineDowntimeDto> psData;
                if (useBgCache && _view.GetBackgroundDowntimeCache() != null)
                {
                    var downtimeCache = _view.GetBackgroundDowntimeCache();
                    psData = machineIds
                        .Where(id => downtimeCache.ContainsKey(id))
                        .SelectMany(id => downtimeCache[id]);
                }
                else
                {
                    psData = await _repository.GetMachineDowntimeAsync(shiftStart, shiftEnd, machineIds);
                }

                foreach (var row in psData)
                {
                    if (machines.TryGetValue(row.MachineId, out var machine))
                    {
                        machine.PlannedStopMinutes = row.PlannedMin;
                        machine.SuddenStopMinutes = row.SuddenMin;
                    }
                }

                // --- Background Preload ---
                if (!useBgCache)
                {
                    _view.PreloadAllAreasBackground(shiftStart, shiftEnd);
                }

                // --- Calculation Post-Processing ---
                int currentHourCount = isPastShift ? maxShiftHours : Math.Max(1, Math.Min(maxShiftHours, (int)(DateTime.Now - shiftStart).TotalHours + 1));

                foreach (var machine in machines.Values)
                {
                    int mId = machine.MachineId;
                    long totalPiecesShiftIni = 0;
                    int firstActiveHour = -1;
                    int lastActiveHour = -1;

                    for (int i = 0; i < maxShiftHours; i++)
                    {
                        if (hourFirst[mId][i] == -1 || i >= currentHourCount)
                        {
                            machine.HourlyPieces[i] = 0;
                            continue;
                        }

                        long first = hourFirst[mId][i];
                        long last = hourLast[mId][i];
                        long max = hourMax[mId][i];
                        long production = last >= first ? (last - first) : ((max - first) + last);

                        machine.HourlyPieces[i] = production;
                        totalPiecesShiftIni += production;

                        if (production > 0)
                        {
                            if (firstActiveHour == -1) firstActiveHour = i + 1;
                            lastActiveHour = i + 1;
                        }
                    }

                    machine.TotalPieces = totalPiecesShiftIni;
                    bool isOvertime = lastActiveHour >= 10;
                    int activeEndHour = isOvertime ? currentHourCount : Math.Min(currentHourCount, 9);
                    
                    int divisorIndex = firstActiveHour != -1 ? (activeEndHour - firstActiveHour + 1) : activeEndHour;
                    divisorIndex = Math.Max(0, Math.Min(12, divisorIndex));

                    double effectiveDivisor = _effectiveHours[divisorIndex];
                    machine.AveragePerHour = (double)totalPiecesShiftIni / effectiveDivisor;
                }

                // --- Filtering & Sorting ---
                string selectedMetric = _view.SelectedMetric;
                string selectedSort = _view.SelectedSort;
                
                var finalMachines = machines.Values.Where(m => m.TargetPerHour > 0).ToList();

                if (selectedSort == "Nomor Mesin")
                {
                    finalMachines = finalMachines.OrderBy(x => x.MachineName).ToList();
                }
                else
                {
                    bool sortAscending = selectedSort == "↑ Terendah";
                    if (selectedMetric.Contains("Efisiensi"))
                    {
                        finalMachines = sortAscending 
                            ? finalMachines.OrderBy(x => x.Efficiency).ToList()
                            : finalMachines.OrderByDescending(x => x.Efficiency).ToList();
                    }
                    else
                    {
                        finalMachines = sortAscending 
                            ? finalMachines.OrderBy(x => x.TotalPieces).ToList()
                            : finalMachines.OrderByDescending(x => x.TotalPieces).ToList();
                    }
                }

                _view.UpdateChart(finalMachines, selectedMetric, currentHourCount, maxBreakMinutes);

                string stateText = isPastShift ? "Selesai" : $"Berjalan: Jam ke-{currentHourCount}";
                _view.UpdateStatus($"Update: {DateTime.Now:HH:mm:ss} | {namaShift} ({shiftStart:dd MMM yyyy}) | {stateText}");
            }
            catch (Exception ex)
            {
                _view.UpdateStatus($"Error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                _view.SetLoadingState(false);
            }
        }

        private DateTime GetShiftTimeRange(out DateTime shiftEnd, out bool isPastShift, out string shiftName)
        {
            DateTime now = DateTime.Now;
            DateTime selectedDate = _view.SelectedDate.Date;
            bool isAuto = _view.SelectedShiftIndex == 0;
            bool isPagi = _view.SelectedShiftIndex == 1;

            DateTime shiftStart;

            if (isAuto)
            {
                isPastShift = false;
                if (now.Hour >= 7 && now.Hour < 19)
                {
                    shiftName = "Shift Pagi";
                    shiftStart = now.Date.AddHours(7);
                }
                else if (now.Hour >= 19)
                {
                    shiftName = "Shift Malam";
                    shiftStart = now.Date.AddHours(19);
                }
                else
                {
                    shiftName = "Shift Malam";
                    shiftStart = now.Date.AddDays(-1).AddHours(19);
                }
            }
            else
            {
                if (isPagi)
                {
                    shiftName = "Shift Pagi";
                    shiftStart = selectedDate.AddHours(7);
                }
                else
                {
                    shiftName = "Shift Malam";
                    shiftStart = selectedDate.AddHours(19);
                }
                isPastShift = now >= shiftStart.AddHours(12);
            }

            shiftEnd = shiftStart.AddHours(12);
            return shiftStart;
        }

        // Note: The PreloadAllAreasAsync logic should ideally be triggered by the view to the repository or 
        // the controller can do it and notify the view. Since the view currently holds the cache,
        // we can let the controller fetch it and notify the view.
        public async Task ExecuteBackgroundPreloadAsync(DateTime shiftStart, DateTime shiftEnd)
        {
            try
            {
                // Fetch for ALL machines
                var machineList = (await _repository.GetMachineListAsync("Semua Area")).ToList();
                var machineIds = machineList.Select(m => m.MachineId).ToList();

                var logRows = await _repository.GetProcessLogsAsync(shiftStart, shiftEnd, machineIds);
                var logCache = logRows.GroupBy(x => x.MachineId).ToDictionary(g => g.Key, g => g.ToList());

                var downtimeRows = await _repository.GetMachineDowntimeAsync(shiftStart, shiftEnd, machineIds);
                var downtimeCache = downtimeRows.GroupBy(x => x.MachineId).ToDictionary(g => g.Key, g => g.ToList());

                _view.NotifyCacheReady(shiftStart, shiftEnd, logCache, downtimeCache);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Background Preload Error: " + ex.Message);
            }
        }

        public async Task<IEnumerable<string>> GetAreasAsync()
        {
            return await _repository.GetAreasAsync();
        }
    }
}
