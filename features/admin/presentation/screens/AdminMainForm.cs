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

        private UserControl _currentActiveView;

        // Kita gunakan Button standar bawaan C# untuk menghindari konflik warna dari AppButton
        private Button _activeMenuButton;
        private List<Button> _menuButtons = new List<Button>();
        

        public AdminMainForm()
        {
            InitializeServices();
            InitializeViews();
            SetupUI(); 
            
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Maximized;
            this.Shown += AdminMainForm_Shown;
        }

        private async void AdminMainForm_Shown(object sender, EventArgs e)
        {
            await Task.Delay(50);
            if (_menuButtons.Count > 0) SetActiveMenu(_menuButtons[0]);
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
            _masterDataView = new MasterDataView(_repository); 
            _reportView = new ReportView();
            _backupView = new BackupView();
        }

        private void SetupUI()
        {
            this.Size = new Size(1280, 800);
            this.Text = "MTC System - Administrator Dashboard";
            this.BackColor = AppColors.Surface;

            // 1. Sidebar Panel
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 260, BackColor = AppColors.CardBackground };
            pnlSidebar.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 1, BackColor = AppColors.Separator });
            
            Panel pnlBrand = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(24, 30, 24, 20) };
            pnlBrand.Controls.Add(new AppLabel { Text = "MTC Admin", Type = AppLabel.LabelType.Header2, ForeColor = AppColors.PrimaryDark, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            pnlSidebar.Controls.Add(pnlBrand);

            var flowMenu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12, 10, 12, 0), AutoScroll = true };
            pnlSidebar.Controls.Add(flowMenu);
            flowMenu.BringToFront();

            // ==========================================
            // DEFINISI MENU & DROPDOWN
            // ==========================================
            AddMenuButton("📊 Monitoring Widget", flowMenu, _monitoringView, () => _monitoringView.OnViewLoad());
            
            AddDropdownMenu("📂 Master Data", flowMenu, new Dictionary<string, Action>
            {
                { "👤 Data User", () => { LoadView(_masterDataView); _masterDataView.LoadCategory("User"); } },
                { "⚙️ Data Mesin", () => { LoadView(_masterDataView); _masterDataView.LoadCategory("Mesin"); } },
                { "🔧 Data Sparepart", () => { LoadView(_masterDataView); _masterDataView.LoadCategory("Sparepart"); } },
                { "⚠️ Data Problem", () => { LoadView(_masterDataView); _masterDataView.LoadCategory("Problem"); } }
            });

            AddMenuButton("🖨️ Laporan / Export", flowMenu, _reportView);
            AddMenuButton("💾 Backup Database", flowMenu, _backupView);
            
            // Footer (Logout tetap pakai AppButton karena butuh border merah)
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(24, 0, 24, 24) };
            AppButton btnLogout = new AppButton { Text = "🚪 Logout", Type = AppButton.ButtonType.Outline, Dock = DockStyle.Fill, ForeColor = AppColors.Danger };
            btnLogout.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnLogout);
            pnlSidebar.Controls.Add(pnlFooter);

            // 2. Content Panel
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = AppColors.Surface, Padding = new Padding(0) };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
        }

        // ==========================================
        // HELPER: PEMBUAT TOMBOL SIDEBAR BEBAS BUG
        // ==========================================
        private Button CreateSidebarButton(string text, int width, bool isSubMenu = false)
        {
            Button btn = new Button
            {
                Text = text,
                Width = width,
                Height = isSubMenu ? 45 : 50,
                Margin = new Padding(0, 0, 0, isSubMenu ? 0 : 8), // Sub-menu tidak punya jarak agar menempel
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(isSubMenu ? 45 : 16, 0, 0, 0), // Sub-menu menjorok ke dalam
                Font = isSubMenu ? AppFonts.BodySmall : AppFonts.Body,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = AppColors.TextSecondary,
                Cursor = Cursors.Hand,
                TabStop = false // Hilangkan garis putus-putus saat difokuskan
            };
            
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = AppColors.SurfaceHover;
            btn.FlatAppearance.MouseDownBackColor = AppColors.Separator;

            return btn;
        }

        private void AddMenuButton(string text, FlowLayoutPanel parent, UserControl targetView, Action onLoadAction = null)
        {
            int btnWidth = parent.Width - parent.Padding.Left - parent.Padding.Right - 5;
            Button btn = CreateSidebarButton(text, btnWidth);

            btn.Click += (s, e) => 
            {
                SetActiveMenu(btn);
                LoadView(targetView);
                onLoadAction?.Invoke();
            };

            _menuButtons.Add(btn);
            parent.Controls.Add(btn);
        }

        private void AddDropdownMenu(string text, FlowLayoutPanel parent, Dictionary<string, Action> subMenuDict)
        {
            int btnWidth = parent.Width - parent.Padding.Left - parent.Padding.Right - 5;

            // 1. Tombol Utama
            Button btnMain = CreateSidebarButton(text + "   ▼", btnWidth);
            parent.Controls.Add(btnMain);

            // 2. Buat anak-anak menu (Sub-menus) tapi langsung disembunyikan
            List<Button> subButtons = new List<Button>();

            foreach (var item in subMenuDict)
            {
                Button btnSub = CreateSidebarButton(item.Key, btnWidth, isSubMenu: true);
                btnSub.Visible = false; // AWALNYA DISEMBUNYIKAN

                btnSub.Click += (s, e) => 
                {
                    SetActiveMenu(btnSub);
                    item.Value.Invoke();
                };

                _menuButtons.Add(btnSub);
                subButtons.Add(btnSub);
                parent.Controls.Add(btnSub);
            }

            // 3. Logika untuk memunculkan anak-anak menu saat tombol utama diklik
            btnMain.Click += (s, e) => 
            {
                bool isCurrentlyHidden = !subButtons[0].Visible;
                
                // Ubah panah
                btnMain.Text = isCurrentlyHidden ? text + "   ▲" : text + "   ▼";
                btnMain.BackColor = isCurrentlyHidden ? AppColors.SurfaceHover : Color.Transparent;
                btnMain.Margin = new Padding(0, 0, 0, isCurrentlyHidden ? 0 : 8); // Rapikan spasi

                // Tampilkan / Sembunyikan sub-menu
                foreach (var sub in subButtons)
                {
                    sub.Visible = isCurrentlyHidden;
                }
            };
        }

        private void SetActiveMenu(Button clickedButton)
        {
            // Kembalikan semua tombol ke warna abu-abu (tidak aktif)
            foreach (var btn in _menuButtons)
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = AppColors.TextSecondary;
                btn.Font = new Font(AppFonts.Body.FontFamily, btn.Font.Size, FontStyle.Regular);
            }

            // Beri warna biru untuk tombol yang sedang diklik
            _activeMenuButton = clickedButton;
            _activeMenuButton.BackColor = AppColors.PrimaryLight;
            _activeMenuButton.ForeColor = AppColors.PrimaryDark;
            _activeMenuButton.Font = new Font(_activeMenuButton.Font.FontFamily, _activeMenuButton.Font.Size, FontStyle.Bold);
        }

        private void LoadView(UserControl view)
        {            
            pnlContent.Controls.Clear();
            view.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(view);
        }
    }
}