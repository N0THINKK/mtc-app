using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using mtc_app.features.authentication.data.repositories;
using mtc_app.shared.data.dtos;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.features.authentication.data.decorators
{
    /// <summary>
    /// Offline-aware decorator for AuthRepository.
    /// Falls back to local SQLite cache when network is unavailable.
    /// </summary>
    public class OfflineAuthRepository : IAuthRepository
    {
        private readonly IAuthRepository _innerRepository;
        private readonly OfflineRepository _offlineRepo;
        private readonly NetworkMonitor _networkMonitor;

        public OfflineAuthRepository(
            IAuthRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor)
        {
            _innerRepository = innerRepository;
            _offlineRepo = offlineRepo;
            _networkMonitor = networkMonitor;
        }

        /// <summary>
        /// Attempts remote login first, falls back to offline cache on network error.
        /// </summary>
        public async Task<UserDto> LoginAsync(string username, string password)
        {
            // Try online first if network appears available
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    var user = await _innerRepository.LoginAsync(username, password);
                    if (user != null)
                    {
                        return user;
                    }
                    // User not found or wrong password - don't fall back to offline
                    // (prevents offline bypass of changed passwords)
                    return null;
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineAuth] Network error, trying offline: {ex.Message}");
                    // Fall through to offline validation
                }
            }

            // Offline fallback
            System.Diagnostics.Debug.WriteLine("[OfflineAuth] Attempting offline login...");
            var offlineUser = _offlineRepo.ValidateOfflineLogin(username, password);
            
            if (offlineUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineAuth] Offline login successful for: {username}");
                return offlineUser;
            }
            
            // No valid user found
            return null;
        }

        /// <summary>
        /// Determines if exception is network-related.
        /// </summary>
        private bool IsNetworkException(Exception ex)
        {
            if (ex == null) return false;
            
            // Check for common network exceptions
            if (ex is SocketException) return true;
            if (ex is TimeoutException) return true;
            
            // Check message for MySQL connection errors
            var message = ex.Message?.ToLowerInvariant() ?? "";
            if (message.Contains("unable to connect")) return true;
            if (message.Contains("connection refused")) return true;
            if (message.Contains("timeout")) return true;
            if (message.Contains("host")) return true;
            
            // Check inner exception
            return ex.InnerException != null && IsNetworkException(ex.InnerException);
        }
    }
}
