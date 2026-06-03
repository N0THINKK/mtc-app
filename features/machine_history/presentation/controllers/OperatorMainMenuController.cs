using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using mtc_app.shared.data.session;
using mtc_app.shared.data.utils;
using mtc_app.shared.infrastructure;
using Newtonsoft.Json.Linq;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public class OperatorMainMenuController
    {
        private readonly IOperatorMainMenuView _view;
        
        private int? _currentActiveRecordId = null;
        private DateTime? _offlineStartTime = null;

        private int _currentTrackedHour = DateTime.Now.Hour;
        private int _viewedHour = DateTime.Now.Hour;

        private Dictionary<string, int> _quickCounts = new Dictionary<string, int>
        {
            { "Wire", 0 },
            { "Applikator A", 0 },
            { "Applikator B", 0 },
            { "Double", 0 }
        };

        public OperatorMainMenuController(IOperatorMainMenuView view)
        {
            _view = view;
        }

        public int GetMachineIdInt()
        {
            string machineIdStr = DatabaseHelper.GetMachineId();
            if (int.TryParse(machineIdStr, out int mId)) return mId;
            return 0;
        }

        public async Task CheckActiveIdleStatusAsync()
        {
            try
            {
                int mId = GetMachineIdInt();
                if (mId == 0) return;

                using (var conn = DatabaseHelper.GetConnection())
                {
                    var sql = "SELECT id, activity_id, (SELECT activity_name FROM activity_types WHERE id = activity_id) as act_name FROM machine_operator_activities WHERE machine_id = @MId AND end_time IS NULL ORDER BY start_time DESC LIMIT 1";
                    var activeRec = await conn.QueryFirstOrDefaultAsync(sql, new { MId = mId });
                    
                    if (activeRec != null)
                    {
                        _currentActiveRecordId = Convert.ToInt32(activeRec.id);
                        string actName = activeRec.act_name?.ToString() ?? "Unknown";
                        _view.SetIdleState(actName);
                    }
                    else
                    {
                        _view.SetRunState();
                    }
                }
            }
            catch { }
        }

        public void StopCurrentDowntime()
        {
            if (_currentActiveRecordId != null || _offlineStartTime != null)
            {
                try
                {
                    int mId = GetMachineIdInt();

                    if (_offlineStartTime != null)
                    {
                        var payload = new
                        {
                            MachineId = mId,
                            StartTime = _offlineStartTime.Value,
                            EndTime = DateTime.Now
                        };
                        ServiceLocator.OfflineRepo.AddToQueue("END_ACTIVITY", "machine_operator_activities", payload);
                    }
                    else if (_currentActiveRecordId != null)
                    {
                        if (ServiceLocator.NetworkMonitor.CheckNow())
                        {
                            using (var conn = DatabaseHelper.GetConnection())
                            {
                                string sql = "UPDATE machine_operator_activities SET end_time = @Now WHERE id = @Id";
                                conn.Execute(sql, new { Now = DateTime.Now, Id = _currentActiveRecordId.Value });
                            }
                        }
                        else
                        {
                            var payload = new
                            {
                                RecordId = _currentActiveRecordId.Value,
                                EndTime = DateTime.Now
                            };
                            ServiceLocator.OfflineRepo.AddToQueue("END_ACTIVITY_BY_ID", "machine_operator_activities", payload);
                        }
                    }
                }
                catch { }
                finally
                {
                    _currentActiveRecordId = null;
                    _offlineStartTime = null;
                    _view.SetRunState();
                }
            }
        }

        public void ToggleMachineState(int selectedActivityId, string selectedActivityName)
        {
            try
            {
                int mId = GetMachineIdInt();
                if (mId == 0) 
                {
                    _view.ShowWarning("Mesin belum dikonfigurasi. Silakan setup ID Mesin terlebih dahulu.");
                    return;
                }

                string opName = UserSession.CurrentUser?.Username ?? "Unknown";
                TimeSpan nowTime = DateTime.Now.TimeOfDay;
                string shiftName = (nowTime >= new TimeSpan(7, 0, 0) && nowTime < new TimeSpan(19, 0, 0)) ? "Shift Pagi" : "Shift Malam";

                if (_currentActiveRecordId == null && _offlineStartTime == null)
                {
                    // Online path
                    if (ServiceLocator.NetworkMonitor.CheckNow())
                    {
                        using (var conn = DatabaseHelper.GetConnection())
                        {
                            string sql = "INSERT INTO machine_operator_activities (machine_id, operator_name, activity_id, start_time, shift_name) VALUES (@MId, @OpName, @ActId, @Now, @Shift); SELECT LAST_INSERT_ID();";
                            var startTime = DateTime.Now;
                            int newId = conn.QuerySingle<int>(sql, new { MId = mId, OpName = opName, ActId = selectedActivityId, Now = startTime, Shift = shiftName });
                            
                            _currentActiveRecordId = newId;
                            _view.SetIdleState(selectedActivityName);
                        }
                    }
                    else
                    {
                        var now = DateTime.Now;
                        var startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                        var payload = new
                        {
                            MachineId = mId,
                            OperatorName = opName,
                            ActivityId = selectedActivityId,
                            StartTime = startTime,
                            ShiftName = shiftName
                        };
                        ServiceLocator.OfflineRepo.AddToQueue("START_ACTIVITY", "machine_operator_activities", payload);

                        _offlineStartTime = startTime;
                        _view.SetIdleState(selectedActivityName);
                    }
                }
                else
                {
                    StopCurrentDowntime();
                }
            }
            catch (Exception ex)
            {
                _view.ShowError("Gagal mengupdate status: " + ex.Message);
            }
        }

        public void CheckHourChange()
        {
            if (DateTime.Now.Hour != _currentTrackedHour)
            {
                _currentTrackedHour = DateTime.Now.Hour;
                _viewedHour = _currentTrackedHour;
                UpdateJamDisplay();
                _ = FetchCurrentHourCountsAsync();
            }
        }

        public void ChangeViewedHour(int delta)
        {
            if (delta < 0)
            {
                int currentShiftHour = GetShiftHourDisplay(_viewedHour);
                if (currentShiftHour > 1)
                {
                    _viewedHour--;
                    if (_viewedHour < 0) _viewedHour = 23;
                    UpdateJamDisplay();
                    _ = FetchCurrentHourCountsAsync();
                }
            }
            else if (delta > 0)
            {
                if (_viewedHour != _currentTrackedHour)
                {
                    _viewedHour++;
                    if (_viewedHour > 23) _viewedHour = 0;
                    UpdateJamDisplay();
                    _ = FetchCurrentHourCountsAsync();
                }
            }
        }

        private void UpdateJamDisplay()
        {
            _view.UpdateJamDisplay(_viewedHour, _currentTrackedHour, GetShiftHourDisplay(_viewedHour));
        }

        private int GetShiftHourDisplay(int realHour)
        {
            if (realHour >= 7 && realHour < 19) return realHour - 7 + 1;
            if (realHour >= 19) return realHour - 19 + 1;
            return realHour + 6;
        }

        private DateTime GetDateForHour(int hour)
        {
            var now = DateTime.Now;
            var shiftStart = new TimeSpan(7, 0, 0);
            var shiftEnd = new TimeSpan(19, 0, 0);
            bool isCurrentShiftPagi = now.TimeOfDay >= shiftStart && now.TimeOfDay < shiftEnd;

            if (isCurrentShiftPagi)
            {
                return now.Date;
            }
            else
            {
                DateTime shiftStartDate = now.TimeOfDay >= shiftEnd ? now.Date : now.Date.AddDays(-1);
                if (hour >= 19 && hour <= 23) return shiftStartDate;
                else return shiftStartDate.AddDays(1);
            }
        }

        public async Task FetchCurrentHourCountsAsync()
        {
            var keys = new List<string>(_quickCounts.Keys);
            foreach (var key in keys) _quickCounts[key] = 0;

            int mId = GetMachineIdInt();
            string opName = UserSession.CurrentUser?.Username ?? "Unknown";
            int currentHour = _viewedHour;
            DateTime currentDate = GetDateForHour(_viewedHour);

            string cacheKey = $"QuickCount_{mId}_{opName}_{currentDate:yyyyMMdd}_{currentHour}";

            if (ServiceLocator.NetworkMonitor.CheckNow())
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        string sql = @"
                            SELECT item_name, total_count 
                            FROM operator_quick_counts 
                            WHERE machine_id = @MId AND operator_name = @OpName 
                            AND record_date = @RecordDate AND record_hour = @RecordHour";

                        var results = await conn.QueryAsync(sql, new 
                        { 
                            MId = mId, OpName = opName, RecordDate = currentDate, RecordHour = currentHour
                        });

                        foreach (var row in results)
                        {
                            string itemName = row.item_name;
                            int count = (int)row.total_count;
                            if (_quickCounts.ContainsKey(itemName)) _quickCounts[itemName] = count;
                        }

                        ServiceLocator.OfflineRepo.SetCache(cacheKey, _quickCounts, TimeSpan.FromHours(12));
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    var cached = ServiceLocator.OfflineRepo.GetCache<Dictionary<string, int>>(cacheKey);
                    if (cached != null)
                    {
                        foreach (var kvp in cached)
                        {
                            if (_quickCounts.ContainsKey(kvp.Key)) _quickCounts[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch { }
            }

            try
            {
                var queueItems = ServiceLocator.OfflineRepo.GetPendingItems();
                foreach (var item in queueItems)
                {
                    if (item.TableName == "operator_quick_counts" && 
                        (item.ActionType == "INCREMENT_QUICK_COUNT" || item.ActionType == "DECREMENT_QUICK_COUNT"))
                    {
                        var json = JObject.Parse(item.PayloadJson);
                        int hmId = json["MachineId"]?.ToObject<int>() ?? 0;
                        string hopName = json["OperatorName"]?.ToString() ?? "";
                        string hItemName = json["ItemName"]?.ToString() ?? "";
                        DateTime hRecordDate = json["RecordDate"]?.ToObject<DateTime>() ?? DateTime.MinValue;
                        int hRecordHour = json["RecordHour"]?.ToObject<int>() ?? -1;

                        if (hmId == mId && hopName == opName && hRecordDate.Date == currentDate && hRecordHour == currentHour)
                        {
                            int delta = item.ActionType == "INCREMENT_QUICK_COUNT" ? 1 : -1;
                            if (_quickCounts.ContainsKey(hItemName))
                            {
                                int newCount = _quickCounts[hItemName] + delta;
                                _quickCounts[hItemName] = Math.Max(0, newCount);
                            }
                        }
                    }
                }
            }
            catch { }

            foreach (var kvp in _quickCounts)
            {
                _view.UpdateQuickCountDisplay(kvp.Key, kvp.Value);
            }
        }

        public void UpdateQuickCount(string itemName, int delta)
        {
            CheckHourChange();

            if (!_quickCounts.ContainsKey(itemName)) return;

            int current = _quickCounts[itemName];
            int newCount = current + delta;
            if (newCount < 0) newCount = 0;
            if (newCount == current) return;

            _quickCounts[itemName] = newCount;
            _view.UpdateQuickCountDisplay(itemName, newCount);

            try
            {
                int mId = GetMachineIdInt();
                string opName = UserSession.CurrentUser?.Username ?? "Unknown";
                TimeSpan nowTime = DateTime.Now.TimeOfDay;
                string shiftName = (nowTime >= new TimeSpan(7, 0, 0) && nowTime < new TimeSpan(19, 0, 0)) ? "Shift Pagi" : "Shift Malam";

                var payload = new
                {
                    MachineId = mId,
                    OperatorName = opName,
                    ShiftName = shiftName,
                    ItemName = itemName,
                    RecordDate = GetDateForHour(_viewedHour),
                    RecordHour = _viewedHour
                };

                string actionType = delta > 0 ? "INCREMENT_QUICK_COUNT" : "DECREMENT_QUICK_COUNT";
                ServiceLocator.OfflineRepo.AddToQueue(actionType, "operator_quick_counts", payload);

                if (ServiceLocator.NetworkMonitor.CheckNow())
                {
                    _view.TriggerSync();
                }
            }
            catch { }
        }
    }
}
