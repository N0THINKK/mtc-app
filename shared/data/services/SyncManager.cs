using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using mtc_app;
using mtc_app.shared.data.local;

namespace mtc_app.shared.data.services
{
    /// <summary>
    /// Manages synchronization of offline queue items to the main database.
    /// Listens to NetworkMonitor and processes queue when online.
    /// </summary>
    public class SyncManager : IDisposable
    {
        private readonly OfflineRepository _offlineRepo;
        private readonly NetworkMonitor _networkMonitor;
        private readonly Timer _syncTimer;
        private readonly int _syncIntervalMs;
        private bool _isSyncing;
        private bool _disposed;

        /// <summary>
        /// Fired when sync status changes.
        /// </summary>
        public event EventHandler<SyncStatusEventArgs> OnSyncStatusChanged;

        /// <summary>
        /// Fired when an item is moved to dead letter queue.
        /// </summary>
        public event EventHandler<DeadLetterEventArgs> OnDeadLetterMoved;

        /// <summary>
        /// Gets remaining items in queue.
        /// </summary>
        public int PendingCount => _offlineRepo.GetQueueCount();

        /// <summary>
        /// Gets count of items in dead letter queue.
        /// </summary>
        public int DeadLetterCount => _offlineRepo.GetDeadLetterCount();

        /// <summary>
        /// Creates a new SyncManager.
        /// </summary>
        /// <param name="offlineRepo">The offline repository instance.</param>
        /// <param name="networkMonitor">The network monitor instance.</param>
        /// <param name="syncIntervalMs">Interval between sync attempts (default: 30s)</param>
        public SyncManager(OfflineRepository offlineRepo, NetworkMonitor networkMonitor, int syncIntervalMs = 30000)
        {
            _offlineRepo = offlineRepo;
            _networkMonitor = networkMonitor;
            _syncIntervalMs = syncIntervalMs;
            _isSyncing = false;

            // Subscribe to network status changes
            _networkMonitor.OnStatusChanged += OnNetworkStatusChanged;

            // Start periodic sync timer
            _syncTimer = new Timer(TrySyncAsync, null, syncIntervalMs, syncIntervalMs);
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusEventArgs e)
        {
            if (e.IsOnline)
            {
                // Network came back online - trigger immediate sync
                TrySyncAsync(null);
            }
        }

        private void TrySyncAsync(object state)
        {
            // Prevent concurrent sync operations
            if (_isSyncing || !_networkMonitor.IsOnline)
                return;

            Task.Run(() =>
            {
                try
                {
                    _isSyncing = true;
                    ProcessQueue();
                }
                finally
                {
                    _isSyncing = false;
                }
            });
        }

        private void ProcessQueue()
        {
            var items = _offlineRepo.GetPendingItems();
            if (items.Count == 0)
                return;

            OnSyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(
                SyncStatus.Syncing, 
                $"Syncing {items.Count} pending items..."
            ));

            int successCount = 0;
            int failCount = 0;

            foreach (var item in items)
            {
                try
                {
                    // TODO: Wire real sync logic here
                    // For now, just log and mark as processed
                    bool success = ProcessSyncItem(item);

                    if (success)
                    {
                        _offlineRepo.RemoveFromQueue(item.Id);
                        successCount++;
                    }
                    else
                    {
                        HandleFailedItem(item, "Sync returned false");
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncManager] Error processing item {item.Id}: {ex.Message}");
                    HandleFailedItem(item, ex.Message);
                    failCount++;
                }
            }

