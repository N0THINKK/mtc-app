using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.group_leader.data.dtos;
using mtc_app.features.group_leader.data.repositories;
using mtc_app.shared.data.decorators;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.features.group_leader.data.decorators
{
    /// <summary>
    /// Decorator that adds offline support to GroupLeaderRepository.
    /// Read operations pass through; write operations use offline fallback.
    /// </summary>
    public class OfflineGroupLeaderRepository : OfflineAwareRepositoryBase, IGroupLeaderRepository
    {
        private readonly IGroupLeaderRepository _innerRepository;

        public OfflineGroupLeaderRepository(
            IGroupLeaderRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor)
            : base(offlineRepo, networkMonitor)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        /// <summary>
        /// Pass-through: Read operations don't need offline queueing.
        /// </summary>
        public Task<IEnumerable<GroupLeaderTicketDto>> GetTicketsAsync(string statusFilter = null, string machineFilter = null)
        {
            return _innerRepository.GetTicketsAsync(statusFilter, machineFilter);
        }

        /// <summary>
        /// Pass-through: Read operations don't need offline queueing.
        /// </summary>
        public Task<GroupLeaderTicketDetailDto> GetTicketDetailAsync(Guid ticketId)
        {
            return _innerRepository.GetTicketDetailAsync(ticketId);
        }

        /// <summary>
        /// Write operation with offline fallback.
        /// If network fails, queues the validation for later sync.
        /// </summary>
        public async Task ValidateTicketAsync(Guid ticketId, int rating, string note)
        {
            var payload = new ValidateTicketPayload
            {
                TicketId = ticketId,
                Rating = rating,
                Note = note
            };

            await ExecuteWithOfflineFallbackAsync(
                () => _innerRepository.ValidateTicketAsync(ticketId, rating, note),
                "UPDATE",
                "tickets",
                payload
            );
        }
    }

    /// <summary>
    /// Payload for ticket validation sync.
    /// </summary>
    public class ValidateTicketPayload
    {
        public Guid TicketId { get; set; }
        public int Rating { get; set; }
        public string Note { get; set; }
    }
}
