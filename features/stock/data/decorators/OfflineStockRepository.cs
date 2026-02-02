using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.stock.data.dtos;
using mtc_app.features.stock.data.enums;
using mtc_app.features.stock.data.repositories;
using mtc_app.shared.data.decorators;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.features.stock.data.decorators
{
    /// <summary>
    /// Decorator that adds offline support to StockRepository.
    /// </summary>
    public class OfflineStockRepository : OfflineAwareRepositoryBase, IStockRepository
    {
        private readonly IStockRepository _innerRepository;

        public OfflineStockRepository(
            IStockRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor)
            : base(offlineRepo, networkMonitor)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        /// <summary>
        /// Pass-through: Read operation.
        /// </summary>
        public Task<IEnumerable<PartRequestDto>> GetRequestsAsync(RequestStatus? filter, SortOrder sort)
        {
            return _innerRepository.GetRequestsAsync(filter, sort);
        }

        /// <summary>
        /// Pass-through: Read operation.
        /// </summary>
        public Task<StockStatsDto> GetStatsAsync()
        {
            return _innerRepository.GetStatsAsync();
        }

        /// <summary>
        /// Write operation with offline fallback.
        /// </summary>
        public async Task<bool> MarkAsReadyAsync(int requestId)
        {
            var payload = new MarkAsReadyPayload { RequestId = requestId };

            return await ExecuteWithOfflineFallbackAsync(
                () => _innerRepository.MarkAsReadyAsync(requestId),
                "UPDATE",
                "part_requests",
                payload,
                defaultValue: true // Optimistic: assume success when queued
            );
        }
    }

    /// <summary>
    /// Payload for mark as ready sync.
    /// </summary>
    public class MarkAsReadyPayload
    {
        public int RequestId { get; set; }
    }
}
