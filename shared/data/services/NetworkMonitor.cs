using System;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace mtc_app.shared.data.services
{
    /// <summary>
    /// Monitors network connectivity to the MariaDB server.
    /// Uses TcpClient to verify actual database reachability (intranet-safe).
    /// </summary>
    public class NetworkMonitor : IDisposable
    {
        private readonly string _dbHost;
        private readonly int _dbPort;
        private readonly int _checkIntervalMs;
        private readonly int _connectionTimeoutMs;
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
        /// Creates a new NetworkMonitor that checks DB connectivity.
        /// Parses host/port from the connection string.
        /// </summary>
        /// <param name="checkIntervalMs">Interval between checks (default: 10s)</param>
        /// <param name="connectionTimeoutMs">TCP connect timeout (default: 1.5s for responsive UI)</param>
        public NetworkMonitor(int checkIntervalMs = 10000, int connectionTimeoutMs = 1500)
        {
            _checkIntervalMs = checkIntervalMs;
            _connectionTimeoutMs = connectionTimeoutMs;
            _isOnline = false;

            // Parse host and port from DatabaseHelper connection string
            (_dbHost, _dbPort) = ParseConnectionString(DatabaseHelper.ConnectionString);

            System.Diagnostics.Debug.WriteLine($"[NetworkMonitor] Target: {_dbHost}:{_dbPort}");

            // Start periodic check timer
            _checkTimer = new Timer(CheckConnectivity, null, 0, _checkIntervalMs);
        }

        /// <summary>
        /// Parses Server and Port from MySQL connection string.
        /// Format: "Server=localhost;Port=3306;Database=...;User=...;Password=..."
        /// </summary>
        private (string host, int port) ParseConnectionString(string connectionString)
        {
            string host = "localhost";
            int port = 3306; // Default MySQL/MariaDB port

            if (string.IsNullOrEmpty(connectionString))
                return (host, port);

            // Extract Server/Host
            var serverMatch = Regex.Match(connectionString, @"Server\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            if (!serverMatch.Success)
                serverMatch = Regex.Match(connectionString, @"Host\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            if (serverMatch.Success)
                host = serverMatch.Groups[1].Value.Trim();

            // Extract Port
            var portMatch = Regex.Match(connectionString, @"Port\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (portMatch.Success)
                int.TryParse(portMatch.Groups[1].Value, out port);

            return (host, port);
        }

        private void CheckConnectivity(object state)
        {
            // Run on background thread, non-blocking to UI
            _ = CheckConnectivityAsync();
        }

        /// <summary>
        /// Async connectivity check with short timeout.
        /// </summary>
        private async Task CheckConnectivityAsync()
        {
            bool wasOnline = _isOnline;
            _isOnline = await TryConnectAsync(_dbHost, _dbPort);

            if (wasOnline != _isOnline)
            {
                System.Diagnostics.Debug.WriteLine($"[NetworkMonitor] Status changed: {(wasOnline ? "Online" : "Offline")} -> {(_isOnline ? "Online" : "Offline")}");
                OnStatusChanged?.Invoke(this, new NetworkStatusEventArgs(_isOnline));
            }
        }

        /// <summary>
        /// Attempts TCP connection with strict timeout using Task.WhenAny pattern.
        /// This is completely non-blocking and respects the timeout strictly.
        /// </summary>
        private async Task<bool> TryConnectAsync(string host, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    var timeoutTask = Task.Delay(_connectionTimeoutMs);
                    
                    // Wait for connection OR timeout - whichever happens first
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == connectTask && client.Connected)
                    {
                        return true; // Connected successfully
                    }
                    
                    return false; // Timeout or failed
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NetworkMonitor] Connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns the CACHED status instantly (non-blocking).
        /// Use this for UI decisions. The background timer keeps it updated.
        /// </summary>
        public bool CheckNow()
        {
            // Return cached value immediately - no blocking!
            // The background timer will update _isOnline periodically.
            return _isOnline;
        }

        /// <summary>
        /// Forces an immediate async connectivity check.
        /// Call this if you need fresh status and can await.
        /// </summary>
        public async Task<bool> CheckNowAsync()
        {
            bool wasOnline = _isOnline;
            _isOnline = await TryConnectAsync(_dbHost, _dbPort);

            if (wasOnline != _isOnline)
            {
                OnStatusChanged?.Invoke(this, new NetworkStatusEventArgs(_isOnline));
            }

            return _isOnline;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
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
