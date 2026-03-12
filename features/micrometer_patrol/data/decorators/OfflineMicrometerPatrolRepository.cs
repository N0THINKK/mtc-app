using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.micrometer_patrol.data.dtos;
using mtc_app.features.micrometer_patrol.data.repositories;
using mtc_app.shared.data.decorators;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.features.micrometer_patrol.data.decorators
{
    public class OfflineMicrometerPatrolRepository : OfflineAwareRepositoryBase, IMicrometerPatrolRepository
    {
        private readonly IMicrometerPatrolRepository _innerRepository;

        public OfflineMicrometerPatrolRepository(
            IMicrometerPatrolRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor) : base(offlineRepo, networkMonitor)
        {
            _innerRepository = innerRepository;
        }

        public async Task<bool> SavePatrolAsync(MicrometerPatrolDto patrolData)
        {
            return await ExecuteWithOfflineFallbackAsync(
                () => _innerRepository.SavePatrolAsync(patrolData),
                "INSERT",
                "micrometer_patrols",
                patrolData,
                defaultValue: true
            );
        }

        public async Task<IEnumerable<MicrometerPatrolDto>> GetTodayPatrolsAsync(DateTime date)
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetTodayPatrolsAsync(date);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    // Fallback to offline if network error occurs
                }
            }
            
            // Offline: return empty list as there's no local cache for this yet.
            return new List<MicrometerPatrolDto>();
        }
    }
}