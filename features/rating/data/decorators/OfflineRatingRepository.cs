using System;
using System.Threading.Tasks;
using mtc_app.features.rating.data.dtos;
using mtc_app.features.rating.data.repositories;
using mtc_app.shared.data.decorators;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.features.rating.data.decorators
{
    /// <summary>
    /// Decorator that adds offline support to RatingRepository.
    /// </summary>
    public class OfflineRatingRepository : OfflineAwareRepositoryBase, IRatingRepository
    {
        private readonly IRatingRepository _innerRepository;

        public OfflineRatingRepository(
            IRatingRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor)
            : base(offlineRepo, networkMonitor)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        /// <summary>
        /// Write operation with offline fallback.
        /// </summary>
        public async Task SubmitRatingAsync(RatingDto rating)
        {
            await ExecuteWithOfflineFallbackAsync(
                () => _innerRepository.SubmitRatingAsync(rating),
                "UPDATE",
                "tickets",
                rating
            );
        }
    }
}
