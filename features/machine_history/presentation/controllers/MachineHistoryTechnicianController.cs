using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.shared.infrastructure;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public class MachineHistoryTechnicianController
    {
        private readonly IMachineHistoryTechnicianView _view;
        private readonly ITechnicianTicketRepository _repo;

        // Ticket State
        private int _ticketStatus = 1;
        private int _isMachineRunning = 0;
        private int _arrivalSeconds = 0;
        private int _repairSeconds = 0;
        private int _inspectionSeconds = 0;

        // Session Tracking
        private List<long> _activeSessionIds = new List<long>();
        private List<int> _activeTechnicianIds = new List<int>();
        private Dictionary<long, int> _sessionElapsedMap = new Dictionary<long, int>();

        private int _tickCounter = 0;

        public MachineHistoryTechnicianController(IMachineHistoryTechnicianView view, ITechnicianTicketRepository repo)
        {
            _view = view;
            _repo = repo;
        }

        public async Task InitializeAsync()
        {
            await ResolveOfflineTicketIdAsync();
            await LoadTicketStatusAsync();
            await LoadPreviousSessionsAsync();
            await LoadTicketProblemsAsync();
        }

        private async Task ResolveOfflineTicketIdAsync()
        {
            if (_view.CurrentTicketId >= 0) return;

            int pendingId = (int)Math.Abs(_view.CurrentTicketId);
            var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
            if (request != null) return;

            if (!ServiceLocator.NetworkMonitor.CheckNow()) return;

            try
            {
                var realId = await _repo.ResolveSyncedTicketIdAsync();
                if (realId.HasValue && realId.Value > 0)
                {
                    _view.CurrentTicketId = realId.Value;
                    System.Diagnostics.Debug.WriteLine($"[Controller] Resolved synced ticket: {_view.CurrentTicketId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Controller] Failed to resolve synced ticket: {ex.Message}");
            }
        }

        private async Task LoadTicketStatusAsync()
        {
            if (_view.CurrentTicketId <= 0) return;

            try
            {
                var status = await _repo.LoadTicketStatusAsync(_view.CurrentTicketId);
                if (status != null)
                {
                    _ticketStatus = status.StatusId;
                    _arrivalSeconds = status.ArrivalSeconds;
                    _repairSeconds = status.RepairSeconds;
                    _inspectionSeconds = status.InspectionSeconds;
                    _isMachineRunning = status.IsMachineRunning;

                    _view.RunOnUIThread(() => _view.UpdateMachineStateIndicator(_isMachineRunning));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Controller] Error loading status: {ex.Message}");
            }
        }

        public async Task ToggleMachineStateAsync()
        {
            int newState = (_isMachineRunning == 1) ? 0 : 1;
            string stateText = newState == 1 ? "RUN" : "STOP";

            if (!_view.ShowConfirm($"Ubah status mesin menjadi {stateText}?")) return;

            try
            {
                if (_view.CurrentTicketId > 0)
                {
                    await _repo.UpdateMachineRunningStateAsync(_view.CurrentTicketId, newState);
                }
                _isMachineRunning = newState;
                _view.RunOnUIThread(() => _view.UpdateMachineStateIndicator(_isMachineRunning));
            }
            catch (Exception ex)
            {
                _view.RunOnUIThread(() => _view.ShowError($"Gagal mengubah status mesin: {ex.Message}"));
            }
        }

        public void OnTimerTick()
        {
            if (!_view.IsVerified)
            {
                _arrivalSeconds++;
            }
            else if (_ticketStatus == 2)
            {
                _repairSeconds++;
                foreach (var sessionId in _activeSessionIds)
                {
                    if (_sessionElapsedMap.ContainsKey(sessionId))
                    {
                        _sessionElapsedMap[sessionId]++;
                    }
                }
            }
            else if (_ticketStatus == 3)
            {
                _inspectionSeconds++;
            }

            _view.UpdateTimerDisplay(_arrivalSeconds, _repairSeconds);

            _tickCounter++;

            // Sync timer to DB every 10 seconds
            if (_tickCounter % 10 == 0 && _view.CurrentTicketId > 0)
            {
                Task.Run(() => SaveTimerToDatabaseAsync());
            }
        }

        private async Task SaveTimerToDatabaseAsync()
        {
            if (_view.CurrentTicketId <= 0 || !ServiceLocator.NetworkMonitor.IsOnline) return;

            try
            {
                await _repo.SaveTicketTimersAsync(_view.CurrentTicketId, _arrivalSeconds, _repairSeconds, _inspectionSeconds);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Controller] Error saving timer: {ex.Message}");
            }
        }

        private async Task LoadPreviousSessionsAsync()
        {
            if (_view.CurrentTicketId <= 0) return;

            try
            {
                var sessions = await _repo.LoadPreviousSessionsAsync(_view.CurrentTicketId);
                if (sessions.Any())
                {
                    var lines = new List<string>();
                    lines.Add("⚠️ Riwayat Sesi Teknisi:");

                    foreach (var s in sessions)
                    {
                        var ts = TimeSpan.FromSeconds(s.Elapsed);
                        string duration = ts.TotalMinutes >= 1 ? $"{(int)ts.TotalMinutes} menit" : $"{s.Elapsed} detik";
                        string marker = s.IsCompleting == 1 ? " ✅" : "";
                        lines.Add($"  • {s.TechName}: {duration}{marker}");
                    }

                    _view.RunOnUIThread(() => _view.ShowPreviousSessions(lines));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Controller] Error loading sessions: {ex.Message}");
            }
        }

        private async Task LoadTicketProblemsAsync()
        {
            if (_view.CurrentTicketId <= 0) return;

            try
            {
                var problems = await _repo.LoadTicketProblemsAsync(_view.CurrentTicketId);
                int idx = 0;
                foreach (var p in problems)
                {
                    _view.RunOnUIThread(() => _view.AddProblemItem(p.ProblemId, p.ProblemType, p.ProblemDetail, _view.IsVerified, idx++));
                }
            }
            catch (Exception ex)
            {
                _view.RunOnUIThread(() => _view.ShowError($"Gagal memuat problem: {ex.Message}"));
            }
        }
    }
}
