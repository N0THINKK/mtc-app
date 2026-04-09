using System;
using System.Threading.Tasks;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.shared.data.decorators
{
    /// <summary>
    /// Base class for repository decorators that add offline support.
    /// Uses the Decorator pattern to intercept failed write operations
    /// and queue them for later synchronization.
    /// </summary>
    public abstract class OfflineAwareRepositoryBase
    {
        protected readonly OfflineRepository _offlineRepo;
        protected readonly NetworkMonitor _networkMonitor;

        protected OfflineAwareRepositoryBase(OfflineRepository offlineRepo, NetworkMonitor networkMonitor)
        {
            _offlineRepo = offlineRepo ?? throw new ArgumentNullException(nameof(offlineRepo));
            _networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
        }

        /// <summary>
        /// Executes a write operation with offline fallback.
        /// If online, attempts the operation directly.
        /// If offline or operation fails, queues the operation for later sync.
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="actionType">Type of action (INSERT, UPDATE, DELETE)</param>
        /// <param name="tableName">Target table name for sync</param>
        /// <param name="payload">Data payload to serialize</param>
        /// <param name="defaultValue">Default value to return if queued offline</param>
        /// <returns>Operation result or default value</returns>
        protected async Task<T> ExecuteWithOfflineFallbackAsync<T>(
            Func<Task<T>> operation,
            string actionType,
            string tableName,
            object payload,
            T defaultValue = default)
        {
            // If we know we're offline, queue immediately
            if (!_networkMonitor.IsOnline)
            {
                QueueOperation(actionType, tableName, payload);
                return defaultValue;
            }

            try
            {
                // Try the actual operation
                return await operation();
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                // Network error - queue for later
                System.Diagnostics.Debug.WriteLine($"[OfflineDecorator] Network error, queuing: {ex.Message}");
                QueueOperation(actionType, tableName, payload);
                return defaultValue;
            }
            // Let other exceptions bubble up
        }

        /// <summary>
        /// Executes a void write operation with offline fallback.
        /// </summary>
        protected async Task ExecuteWithOfflineFallbackAsync(
            Func<Task> operation,
            string actionType,
            string tableName,
            object payload)
        {
            if (!_networkMonitor.IsOnline)
            {
                QueueOperation(actionType, tableName, payload);
                return;
            }

            try
            {
                await operation();
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineDecorator] Network error, queuing: {ex.Message}");
                QueueOperation(actionType, tableName, payload);
            }
        }

        private void QueueOperation(string actionType, string tableName, object payload)
        {
            _offlineRepo.AddToQueue(actionType, tableName, payload);
            System.Diagnostics.Debug.WriteLine($"[OfflineDecorator] Queued: {actionType} on {tableName}");
        }

        /// <summary>
        /// Determines if an exception is network-related.
        /// </summary>
        protected virtual bool IsNetworkException(Exception ex)
        {
            // Check common network exception types
            if (ex is System.Net.Sockets.SocketException) return true;
            if (ex is TimeoutException) return true;
            
            // MySQL-specific connection errors
            if (ex.GetType().Name.Contains("MySql") && 
                (ex.Message.Contains("Unable to connect") ||
                 ex.Message.Contains("Connection refused") ||
                 ex.Message.Contains("timeout")))
            {
                return true;
            }

            // Check inner exceptions
            if (ex.InnerException != null)
            {
                return IsNetworkException(ex.InnerException);
            }

            return false;
        }
    }
}
