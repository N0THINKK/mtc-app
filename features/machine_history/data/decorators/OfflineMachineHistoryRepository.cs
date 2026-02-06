using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.features.machine_history.data.decorators
{
    public class OfflineMachineHistoryRepository : IMachineHistoryRepository
    {
        private readonly IMachineHistoryRepository _innerRepository;
        private readonly OfflineRepository _offlineRepo;
        private readonly NetworkMonitor _networkMonitor;

        public OfflineMachineHistoryRepository(
            IMachineHistoryRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor)
        {
            _innerRepository = innerRepository;
            _offlineRepo = offlineRepo;
            _networkMonitor = networkMonitor;
        }

        public async Task<IEnumerable<MachineHistoryDto>> GetHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, string machineFilter = null)
        {
            // History read logic can be sophisticated, but for now we prioritize remote.
            // Offline history read is not fully requested yet, but we can try catch.
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetHistoryAsync(startDate, endDate, machineFilter);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    // Fallback
                }
            }

            // TODO: Improve offline history read if needed.
            // Currently returns empty or cached history if implemented.
            return new List<MachineHistoryDto>(); 
        }

        public async Task<(long TicketId, string TicketCode)> CreateTicketAsync(CreateTicketRequest request)
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.CreateTicketAsync(request);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineTicket] Network error, buffering ticket: {ex.Message}");
                }
            }

            // Offline Mode: Buffer to SQLite
            int pendingId = _offlineRepo.SavePendingTicket(request);
            return (-pendingId, "OFFLINE-QUEUED"); // Negative ID indicates "Saved Locally"
        }

        private bool IsNetworkException(Exception ex)
        {
            if (ex == null) return false;
            if (ex is SocketException) return true;
            if (ex is TimeoutException) return true;
            
            var message = ex.Message?.ToLowerInvariant() ?? "";
            if (message.Contains("unable to connect")) return true;
            if (message.Contains("connection refused")) return true;
            if (message.Contains("timeout")) return true;
            
            return ex.InnerException != null && IsNetworkException(ex.InnerException);
        }

        public async Task<MachineHistoryDto> GetActiveTicketForMachineAsync(int machineId)
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetActiveTicketForMachineAsync(machineId);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    // Fallback to offline check
                }
            }

            // Offline: Check SQLite for pending tickets on this machine
            // For now, return null (offline tickets are not yet supported for this check)
            return null;
        }
    }
}
