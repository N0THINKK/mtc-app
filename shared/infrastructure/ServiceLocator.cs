using mtc_app.features.group_leader.data.decorators;
using mtc_app.features.group_leader.data.repositories;
using mtc_app.features.machine_history.data.decorators;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.features.rating.data.decorators;
using mtc_app.features.rating.data.repositories;
using mtc_app.features.stock.data.decorators;
using mtc_app.features.stock.data.repositories;
using mtc_app.features.technician.data.decorators;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.shared.infrastructure
{
    /// <summary>
    /// Simple Service Locator for creating offline-aware repository instances.
    /// This centralizes the wiring of decorators without requiring a full DI container.
    /// </summary>
    public static class ServiceLocator
    {
        // Lazy singleton instances for shared services
        private static OfflineRepository _offlineRepo;
        private static NetworkMonitor _networkMonitor;
        private static SyncManager _syncManager;
        private static CacheWarmerService _cacheWarmer;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the shared OfflineRepository instance.
        /// </summary>
        public static OfflineRepository OfflineRepo
        {
            get
            {
                EnsureServicesInitialized();
                return _offlineRepo;
            }
        }

        /// <summary>
        /// Gets the shared NetworkMonitor instance.
        /// </summary>
        public static NetworkMonitor NetworkMonitor
        {
            get
            {
                EnsureServicesInitialized();
                return _networkMonitor;
            }
        }

        /// <summary>
        /// Gets the shared SyncManager instance.
        /// </summary>
        public static SyncManager SyncManager
        {
            get
            {
                EnsureServicesInitialized();
                return _syncManager;
            }
        }

        /// <summary>
        /// Gets the shared CacheWarmerService instance.
        /// </summary>
        public static CacheWarmerService CacheWarmer
        {
            get
            {
                EnsureServicesInitialized();
                return _cacheWarmer;
            }
        }

        private static void EnsureServicesInitialized()
        {
            if (_offlineRepo == null)
            {
                lock (_lock)
                {
                    if (_offlineRepo == null)
                    {
                        _offlineRepo = new OfflineRepository();
                        _networkMonitor = new NetworkMonitor();
                        _syncManager = new SyncManager(_offlineRepo, _networkMonitor);
                        _cacheWarmer = new CacheWarmerService(_offlineRepo, _networkMonitor);
                        _cacheWarmer.Start(); // Start cache warming on app start
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Repository Factory Methods
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an offline-aware GroupLeaderRepository.
        /// </summary>
        public static IGroupLeaderRepository CreateGroupLeaderRepository()
        {
            EnsureServicesInitialized();
            return new OfflineGroupLeaderRepository(
                new GroupLeaderRepository(),
                _offlineRepo,
                _networkMonitor
            );
        }

        /// <summary>
        /// Creates an offline-aware StockRepository.
        /// </summary>
        public static IStockRepository CreateStockRepository()
        {
            EnsureServicesInitialized();
            return new OfflineStockRepository(
                new StockRepository(),
                _offlineRepo,
                _networkMonitor
            );
        }

        /// <summary>
        /// Creates an offline-aware RatingRepository.
        /// </summary>
        public static IRatingRepository CreateRatingRepository()
        {
            EnsureServicesInitialized();
            return new OfflineRatingRepository(
                new RatingRepository(),
                _offlineRepo,
                _networkMonitor
            );
        }

        /// <summary>
        /// Creates an offline-aware TechnicianRepository with cached read fallback.
        /// </summary>
        public static ITechnicianRepository CreateTechnicianRepository()
        {
            EnsureServicesInitialized();
            return new OfflineTechnicianRepository(
                new TechnicianRepository(),
                _offlineRepo,
                _networkMonitor
            );
        }

        /// <summary>
        /// Creates an offline-aware MachineHistoryRepository with cached read fallback.
        /// </summary>
        public static IMachineHistoryRepository CreateMachineHistoryRepository()
        {
            EnsureServicesInitialized();
            return new OfflineMachineHistoryRepository(
                new MachineHistoryRepository(),
                _offlineRepo,
                _networkMonitor
            );
        }
    }
}