            var remaining = _offlineRepo.GetQueueCount();
            var status = remaining == 0 ? SyncStatus.Complete : SyncStatus.PartialComplete;
            OnSyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(
                status,
                $"Synced: {successCount}, Failed: {failCount}, Remaining: {remaining}"
            ));
        }

        /// <summary>
        /// Processes a single sync item by dispatching to appropriate handler.
        /// </summary>
        protected virtual bool ProcessSyncItem(SyncQueueItem item)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SyncManager] Processing: {item.ActionType} on {item.TableName} (ID: {item.Id})"
            );

            try
            {
                // Dispatch based on table and action
                switch (item.TableName)
                {
                    case "tickets":
                        return ProcessTicketSync(item);
                    
                    case "part_requests":
                        return ProcessPartRequestSync(item);
                    
                    default:
                        System.Diagnostics.Debug.WriteLine($"[SyncManager] Unknown table: {item.TableName}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncManager] Sync error: {ex.Message}");
                return false;
            }
        }

        private bool ProcessTicketSync(SyncQueueItem item)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Determine payload type and execute appropriate SQL
                var json = Newtonsoft.Json.Linq.JObject.Parse(item.PayloadJson);
                
                // Case-insensitive key check helper
                bool HasKey(string key) => json.GetValue(key, StringComparison.OrdinalIgnoreCase) != null;

                // Check if it's a ValidateTicketPayload (has TicketId and Rating)
                if (HasKey("TicketId") && HasKey("Rating"))
                {
                    // Normalize UUID to string ensuring standard format
                    var ticketUuidStr = json.GetValue("TicketId", StringComparison.OrdinalIgnoreCase).ToObject<Guid>().ToString();
                    var rating = json.GetValue("Rating", StringComparison.OrdinalIgnoreCase).ToObject<int>();
                    var note = json.GetValue("Note", StringComparison.OrdinalIgnoreCase)?.ToString();

                    System.Diagnostics.Debug.WriteLine($"[SyncManager] Syncing Validation: ID={ticketUuidStr}, Rating={rating}");

                    string sql = @"
                        UPDATE tickets 
                        SET gl_rating_score = @Rating, 
                            gl_rating_note = @Note,
                            gl_validated_at = NOW(),
                            status_id = 3
                        WHERE ticket_uuid = @TicketId";

                    try 
                    {
                        var affected = connection.Execute(sql, new { TicketId = ticketUuidStr, Rating = rating, Note = note });
                        if (affected == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SyncManager] Validation Sync Failed: Ticket {ticketUuidStr} not found in DB.");
                            // Consider retrying or checking if ticket exists by ID?
                        }
                        return affected > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SyncManager] SQL Error in Validation Sync: {ex.Message}");
                        throw; // Let ProcessQueue handle retry
                    }
                }
                // Check if it's a RatingDto (has TicketId and Score - Technician Rating)
                else if (HasKey("TicketId") && HasKey("Score"))
                {
                    var ticketId = json.GetValue("TicketId", StringComparison.OrdinalIgnoreCase).ToObject<long>();
                    var score = json.GetValue("Score", StringComparison.OrdinalIgnoreCase).ToObject<int>();
                    var comment = json.GetValue("Comment", StringComparison.OrdinalIgnoreCase)?.ToString();

                    System.Diagnostics.Debug.WriteLine($"[SyncManager] Syncing Tech Rating: ID={ticketId}, Score={score}");

                    string sql = @"
                        UPDATE tickets 
                        SET tech_rating_score = @Score, 
                            tech_rating_note = @Comment
                        WHERE ticket_id = @TicketId";

                    try
                    {
                        var affected = connection.Execute(sql, new { TicketId = ticketId, Score = score, Comment = comment });
                        return affected > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SyncManager] SQL Error in Tech Rating Sync: {ex.Message}");
                        throw;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[SyncManager] Unknown payload structure for tickets table: {item.PayloadJson}");
                return false;
            }
        }

        private bool ProcessPartRequestSync(SyncQueueItem item)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                var json = Newtonsoft.Json.Linq.JObject.Parse(item.PayloadJson);
                
                if (json.ContainsKey("RequestId"))
                {
                    var requestId = json["RequestId"].ToObject<int>();

                    string sql = @"
                        UPDATE part_requests 
                        SET status_id = 2, ready_at = NOW() 
                        WHERE request_id = @Id";

                    var affected = connection.Execute(sql, new { Id = requestId });
                    return affected > 0;
                }

                return false;
            }
        }

        /// <summary>
        /// Handles a failed sync item - either increment retry or move to dead letter.
        /// </summary>
        private void HandleFailedItem(SyncQueueItem item, string errorMessage)
        {
            // Check if we've exceeded max retries
            if (item.RetryCount >= OfflineRepository.MAX_RETRY_COUNT)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncManager] Max retries exceeded for item {item.Id}, moving to dead letter");
                _offlineRepo.MoveToDeadLetter(item, errorMessage);
                
                // Fire event for UI notification
                OnDeadLetterMoved?.Invoke(this, new DeadLetterEventArgs(
                    item.TableName, 
                    item.ActionType, 
                    $"Failed after {OfflineRepository.MAX_RETRY_COUNT} attempts: {errorMessage}"
                ));
            }
            else
            {
                _offlineRepo.IncrementRetryCount(item.Id);
            }
        }

        /// <summary>
        /// Forces an immediate sync attempt.
        /// </summary>
        public void SyncNow()
        {
            TrySyncAsync(null);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _networkMonitor.OnStatusChanged -= OnNetworkStatusChanged;
                _syncTimer?.Dispose();
            }
        }
    }

    public enum SyncStatus
    {
        Idle,
        Syncing,
        Complete,
        PartialComplete,
        Failed
    }

    public class SyncStatusEventArgs : EventArgs
    {
        public SyncStatus Status { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }

        public SyncStatusEventArgs(SyncStatus status, string message)
        {
            Status = status;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    public class DeadLetterEventArgs : EventArgs
    {
        public string TableName { get; }
        public string ActionType { get; }
        public string ErrorMessage { get; }
        public DateTime Timestamp { get; }

        public DeadLetterEventArgs(string tableName, string actionType, string errorMessage)
        {
            TableName = tableName;
            ActionType = actionType;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.Now;
        }
    }
}
