using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace mtc_app.shared.data.services
{
    /// <summary>
    /// Monitors network connectivity to the database server.
    /// Uses actual ping to verify real connectivity, not just local network status.
    /// </summary>
    public class NetworkMonitor : IDisposable
    {
        private readonly string _targetHost;
        private readonly int _checkIntervalMs;
        private readonly Timer _checkTimer;
        private bool _isOnline;
        private bool _disposed;

        /// <summary>
        /// Fired when the connection status changes.
        /// </summary>
        public event EventHandler<NetworkStatusEventArgs> OnStatusChanged;

        /// <summary>
        /// Gets the current online status.
        /// </summary>
        public bool IsOnline => _isOnline;

        /// <summary>
        /// Creates a new NetworkMonitor.
        /// </summary>
        /// <param name="targetHost">Host to ping (default: Google DNS 8.8.8.8)</param>
        /// <param name="checkIntervalMs">Interval between checks in milliseconds (default: 10s)</param>
        public NetworkMonitor(string targetHost = "8.8.8.8", int checkIntervalMs = 10000)
        {
            _targetHost = targetHost;
            _checkIntervalMs = checkIntervalMs;
            _isOnline = false;
            
            // Subscribe to system network changes for immediate detection
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            
            // Start periodic check timer for more reliable detection
            _checkTimer = new Timer(CheckConnectivity, null, 0, _checkIntervalMs);
        }

        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            // Trigger an immediate check when network status changes
            CheckConnectivity(null);
        }

        private void CheckConnectivity(object state)
        {
            Task.Run(() =>
            {
                bool wasOnline = _isOnline;
                _isOnline = PingHost(_targetHost);
                
                if (wasOnline != _isOnline)
                {
                    OnStatusChanged?.Invoke(this, new NetworkStatusEventArgs(_isOnline));
                }
            });
        }

        private bool PingHost(string host)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(host, 3000); // 3 second timeout
                    return reply?.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Forces an immediate connectivity check.
        /// </summary>
        public bool CheckNow()
        {
            _isOnline = PingHost(_targetHost);
            return _isOnline;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
                _checkTimer?.Dispose();
            }
        }
    }

    /// <summary>
    /// Event args for network status changes.
    /// </summary>
    public class NetworkStatusEventArgs : EventArgs
    {
        public bool IsOnline { get; }
        public DateTime Timestamp { get; }

        public NetworkStatusEventArgs(bool isOnline)
        {
            IsOnline = isOnline;
            Timestamp = DateTime.Now;
        }
    }
}
