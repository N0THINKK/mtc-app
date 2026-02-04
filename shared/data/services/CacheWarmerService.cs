using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.data.local;

namespace mtc_app.shared.data.services
{
    /// <summary>
    /// Background service that warms the local cache when the app starts.
    /// Caches active tickets and recent machine history for offline access.
    /// </summary>
    public class CacheWarmerService : IDisposable
    {
        private readonly OfflineRepository _offlineRepo;
        private readonly NetworkMonitor _networkMonitor;
        private readonly Timer _refreshTimer;
        private readonly int _refreshIntervalMs;
        private bool _isWarming;
        private bool _disposed;

        private const int HISTORY_DAYS = 90; // Cache last 90 days of history

        public event EventHandler<CacheWarmEventArgs> OnCacheWarmCompleted;

        /// <summary>
        /// Creates a new CacheWarmerService.
        /// </summary>
        /// <param name="offlineRepo">The offline repository for caching.</param>
        /// <param name="networkMonitor">The network monitor to check connectivity.</param>
        /// <param name="refreshIntervalMs">Interval for cache refresh (default: 15 min).</param>
        public CacheWarmerService(OfflineRepository offlineRepo, NetworkMonitor networkMonitor, int refreshIntervalMs = 900000)
        {
            _offlineRepo = offlineRepo;
            _networkMonitor = networkMonitor;
            _refreshIntervalMs = refreshIntervalMs;
            _isWarming = false;

            // Subscribe to network status changes - warm cache when we come online
            _networkMonitor.OnStatusChanged += OnNetworkStatusChanged;

            // Start refresh timer
            _refreshTimer = new Timer(TryWarmCache, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Starts the cache warming service.
        /// </summary>
        public void Start()
        {
            // Initial warm on startup
            TryWarmCache(null);

            // Schedule periodic refresh
            _refreshTimer.Change(_refreshIntervalMs, _refreshIntervalMs);
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusEventArgs e)
        {
            if (e.IsOnline)
            {
                // Network came back - warm the cache
                TryWarmCache(null);
            }
        }

        private void TryWarmCache(object state)
        {
            if (_isWarming || !_networkMonitor.IsOnline)
                return;

            Task.Run(async () =>
            {
                try
                {
                    _isWarming = true;
                    System.Diagnostics.Debug.WriteLine("[CacheWarmer] Starting cache warm...");

                    int ticketsCached = await WarmTicketCacheAsync();
                    int historyCached = await WarmHistoryCacheAsync();

                    System.Diagnostics.Debug.WriteLine($"[CacheWarmer] Complete: {ticketsCached} tickets, {historyCached} history records");

                    OnCacheWarmCompleted?.Invoke(this, new CacheWarmEventArgs(ticketsCached, historyCached));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheWarmer] Error: {ex.Message}");
                }
                finally
                {
                    _isWarming = false;
                }
            });
        }

        /// <summary>
        /// Warms the ticket cache with Open and Repairing tickets.
        /// </summary>
        private async Task<int> WarmTicketCacheAsync()
        {
            try
            {
                // Create a fresh repository for this operation
                var repo = new TechnicianRepository();
                
                // GetActiveTickets returns Open and Repairing tickets
                var tickets = repo.GetActiveTickets()?.ToList() ?? new List<TicketDto>();

                if (tickets.Count > 0)
                {
                    _offlineRepo.SaveTicketsToCache(
                        tickets,
                        t => t.TicketId
                    );
                }

                return tickets.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheWarmer] Ticket cache error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Warms the machine history cache with last 90 days of data.
        /// </summary>
        private async Task<int> WarmHistoryCacheAsync()
        {
            try
            {
                var repo = new MachineHistoryRepository();
                
                var startDate = DateTime.Now.AddDays(-HISTORY_DAYS);
                var endDate = DateTime.Now;

                var history = await repo.GetHistoryAsync(startDate, endDate);
                var historyList = history?.ToList() ?? new List<MachineHistoryDto>();

                if (historyList.Count > 0)
                {
                    _offlineRepo.SaveHistoryToCache(
                        historyList,
                        h => h.TicketId
                    );
                }

                return historyList.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheWarmer] History cache error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Forces an immediate cache refresh.
        /// </summary>
        public void RefreshNow()
        {
            TryWarmCache(null);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _networkMonitor.OnStatusChanged -= OnNetworkStatusChanged;
                _refreshTimer?.Dispose();
            }
        }
    }

    public class CacheWarmEventArgs : EventArgs
    {
        public int TicketsCached { get; }
        public int HistoryCached { get; }
        public DateTime Timestamp { get; }

        public CacheWarmEventArgs(int ticketsCached, int historyCached)
        {
            TicketsCached = ticketsCached;
            HistoryCached = historyCached;
            Timestamp = DateTime.Now;
        }
    }
}
