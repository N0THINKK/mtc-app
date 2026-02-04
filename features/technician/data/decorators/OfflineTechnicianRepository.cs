using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.data.decorators;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.features.technician.data.decorators
{
    /// <summary>
    /// Offline-aware decorator for TechnicianRepository.
    /// Falls back to cached tickets when offline.
    /// </summary>
    public class OfflineTechnicianRepository : OfflineAwareRepositoryBase, ITechnicianRepository
    {
        private readonly ITechnicianRepository _innerRepository;

        public OfflineTechnicianRepository(
            ITechnicianRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor)
            : base(offlineRepo, networkMonitor)
        {
            _innerRepository = innerRepository;
        }

        /// <summary>
        /// Gets active tickets. Falls back to cached tickets when offline.
        /// </summary>
        public IEnumerable<TicketDto> GetActiveTickets()
        {
            if (!_networkMonitor.IsOnline)
            {
                System.Diagnostics.Debug.WriteLine("[OfflineTechRepo] Offline - returning cached tickets");
                return _offlineRepo.GetTicketsFromCache<TicketDto>();
            }

            try
            {
                return _innerRepository.GetActiveTickets();
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineTechRepo] Network error, using cache: {ex.Message}");
                return _offlineRepo.GetTicketsFromCache<TicketDto>();
            }
        }

        /// <summary>
        /// Gets technician statistics. Returns empty stats when offline.
        /// </summary>
        public TechnicianStatsDto GetTechnicianStatistics(long technicianId)
        {
            if (!_networkMonitor.IsOnline)
            {
                return new TechnicianStatsDto(); // Empty stats when offline
            }

            try
            {
                return _innerRepository.GetTechnicianStatistics(technicianId);
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                return new TechnicianStatsDto();
            }
        }

        /// <summary>
        /// Gets ticket detail. Returns null when offline (no individual ticket cache).
        /// </summary>
        public async Task<TechnicianTicketDetailDto> GetTicketDetailAsync(long ticketId)
        {
            if (!_networkMonitor.IsOnline)
            {
                // Could potentially find in cached list, but for now return null
                return null;
            }

            try
            {
                return await _innerRepository.GetTicketDetailAsync(ticketId);
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                return null;
            }
        }

        /// <summary>
        /// Gets leaderboard. Returns empty when offline.
        /// </summary>
        public async Task<IEnumerable<TechnicianPerformanceDto>> GetLeaderboardAsync(DateTime start, DateTime end)
        {
            if (!_networkMonitor.IsOnline)
            {
                return Enumerable.Empty<TechnicianPerformanceDto>();
            }

            try
            {
                return await _innerRepository.GetLeaderboardAsync(start, end);
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                return Enumerable.Empty<TechnicianPerformanceDto>();
            }
        }

        /// <summary>
        /// Gets machine performance. Returns empty when offline.
        /// </summary>
        public async Task<IEnumerable<MachinePerformanceDto>> GetMachinePerformanceAsync(DateTime start, DateTime end, string area = null)
        {
            if (!_networkMonitor.IsOnline)
            {
                return Enumerable.Empty<MachinePerformanceDto>();
            }

            try
            {
                return await _innerRepository.GetMachinePerformanceAsync(start, end, area);
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                return Enumerable.Empty<MachinePerformanceDto>();
            }
        }
    }
}
