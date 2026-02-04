using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using mtc_app.shared.data.dtos;
using mtc_app.shared.data.local;
using mtc_app.shared.data.repositories;
using mtc_app.shared.data.services;

namespace mtc_app.shared.data.decorators
{
    /// <summary>
    /// Offline-aware decorator for MasterDataRepository.
    /// Falls back to cached data when network is unavailable.
    /// </summary>
    public class OfflineMasterDataRepository : IMasterDataRepository
    {
        private readonly IMasterDataRepository _innerRepository;
        private readonly OfflineRepository _offlineRepo;
        private readonly NetworkMonitor _networkMonitor;

        public OfflineMasterDataRepository(
            IMasterDataRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor)
        {
            _innerRepository = innerRepository;
            _offlineRepo = offlineRepo;
            _networkMonitor = networkMonitor;
        }

        public async Task<List<CachedMachineDto>> GetMachinesAsync()
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetMachinesAsync();
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineMasterData] Network error, using cache: {ex.Message}");
                }
            }

            // Return from cache
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using cached machines");
            return _offlineRepo.GetMachinesFromCache();
        }

        public async Task<List<CachedShiftDto>> GetShiftsAsync()
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetShiftsAsync();
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineMasterData] Network error, using cache: {ex.Message}");
                }
            }

            // Return from cache
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using cached shifts");
            return _offlineRepo.GetShiftsFromCache();
        }

        public async Task<List<CachedProblemTypeDto>> GetProblemTypesAsync()
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetProblemTypesAsync();
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineMasterData] Network error, using cache: {ex.Message}");
                }
            }

            // Return from cache
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using cached problem types");
            return _offlineRepo.GetProblemTypesFromCache();
        }

        public async Task<List<string>> GetOperatorsAsync()
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetOperatorsAsync();
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineMasterData] Network error, using cache: {ex.Message}");
                }
            }

            // Return from cache (Role ID 1 = Operator)
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using cached operators");
            return _offlineRepo.GetUsersByRole(1);
        }

        public async Task<List<CachedFailureDto>> GetFailuresAsync()
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetFailuresAsync();
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineMasterData] Network error, using cache: {ex.Message}");
                }
            }

            // Return from cache
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using cached failures");
            return _offlineRepo.GetFailuresFromCache();
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
            
            return ex.InnerException != null && IsNetworkException(ex.InnerException);
        }
    }
}
