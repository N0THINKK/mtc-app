using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.admin.data.repositories;
using mtc_app.features.admin.presentation.views;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.screens
{
    public partial class AdminMainForm : AppBaseForm
    {
        private Panel pnlSidebar;
        private Panel pnlContent;
        private IAdminRepository _repository;
        
        // Views
        private MonitoringView _monitoringView;
        private MasterDataView _masterDataView;
        private ReportView _reportView;
        private BackupView _backupView;

        public AdminMainForm()
        {
            // We use manual UI setup (Clean Code) instead of Designer-generated code for better control
            // InitializeComponent(); // Disable Designer code
            SetupUI();
            
            InitializeServices();
            InitializeViews(); 
            
            // Allow maximizing
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Maximized;

            // Load default View
            LoadView(_monitoringView);
            // Trigger Load
            _monitoringView.OnViewLoad();
        }

        private void InitializeServices()
        {
            // Simple Manual Dependency Injection (Composition Root)
            // In a real app complexity, use Microsoft.Extensions.DependencyInjection
            _repository = new AdminRepository();
        }

        private void InitializeViews()
        {
            // Inject Repository
            _monitoringView = new MonitoringView(_repository);
            
            // TODO: Refactor these views next to use DI as well
            _masterDataView = new MasterDataView(); 
            _reportView = new ReportView();
            _backupView = new BackupView();
        }

        private void SetupUI()
        {
            this.Size = new Size(1280, 800);
            this.Text = "MTC System - Administrator Dashboard";
            this.BackColor = AppColors.Surface;

            // 1. Sidebar
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Color.White
            };
            
            // Sidebar Header
            AppLabel lblBrand = new AppLabel 
            {
                Text = "MTC Admin",
                Type = AppLabel.LabelType.Header2,
                Location = new Point(20, 20),
                AutoSize = true
            };
            pnlSidebar.Controls.Add(lblBrand);

            // Menu Buttons
            int startY = 80;
            AddMenuButton("Monitoring Widget", startY, () => {
                LoadView(_monitoringView);
                _monitoringView.OnViewLoad();
            });
            
            AddMenuButton("Master Data", startY + 60, () => LoadView(_masterDataView));
            AddMenuButton("Laporan / Export", startY + 120, () => LoadView(_reportView));
            AddMenuButton("Backup Database", startY + 180, () => LoadView(_backupView));
            
            // Logout
            AppButton btnLogout = new AppButton
            {
                Text = "Logout",
                Type = AppButton.ButtonType.Danger,
                Width = 210,
                Location = new Point(20, this.ClientSize.Height - 80),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnLogout.Click += (s, e) => this.Close();
            pnlSidebar.Controls.Add(btnLogout);

            // 2. Content Panel
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.Surface,
                Padding = new Padding(20)
            };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
        }

        private void AddMenuButton(string text, int y, Action onClick)
        {
            AppButton btn = new AppButton
            {
                Text = text,
                Type = AppButton.ButtonType.Secondary, // Or Ghost/Outline style if available
                Width = 210,
                Height = 45,
                Location = new Point(20, y)
            };
            // Simple styling for menu
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Click += (s, e) => onClick?.Invoke();
            pnlSidebar.Controls.Add(btn);
        }

        private void LoadView(UserControl view)
        {
            pnlContent.Controls.Clear();
            
            // Unload previous view logic if needed (e.g. stop timers)
            // But we don't track previous view easily here without state. 
            // Ideally: _currentView?.OnViewUnload();

            view.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(view);
        }
    }
}
