using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.shared.data.decorators;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.features.machine_history.data.decorators
{
    /// <summary>
    /// Offline-aware decorator for MachineHistoryRepository.
    /// Falls back to cached history (last 90 days) when offline.
    /// </summary>
    public class OfflineMachineHistoryRepository : OfflineAwareRepositoryBase, IMachineHistoryRepository
    {
        private readonly IMachineHistoryRepository _innerRepository;

        public OfflineMachineHistoryRepository(
            IMachineHistoryRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor)
            : base(offlineRepo, networkMonitor)
        {
            _innerRepository = innerRepository;
        }

        /// <summary>
        /// Gets machine history. Falls back to cached history when offline.
        /// Note: Cached data is limited to last 90 days.
        /// </summary>
        public async Task<IEnumerable<MachineHistoryDto>> GetHistoryAsync(
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            string machineFilter = null)
        {
            if (!_networkMonitor.IsOnline)
            {
                System.Diagnostics.Debug.WriteLine("[OfflineHistoryRepo] Offline - returning cached history");
                var cached = _offlineRepo.GetHistoryFromCache<MachineHistoryDto>();
                
                // Apply filters to cached data
                return FilterCachedHistory(cached, startDate, endDate, machineFilter);
            }

            try
            {
                return await _innerRepository.GetHistoryAsync(startDate, endDate, machineFilter);
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineHistoryRepo] Network error, using cache: {ex.Message}");
                var cached = _offlineRepo.GetHistoryFromCache<MachineHistoryDto>();
                return FilterCachedHistory(cached, startDate, endDate, machineFilter);
            }
        }

        /// <summary>
        /// Applies filters to cached history data.
        /// </summary>
        private IEnumerable<MachineHistoryDto> FilterCachedHistory(
            List<MachineHistoryDto> cached,
            DateTime? startDate,
            DateTime? endDate,
            string machineFilter)
        {
            if (cached == null || cached.Count == 0)
                return Enumerable.Empty<MachineHistoryDto>();

            var result = cached.AsEnumerable();

            if (startDate.HasValue)
            {
                result = result.Where(h => h.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                result = result.Where(h => h.CreatedAt <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(machineFilter))
            {
                result = result.Where(h => 
                    h.MachineName != null && 
                    h.MachineName.IndexOf(machineFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return result.OrderByDescending(h => h.CreatedAt).ToList();
        }
    }
}
