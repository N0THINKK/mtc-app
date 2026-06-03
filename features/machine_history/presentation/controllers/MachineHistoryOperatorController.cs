using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.shared.data.repositories;
using mtc_app.shared.infrastructure;
using mtc_app.features.applicator_patrol.data.services;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public class MachineHistoryOperatorController
    {
        private readonly IMachineHistoryOperatorView _view;
        private readonly IMachineHistoryRepository _repository;
        private readonly IMasterDataRepository _masterDataRepository;

        private MachineHistoryDto _pendingTicket;

        public MachineHistoryOperatorController(
            IMachineHistoryOperatorView view, 
            IMachineHistoryRepository repository, 
            IMasterDataRepository masterDataRepository)
        {
            _view = view;
            _repository = repository;
            _masterDataRepository = masterDataRepository;
        }

        public async Task InitializeAsync()
        {
            await LoadAreasAsync();
            await LoadShiftsAsync();
            await Task.Run(() => LoadApplicatorsFromExcel());
            await CheckForPendingTicketAsync();
        }

        private async Task LoadShiftsAsync()
        {
            try
            {
                var shifts = await _masterDataRepository.GetShiftsAsync();
                _view.PopulateShifts(shifts.Select(s => s.ShiftName).ToArray());
            }
            catch { /* Ignore */ }
        }

        private void LoadApplicatorsFromExcel()
        {
            try
            {
                string machineCode = "";
                if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                {
                    var machines = _masterDataRepository.GetMachinesAsync().Result;
                    var machine = machines?.FirstOrDefault(m => m.MachineId == configId);
                    if (machine != null) machineCode = machine.Code ?? "";
                }

                string excelPath = @"C:\MTC_System\Data\MasterAplikator.xls";
                if (!File.Exists(excelPath))
                {
                    string fallback = excelPath + "x";
                    if (File.Exists(fallback)) excelPath = fallback;
                }

                if (!File.Exists(excelPath)) return;

                var (sideA, sideB) = ApplicatorExcelReader.ReadApplicators(excelPath, machineCode);
                var allApplicators = sideA.Union(sideB).OrderBy(x => x).ToArray();

                if (allApplicators.Length > 0)
                {
                    _view.PopulateApplicators(allApplicators);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MachineHistory] Error loading applicators: {ex.Message}");
            }
        }

        private async Task LoadAreasAsync()
        {
            try
            {
                // In a perfect architecture this is in MasterDataRepository.
                // For MVP transition, we execute inline or assume repository has it.
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var areas = await Dapper.SqlMapper.QueryAsync<string>(conn, "SELECT area_name FROM machine_areas WHERE area_name != 'Lain2' ORDER BY area_name");
                    _view.PopulateAreas(areas.ToArray());
                }
            }
            catch { /* Ignore */ }
        }

        public async Task LoadHistoryAsync()
        {
            try
            {
                string areaFilter = _view.FilterArea;
                if (areaFilter == "Semua") areaFilter = null;

                int? machineId = null;
                if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                {
                    machineId = configId;
                }

                var history = await _repository.GetHistoryAsync(_view.FilterStartDate, _view.FilterEndDate, null, areaFilter, machineId);
                _view.SetHistoryData(history.ToList());
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal memuat riwayat: {ex.Message}");
            }
        }

        public async Task CheckForPendingTicketAsync()
        {
            try
            {
                int machineId = 1;
                if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                {
                    machineId = configId;
                }

                _pendingTicket = await _repository.GetActiveTicketForMachineAsync(machineId);
                
                if (_pendingTicket != null)
                {
                    _view.ShowPendingTicket(_pendingTicket.StatusName.ToUpper());
                }
                else
                {
                    _view.HidePendingTicket();
                }
            }
            catch
            {
                _view.HidePendingTicket();
            }
        }

        public void HandlePendingTicketClick()
        {
            if (_pendingTicket != null)
            {
                _view.OpenTechnicianForm(_pendingTicket.TicketId);
            }
        }

        public async Task SubmitTicketAsync()
        {
            if (string.IsNullOrWhiteSpace(_view.OperatorNik) || string.IsNullOrWhiteSpace(_view.Shift))
            {
                _view.ShowWarning("Mohon lengkapi data wajib (NIK Operator dan Shift).");
                return;
            }

            var problems = _view.GetProblems();
            foreach (var p in problems)
            {
                if (string.IsNullOrWhiteSpace(p.ProblemType) || string.IsNullOrWhiteSpace(p.ProblemDetail))
                {
                    _view.ShowWarning("Mohon lengkapi detail problem.");
                    return;
                }
            }

            try 
            {
                int machineId = 1;
                if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                {
                    machineId = configId;
                }

                var request = new CreateTicketRequest
                {
                    OperatorNik = _view.OperatorNik, 
                    ShiftName = _view.Shift,
                    ApplicatorCode = _view.Applicator,
                    MachineId = machineId,
                    Problems = problems.Select(p => new TicketProblemRequest 
                    { 
                        ProblemTypeName = p.ProblemType,
                        FailureName = p.ProblemDetail 
                    }).ToList()
                };

                var result = await _repository.CreateTicketAsync(request);

                string successMsg = (result.TicketId < 0) 
                    ? "Tiket Disimpan Offline.\\nMenunggu Sinkronisasi." 
                    : $"Tiket Berhasil Dibuat!\\nKode: {result.TicketCode}";

                _view.ShowSuccess(successMsg);
                _view.OpenTechnicianForm(result.TicketId);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null) msg += $"\\nDetails: {ex.InnerException.Message}";
                _view.ShowError($"Gagal menyimpan: {msg}", "Error Database");
            }
        }
    }
}
