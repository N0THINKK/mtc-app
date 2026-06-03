using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using mtc_app.shared.data.utils;
using mtc_app.shared.infrastructure;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public class MachineRunController
    {
        private readonly IMachineRunView _view;
        private long _ticketId;
        private int _initialElapsedSeconds = 0;
        private Stopwatch _stopwatch;
        private System.Windows.Forms.Timer _timer;

        public MachineRunController(IMachineRunView view, long ticketId)
        {
            _view = view;
            _ticketId = ticketId;
        }

        public void Initialize()
        {
            TryResolveSyncedTicketId();
            LoadElapsedSeconds();
            StartStopwatch();
        }

        public void Cleanup()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _stopwatch?.Stop();
        }

        private void TryResolveSyncedTicketId()
        {
            if (_ticketId >= 0) return;

            int pendingId = (int)Math.Abs(_ticketId);
            var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
            if (request != null) return; // Still pending

            try
            {
                if (!ServiceLocator.NetworkMonitor.CheckNow()) return;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var realId = conn.QueryFirstOrDefault<long?>(
                        "SELECT ticket_id FROM tickets WHERE status_id IN (1, 2, 3) ORDER BY created_at DESC LIMIT 1");
                    if (realId.HasValue && realId.Value > 0)
                    {
                        _ticketId = realId.Value;
                    }
                }
            }
            catch { }
        }

        private void LoadElapsedSeconds()
        {
            try
            {
                if (_ticketId < 0)
                {
                    int pendingId = (int)Math.Abs(_ticketId);
                    var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
                    if (request != null)
                    {
                        _initialElapsedSeconds = request.RunElapsedSeconds;
                    }
                }
                else if (_ticketId > 0)
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();
                        _initialElapsedSeconds = connection.ExecuteScalar<int>("SELECT COALESCE(run_elapsed_seconds, 0) FROM tickets WHERE ticket_id = @Id", new { Id = _ticketId });
                    }
                }
            }
            catch { }
        }

        private void StartStopwatch()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 100;
            _timer.Tick += (s, e) =>
            {
                if (_stopwatch != null && _stopwatch.IsRunning)
                {
                    int totalSeconds = _initialElapsedSeconds + (int)_stopwatch.Elapsed.TotalSeconds;
                    _view.UpdateStopwatchDisplay(TimeSpan.FromSeconds(totalSeconds).ToString(@"hh\:mm\:ss"));
                }
            };
            _timer.Enabled = true;
            _timer.Start();
        }

        public void RunMachine()
        {
            if (_ticketId < 0)
            {
                try
                {
                    int pendingId = (int)Math.Abs(_ticketId);
                    var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
                    
                    if (request != null)
                    {
                        request.StatusId = 4;
                        request.IsMachineRunning = 1;
                        request.ProductionResumedAt = DateTime.Now;
                        request.RunElapsedSeconds = _initialElapsedSeconds + (int)(_stopwatch?.Elapsed.TotalSeconds ?? 0);
                        ServiceLocator.OfflineRepo.UpdatePendingTicket(pendingId, request);
                    }
                    
                    _stopwatch?.Stop();
                    _timer?.Stop();
                    
                    _view.OpenRatingForm(_ticketId);
                    _view.CloseForm(true);
                }
                catch (Exception ex)
                {
                    _view.ShowError($"Error menyimpan offline: {ex.Message}");
                }
                return;
            }
            else
            {
                bool isOnline = ServiceLocator.NetworkMonitor.CheckNow();
                int totalSeconds = _initialElapsedSeconds + (int)(_stopwatch?.Elapsed.TotalSeconds ?? 0);

                if (!isOnline)
                {
                    ServiceLocator.OfflineRepo.AddToQueue("RUN_MACHINE", "tickets", new { TicketId = _ticketId, TotalSeconds = totalSeconds });
                }
                else
                {
                    try
                    {
                        using (var connection = DatabaseHelper.GetConnection())
                        {
                            connection.Open();
                            string sqlTicket = "UPDATE tickets SET status_id = 4, production_resumed_at = NOW(), is_machine_running = 1, run_elapsed_seconds = @Secs WHERE ticket_id = @Id";
                            connection.Execute(sqlTicket, new { Id = _ticketId, Secs = totalSeconds });

                            int machineId = connection.ExecuteScalar<int>("SELECT machine_id FROM tickets WHERE ticket_id = @Id", new { Id = _ticketId });
                            string sqlMachine = "UPDATE machines SET current_status_id = 1 WHERE machine_id = @MachineId";
                            connection.Execute(sqlMachine, new { MachineId = machineId });
                        }
                    }
                    catch (Exception ex)
                    {
                        _view.ShowError($"Gagal menyimpan data: {ex.Message}");
                        return;
                    }
                }

                _stopwatch?.Stop();
                _timer?.Stop();
                
                _view.OpenRatingForm(_ticketId);
                _view.CloseForm(true);
            }
        }

        public void BackToRepair()
        {
            try
            {
                int totalSeconds = _initialElapsedSeconds + (int)(_stopwatch?.Elapsed.TotalSeconds ?? 0);
                
                if (_ticketId > 0)
                {
                    bool isOnline = ServiceLocator.NetworkMonitor.CheckNow();
                    if (!isOnline)
                    {
                        ServiceLocator.OfflineRepo.AddToQueue("REVERT_REPAIRING", "tickets", new { TicketId = _ticketId, TotalSeconds = totalSeconds });
                    }
                    else
                    {
                        using (var connection = DatabaseHelper.GetConnection())
                        {
                            connection.Open();
                            connection.Execute("UPDATE tickets SET status_id = 2, technician_finished_at = NULL, inspection_started_at = NULL, run_elapsed_seconds = @Secs WHERE ticket_id = @Id", new { Id = _ticketId, Secs = totalSeconds });
                            connection.Execute("UPDATE ticket_technician_sessions SET is_completing_session = 0, ended_at = NULL WHERE ticket_id = @Id AND is_completing_session = 1", new { Id = _ticketId });
                        }
                    }
                }
                else if (_ticketId < 0)
                {
                    int pendingId = (int)Math.Abs(_ticketId);
                    var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
                    if (request != null)
                    {
                        request.StatusId = 2;
                        request.FinishedAt = null;
                        request.RunElapsedSeconds = totalSeconds;
                        ServiceLocator.OfflineRepo.UpdatePendingTicket(pendingId, request);
                    }
                }
                
                _stopwatch?.Stop();
                _timer?.Stop();
                
                _view.CloseForm(false);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal membatalkan status: {ex.Message}");
            }
        }
    }
}
