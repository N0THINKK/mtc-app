using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
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

        // Menyimpan status menu yang aktif
        private AppButton _activeMenuButton;
        private List<AppButton> _menuButtons = new List<AppButton>();

        public AdminMainForm()
        {
            SetupUI();
            InitializeServices();
            InitializeViews(); 
            
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Maximized;

            this.Shown += AdminMainForm_Shown;
        }

        private async void AdminMainForm_Shown(object sender, EventArgs e)
        {
            await Task.Delay(50);

            // Set default view dan aktifkan tombol pertama
            if (_menuButtons.Count > 0)
            {
                SetActiveMenu(_menuButtons[0]);
            }
            LoadView(_monitoringView);
            _monitoringView.OnViewLoad();
        }

        private void InitializeServices()
        {
            _repository = new AdminRepository();
        }

        private void InitializeViews()
        {
            _monitoringView = new MonitoringView(_repository);
            _masterDataView = new MasterDataView(); 
            _reportView = new ReportView();
            _backupView = new BackupView();
        }

        private void SetupUI()
        {
            this.Size = new Size(1280, 800);
            this.Text = "MTC System - Administrator Dashboard";
            this.BackColor = AppColors.Surface;

            // 1. Sidebar Panel (Gunakan warna gelap atau putih bersih tergantung tema. Di sini pakai putih dengan border kanan tipis)
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = AppColors.CardBackground
            };

            // Tambahkan garis pembatas halus di sisi kanan sidebar
            Panel pnlSidebarBorder = new Panel
            {
                Dock = DockStyle.Right,
                Width = 1,
                BackColor = AppColors.Separator
            };
            pnlSidebar.Controls.Add(pnlSidebarBorder);
            
            // Sidebar Header (Logo / Brand Area)
            Panel pnlBrand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(24, 30, 24, 20)
            };

            AppLabel lblBrand = new AppLabel 
            {
                Text = "Dashboard Admin",
                Type = AppLabel.LabelType.Header3,
                ForeColor = AppColors.PrimaryDark, // Gunakan warna brand
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBrand.Controls.Add(lblBrand);
            pnlSidebar.Controls.Add(pnlBrand);

            // Container untuk Menu
            var flowMenu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(12, 10, 12, 0), // Padding kiri-kanan agar tombol tidak menempel tepi
                AutoScroll = true
            };
            
            pnlSidebar.Controls.Add(flowMenu);
            flowMenu.BringToFront();

            // Tambahkan Menu Buttons
            AddMenuButton("📊 Monitoring Widget", flowMenu, _monitoringView, () => _monitoringView.OnViewLoad());
            AddMenuButton("📂 Master Data", flowMenu, _masterDataView);
            AddMenuButton("🖨️ Laporan / Export", flowMenu, _reportView);
            AddMenuButton("💾 Backup Database", flowMenu, _backupView);
            
            // Logout Button (di bagian bawah)
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                Padding = new Padding(24, 0, 24, 24)
            };

            AppButton btnLogout = new AppButton
            {
                Text = "🚪 Logout",
                Type = AppButton.ButtonType.Outline, // Menggunakan outline agar tidak terlalu mencolok seperti Danger
                Dock = DockStyle.Fill,
                ForeColor = AppColors.Danger
            };
            
            // Override warna hover untuk tombol logout
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = Color.FromArgb(20, AppColors.Danger);
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = Color.Transparent;
            
            btnLogout.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnLogout);
            pnlSidebar.Controls.Add(pnlFooter);

            // 2. Content Panel
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.Surface, // Area konten menggunakan warna surface (abu-abu terang)
                Padding = new Padding(0) // Padding diatur oleh masing-masing view (seperti di MonitoringView)
            };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
        }

        private void AddMenuButton(string text, FlowLayoutPanel parent, UserControl targetView, Action onLoadAction = null)
        {
            AppButton btn = new AppButton
            {
                Text = text,
                Width = parent.Width - parent.Padding.Left - parent.Padding.Right - 5, // Sesuaikan lebar dengan kontainer
                Height = 50,
                Margin = new Padding(0, 0, 0, 8),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Font = AppFonts.Body, // Font yang lebih modern
                FlatStyle = FlatStyle.Flat
            };

            // Styling default (Inactive State)
            SetMenuButtonInactiveStyle(btn);

            btn.Click += (s, e) => 
            {
                SetActiveMenu(btn);
                LoadView(targetView);
                onLoadAction?.Invoke();
            };

            _menuButtons.Add(btn);
            parent.Controls.Add(btn);
        }

        private void SetActiveMenu(AppButton clickedButton)
        {
            // Reset semua tombol ke style inactive
            foreach (var btn in _menuButtons)
            {
                SetMenuButtonInactiveStyle(btn);
            }

            // Set tombol yang diklik ke style active
            _activeMenuButton = clickedButton;
            _activeMenuButton.BackColor = AppColors.PrimaryLight; // Background highlight
            _activeMenuButton.ForeColor = AppColors.PrimaryDark;
            _activeMenuButton.Font = new Font(AppFonts.Body, FontStyle.Bold); // Bold saat aktif
            
            // Override hover agar tidak berubah saat sedang aktif
            _activeMenuButton.MouseEnter -= MenuButton_MouseEnter;
            _activeMenuButton.MouseLeave -= MenuButton_MouseLeave;
        }

        private void SetMenuButtonInactiveStyle(AppButton btn)
        {
            btn.BackColor = Color.Transparent; // Background transparan menyatu dengan sidebar
            btn.ForeColor = AppColors.TextSecondary; // Warna teks sekunder
            btn.Font = AppFonts.Body;
            
            // Re-attach hover events
            btn.MouseEnter -= MenuButton_MouseEnter;
            btn.MouseLeave -= MenuButton_MouseLeave;
            btn.MouseEnter += MenuButton_MouseEnter;
            btn.MouseLeave += MenuButton_MouseLeave;
        }

        private void MenuButton_MouseEnter(object sender, EventArgs e)
        {
            if (sender is AppButton btn && btn != _activeMenuButton)
            {
                btn.BackColor = AppColors.SurfaceHover; // Efek hover ringan
                btn.ForeColor = AppColors.TextPrimary;
            }
        }

        private void MenuButton_MouseLeave(object sender, EventArgs e)
        {
            if (sender is AppButton btn && btn != _activeMenuButton)
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = AppColors.TextSecondary;
            }
        }

        private void LoadView(UserControl view)
        {
            pnlContent.Controls.Clear();
            view.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(view);
        }
    }
}