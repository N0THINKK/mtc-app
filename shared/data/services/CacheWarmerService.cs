using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.data.local;
using mtc_app.shared.data.dtos;

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
                    int usersCached = await SyncUsersAsync();
                    var masterDataCounts = await SyncMasterDataAsync();
                    
                    // Push pending tickets
                    int ticketsSynced = await SyncPendingTicketsAsync();

                    System.Diagnostics.Debug.WriteLine($"[CacheWarmer] Complete: {ticketsCached} tickets, {historyCached} history, {usersCached} users, {ticketsSynced} synced");

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
        /// Syncs all users from remote DB to local cache for offline authentication.
        /// </summary>
        private async Task<int> SyncUsersAsync()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    
                    string sql = @"
                        SELECT 
                            u.user_id AS UserId,
                            u.username AS Username,
                            u.password AS Password,
                            u.full_name AS FullName,
                            u.nik AS Nik,
                            u.role_id AS RoleId,
                            r.role_name AS RoleName,
                            COALESCE(u.is_active, 1) AS IsActive
                        FROM users u
                        LEFT JOIN roles r ON u.role_id = r.role_id";
                    
                    var users = await conn.QueryAsync<CachedUserDto>(sql);
                    var userList = users?.ToList() ?? new List<CachedUserDto>();
                    
                    if (userList.Count > 0)
                    {
                        _offlineRepo.SaveUsersToCache(
                            userList,
                            u => u.UserId,
                            u => u.Username,
                            u => u.Password,
                            u => u.FullName,
                            u => u.Nik,
                            u => u.RoleId,
                            u => u.RoleName,
                            u => u.IsActive
                        );
                    }
                    
                    return userList.Count;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheWarmer] User sync error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Syncs master data (machines, shifts, problem types, failures) for offline form dropdowns.
        /// </summary>
        private async Task<(int machines, int shifts, int problemTypes, int failures)> SyncMasterDataAsync()
        {
            int machineCount = 0, shiftCount = 0, problemTypeCount = 0, failureCount = 0;
            int causeCount = 0, actionCount = 0; // [FIX] Defined missing variables
            
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    
                    // Sync Machines
                    var machines = await conn.QueryAsync(
                        @"SELECT machine_id AS MachineId, machine_type AS MachineType, 
                                 machine_area AS MachineArea, machine_number AS MachineNumber,
                                 current_status_id AS StatusId
                          FROM machines");
                    var machineList = machines?.ToList();
                    if (machineList?.Count > 0)
                    {
                        _offlineRepo.SaveMachinesToCache(machineList);
                        machineCount = machineList.Count;
                    }
                    
                    // Sync Shifts
                    var shifts = await conn.QueryAsync(
                        @"SELECT shift_id AS ShiftId, shift_name AS ShiftName FROM shifts");
                    var shiftList = shifts?.ToList();
                    if (shiftList?.Count > 0)
                    {
                        _offlineRepo.SaveShiftsToCache(shiftList);
                        shiftCount = shiftList.Count;
                    }
                    
                    // Sync Problem Types
                    var problemTypes = await conn.QueryAsync(
                        @"SELECT type_id AS TypeId, type_name AS TypeName FROM problem_types");
                    var problemTypeList = problemTypes?.ToList();
                    if (problemTypeList?.Count > 0)
                    {
                        _offlineRepo.SaveProblemTypesToCache(problemTypeList);
                        problemTypeCount = problemTypeList.Count;
                    }

                    // Sync Failures
                    var failures = await conn.QueryAsync("SELECT failure_id, failure_name FROM failures");
                    var failureList = failures?.ToList();
                    if (failureList?.Count > 0)
                    {
                        _offlineRepo.SaveFailuresToCache(failureList);
                        failureCount = failureList.Count;
                    }

                    // Sync Causes (NEW)
                    var causes = await conn.QueryAsync("SELECT cause_id, cause_name FROM failure_causes");
                    var causeList = causes?.ToList();
                    if (causeList?.Count > 0)
                    {
                        _offlineRepo.SaveCausesToCache(causeList);
                        causeCount = causeList.Count;
                    }

                    // Sync Actions (NEW)
                    var actions = await conn.QueryAsync("SELECT action_id, action_name FROM actions");
                    var actionList = actions?.ToList();
                    if (actionList?.Count > 0)
                    {
                        _offlineRepo.SaveActionsToCache(actionList);
                        actionCount = actionList.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheWarmer] Master data sync error: {ex.Message}");
            }
            
            return (machineCount, shiftCount, problemTypeCount, failureCount);
        }

        /// <summary>
        /// Pushes pending offline tickets to the remote server.
        /// </summary>
        private async Task<int> SyncPendingTicketsAsync()
        {
            try
            {
                var pending = _offlineRepo.GetPendingTickets();
                if (pending.Count == 0) return 0;

                int syncedCount = 0;
                var repo = new MachineHistoryRepository();

                foreach (var item in pending)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[Sync] Pushing ticket {item.Id}...");
                        await repo.CreateTicketAsync(item.Request);
                        
                        // If successful, remove from local buffer
                        _offlineRepo.DeletePendingTicket(item.Id);
                        syncedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Sync] Failed to sync ticket {item.Id}: {ex.Message}");
                    }
                }
                
                if (syncedCount > 0)
                    System.Diagnostics.Debug.WriteLine($"[Sync] Successfully pushed {syncedCount} pending tickets.");

                return syncedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Error during pending ticket sync: {ex.Message}");
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
