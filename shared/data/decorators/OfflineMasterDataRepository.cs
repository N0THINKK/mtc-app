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

        public async Task<List<CachedCauseDto>> GetCausesAsync()
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetCausesAsync();
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineMasterData] Network error, using cache: {ex.Message}");
                }
            }

            // Return from cache
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using cached causes");
            var cached = _offlineRepo.GetCausesFromCache();
            return cached.Count > 0 ? cached : GetDefaultCauses();
        }

        public async Task<List<CachedActionDto>> GetActionsAsync()
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetActionsAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineMasterData] Error fetching actions: {ex.Message}");
                }
            }

            // Return from cache
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using cached actions");
            var cached = _offlineRepo.GetActionsFromCache();
            return cached.Count > 0 ? cached : GetDefaultActions();
        }

        public async Task<List<CachedPartDto>> GetPartsAsync()
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetPartsAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineMasterData] Error fetching parts: {ex.Message}");
                }
            }

            // Return from cache
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using cached parts");
            var cached = _offlineRepo.GetPartsFromCache();
            return cached.Count > 0 ? cached : GetDefaultParts();
        }

        #region Fallback Default Data
        
        private List<CachedCauseDto> GetDefaultCauses()
        {
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using default causes (cache empty)");
            return new List<CachedCauseDto>
            {
                new CachedCauseDto { CauseId = 1, CauseName = "Aus/Wear" },
                new CachedCauseDto { CauseId = 2, CauseName = "Kotor" },
                new CachedCauseDto { CauseId = 3, CauseName = "Longgar/Kendor" },
                new CachedCauseDto { CauseId = 4, CauseName = "Patah/Pecah" },
                new CachedCauseDto { CauseId = 5, CauseName = "Korosi/Karat" },
                new CachedCauseDto { CauseId = 6, CauseName = "Overheat" },
                new CachedCauseDto { CauseId = 7, CauseName = "Short Circuit" },
                new CachedCauseDto { CauseId = 8, CauseName = "Kesalahan Operator" },
                new CachedCauseDto { CauseId = 9, CauseName = "Lainnya" }
            };
        }

        private List<CachedActionDto> GetDefaultActions()
        {
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using default actions (cache empty)");
            return new List<CachedActionDto>
            {
                new CachedActionDto { ActionId = 1, ActionName = "Ganti Komponen" },
                new CachedActionDto { ActionId = 2, ActionName = "Perbaiki/Repair" },
                new CachedActionDto { ActionId = 3, ActionName = "Bersihkan" },
                new CachedActionDto { ActionId = 4, ActionName = "Kencangkan" },
                new CachedActionDto { ActionId = 5, ActionName = "Lubrikasi" },
                new CachedActionDto { ActionId = 6, ActionName = "Kalibrasi" },
                new CachedActionDto { ActionId = 7, ActionName = "Reset/Restart" },
                new CachedActionDto { ActionId = 8, ActionName = "Setting Ulang" },
                new CachedActionDto { ActionId = 9, ActionName = "Lainnya" }
            };
        }

        private List<CachedPartDto> GetDefaultParts()
        {
            System.Diagnostics.Debug.WriteLine("[OfflineMasterData] Using default parts (cache empty)");
            return new List<CachedPartDto>
            {
                new CachedPartDto { PartId = 1, PartCode = "SPR-001", PartName = "Bearing" },
                new CachedPartDto { PartId = 2, PartCode = "SPR-002", PartName = "Belt" },
                new CachedPartDto { PartId = 3, PartCode = "SPR-003", PartName = "Blade" },
                new CachedPartDto { PartId = 4, PartCode = "SPR-004", PartName = "Sensor" },
                new CachedPartDto { PartId = 5, PartCode = "SPR-005", PartName = "Motor" },
                new CachedPartDto { PartId = 6, PartCode = "SPR-006", PartName = "Relay" },
                new CachedPartDto { PartId = 7, PartCode = "SPR-007", PartName = "Fuse" },
                new CachedPartDto { PartId = 8, PartCode = "SPR-008", PartName = "Contactor" },
                new CachedPartDto { PartId = 9, PartCode = "LAINNYA", PartName = "Lainnya (Tulis Manual)" }
            };
        }
        
        #endregion

        private bool IsNetworkException(Exception ex)
        {
            // [Modified] Fallback to cache on ANY exception for robustness
            if (ex != null)
                System.Diagnostics.Debug.WriteLine($"[OfflineMasterData] Exception caught: {ex.Message}");
            return true; 
        }
    }
}
