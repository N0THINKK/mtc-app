using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
    /// Read operations fall back to cache; write operations use offline fallback.
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
        /// Read with offline fallback to cached tickets.
        /// </summary>
        public async Task<IEnumerable<GroupLeaderTicketDto>> GetTicketsAsync(string statusFilter = null, string machineFilter = null)
        {
            // Try online first if network appears available
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetTicketsAsync(statusFilter, machineFilter);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineGroupLeader] Network error, using cached tickets: {ex.Message}");
                }
            }

            // Offline fallback: Return cached tickets
            System.Diagnostics.Debug.WriteLine("[OfflineGroupLeader] Loading tickets from cache...");
            var cachedTickets = _offlineRepo.GetTicketsFromCache<GroupLeaderTicketDto>();
            
            // Filter to match online behavior (Status >= 2)
            // Also ensure TicketUuid is valid (in case of legacy cache?)
            return cachedTickets.Where(t => t.StatusId >= 2 && t.TicketUuid != Guid.Empty).ToList();
        }

        /// <summary>
        /// Read with offline fallback.
        /// </summary>
        public async Task<GroupLeaderTicketDetailDto> GetTicketDetailAsync(Guid ticketId)
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetTicketDetailAsync(ticketId);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineGroupLeader] Network error getting detail: {ex.Message}");
                }
            }

            // Offline fallback: Use CachedMachineHistory
            System.Diagnostics.Debug.WriteLine("[OfflineGroupLeader] Loading ticket detail from cache...");
            
            // 1. Try Cached History (Completed tickets)
            var cachedHistory = _offlineRepo.GetHistoryFromCache<mtc_app.features.machine_history.data.dtos.MachineHistoryDto>();
            var historyItem = cachedHistory.FirstOrDefault(h => h.TicketUuid == ticketId);

            if (historyItem != null)
            {
                return new GroupLeaderTicketDetailDto
                {
                    TicketId = historyItem.TicketUuid,
                    TicketCode = historyItem.TicketCode,
                    MachineName = historyItem.MachineName,
                    TechnicianName = historyItem.TechnicianName,
                    OperatorName = historyItem.OperatorName,
                    FailureDetails = historyItem.Issue,
                    ActionDetails = historyItem.Resolution,
                    CounterStroke = historyItem.CounterStroke,
                    CreatedAt = historyItem.CreatedAt,
                    StartedAt = historyItem.StartedAt,
                    FinishedAt = historyItem.FinishedAt,
                    
                    TechRatingScore = historyItem.TechRatingScore,
                    TechRatingNote = historyItem.TechRatingNote,
                    GlRatingScore = historyItem.GlRatingScore,
                    GlRatingNote = historyItem.GlRatingNote
                };
            }

            // 2. If not found in history, it might be in active tickets (but less detailed)
            // For now, return null or try to map basic info? 
            // GL usually only views completed tickets for rating.
            
            System.Diagnostics.Debug.WriteLine("[OfflineGroupLeader] Ticket detail not found in cache");
            return null;
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

        private bool IsNetworkException(Exception ex)
        {
            if (ex == null) return false;
            if (ex is SocketException) return true;
            if (ex is TimeoutException) return true;
            
            var message = ex.Message?.ToLowerInvariant() ?? "";
            if (message.Contains("unable to connect")) return true;
            if (message.Contains("connection refused")) return true;
            if (message.Contains("timeout")) return true;
            if (message.Contains("host")) return true;
            
            return ex.InnerException != null && IsNetworkException(ex.InnerException);
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

