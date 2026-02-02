using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    /// <summary>
    /// Base form with offline support status indicator.
    /// All feature forms should inherit from this.
    /// </summary>
    public class AppBaseForm : Form
    {
        // Shared singleton instances for offline support
        private static OfflineRepository _offlineRepo;
        private static NetworkMonitor _networkMonitor;
        private static SyncManager _syncManager;
        private static bool _servicesInitialized = false;

        // UI Components
        protected StatusStrip statusStrip;
        protected ToolStripStatusLabel lblConnectionStatus;
        protected ToolStripStatusLabel lblPendingSync;

        public AppBaseForm()
        {
            this.Font = AppFonts.Body;
            this.BackColor = AppColors.Background;
            this.ForeColor = AppColors.TextPrimary;

            InitializeOfflineServices();
            InitializeStatusBar();
            SubscribeToEvents();
        }

        /// <summary>
        /// Initializes shared offline services (singleton pattern).
        /// </summary>
        private void InitializeOfflineServices()
        {
            if (!_servicesInitialized)
            {
                _offlineRepo = new OfflineRepository();
                _networkMonitor = new NetworkMonitor();
                _syncManager = new SyncManager(_offlineRepo, _networkMonitor);
                _servicesInitialized = true;
            }
        }

        /// <summary>
        /// Creates the status bar with connection indicator.
        /// </summary>
        private void InitializeStatusBar()
        {
            statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                BackColor = AppColors.Surface,
                SizingGrip = false
            };

            // Connection status label
            lblConnectionStatus = new ToolStripStatusLabel
            {
                Text = "Checking connection...",
                ForeColor = AppColors.TextSecondary,
                Font = AppFonts.Caption,
                Image = null,
                Margin = new Padding(5, 2, 20, 2)
            };

            // Pending sync count
            lblPendingSync = new ToolStripStatusLabel
            {
                Text = "",
                ForeColor = AppColors.TextSecondary,
                Font = AppFonts.Caption,
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            statusStrip.Items.Add(lblConnectionStatus);
            statusStrip.Items.Add(lblPendingSync);

            this.Controls.Add(statusStrip);

            // Initial status check
            UpdateConnectionStatus(_networkMonitor.CheckNow());
        }

        private void SubscribeToEvents()
        {
            _networkMonitor.OnStatusChanged += NetworkMonitor_OnStatusChanged;
            _syncManager.OnSyncStatusChanged += SyncManager_OnSyncStatusChanged;
        }

        private void NetworkMonitor_OnStatusChanged(object sender, NetworkStatusEventArgs e)
        {
            // Marshal to UI thread
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateConnectionStatus(e.IsOnline)));
            }
            else
            {
                UpdateConnectionStatus(e.IsOnline);
            }
        }

        private void SyncManager_OnSyncStatusChanged(object sender, SyncStatusEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateSyncStatus(e)));
            }
            else
            {
                UpdateSyncStatus(e);
            }
        }

        private void UpdateConnectionStatus(bool isOnline)
        {
            if (isOnline)
            {
                lblConnectionStatus.Text = "● Online";
                lblConnectionStatus.ForeColor = AppColors.Success;
            }
            else
            {
                lblConnectionStatus.Text = "● Offline (Changes Saved Locally)";
                lblConnectionStatus.ForeColor = AppColors.Warning;
            }

            // Update pending count
            int pending = _syncManager?.PendingCount ?? 0;
            if (pending > 0)
            {
                lblPendingSync.Text = $"Pending: {pending} item(s)";
                lblPendingSync.ForeColor = AppColors.Warning;
            }
            else
            {
                lblPendingSync.Text = "";
            }
        }

        private void UpdateSyncStatus(SyncStatusEventArgs e)
        {
            switch (e.Status)
            {
                case SyncStatus.Syncing:
                    lblPendingSync.Text = e.Message;
                    lblPendingSync.ForeColor = AppColors.Info;
                    break;
                case SyncStatus.Complete:
                    lblPendingSync.Text = "All synced ✓";
                    lblPendingSync.ForeColor = AppColors.Success;
                    break;
                case SyncStatus.PartialComplete:
                case SyncStatus.Failed:
                    lblPendingSync.Text = e.Message;
                    lblPendingSync.ForeColor = AppColors.Warning;
                    break;
                default:
                    lblPendingSync.Text = "";
                    break;
            }
        }

        /// <summary>
        /// Gets the shared OfflineRepository for derived forms.
        /// </summary>
        protected static OfflineRepository OfflineRepo => _offlineRepo;

        /// <summary>
        /// Gets the shared NetworkMonitor for derived forms.
        /// </summary>
        protected static NetworkMonitor NetworkMon => _networkMonitor;

        /// <summary>
        /// Gets the shared SyncManager for derived forms.
        /// </summary>
        protected static SyncManager SyncMgr => _syncManager;

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Unsubscribe to prevent memory leaks
            if (_networkMonitor != null)
                _networkMonitor.OnStatusChanged -= NetworkMonitor_OnStatusChanged;
            if (_syncManager != null)
                _syncManager.OnSyncStatusChanged -= SyncManager_OnSyncStatusChanged;

            base.OnFormClosed(e);
        }
    }
}
