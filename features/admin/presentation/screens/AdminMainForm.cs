using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.admin.data.repositories;
using mtc_app.features.admin.presentation.controllers;
using mtc_app.features.admin.presentation.views;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.screens
{
    public partial class AdminMainForm : AppBaseForm, IAdminMainView
    {
        private Panel pnlSidebar;
        private Panel pnlContent;
        private IAdminRepository _repository;
        private AdminMainController _controller;
        
        // Views
        private MonitoringView _monitoringView;
        private MasterDataView _masterDataView;
        private ReportView _reportView;
        private BackupView _backupView;
        private NgPatrolAdminView _ngPatrolAdminView;

        private Button _activeMenuButton;
        private List<Button> _menuButtons = new List<Button>();
        
        public AdminMainForm()
        {
            _controller = new AdminMainController(this);
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
            _controller.NavigateTo(_monitoringView);
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
            _ngPatrolAdminView = new NgPatrolAdminView(_repository);
        }

        private void SetupUI()
        {
            this.Size = new Size(1280, 800);
            this.Text = "Manis - Administrator Dashboard";
            this.BackColor = AppColors.Surface;

            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 260, BackColor = AppColors.CardBackground };
            pnlSidebar.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 1, BackColor = AppColors.Separator });
            
            Panel pnlBrand = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(24, 30, 24, 20) };
            pnlBrand.Controls.Add(new AppLabel { Text = "Manis Admin", Type = AppLabel.LabelType.Header2, ForeColor = AppColors.PrimaryDark, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            pnlSidebar.Controls.Add(pnlBrand);

            var flowMenu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12, 10, 12, 0), AutoScroll = true };
            pnlSidebar.Controls.Add(flowMenu);
            flowMenu.BringToFront();

            AddMenuButton("📊 Monitoring Widget", flowMenu, _monitoringView, () => _monitoringView.OnViewLoad());
            
            AddDropdownMenu("📂 Master Data", flowMenu, new Dictionary<string, Action>
            {
                { "👤 Data User", () => { _controller.NavigateTo(_masterDataView); _masterDataView.LoadCategory("User"); } },
                { "⚙️ Data Mesin", () => { _controller.NavigateTo(_masterDataView); _masterDataView.LoadCategory("Mesin"); } },
                { "🗺️ Area Mesin", () => { _controller.NavigateTo(_masterDataView); _masterDataView.LoadCategory("Area Mesin"); } },
                { "🔧 Data Sparepart", () => { _controller.NavigateTo(_masterDataView); _masterDataView.LoadCategory("Sparepart"); } },
                { "⚠️ Data Problem", () => { _controller.NavigateTo(_masterDataView); _masterDataView.LoadCategory("Problem"); } },
                { "📋 Data Checksheet", () => { _controller.NavigateTo(_masterDataView); _masterDataView.LoadCategory("Checksheet"); } },
                { "🎯 Target Output", () => { _controller.NavigateTo(_masterDataView); _masterDataView.LoadCategory("Target"); } },
                { "⏱️ Data Waktu Break", () => { _controller.NavigateTo(_masterDataView); _masterDataView.LoadCategory("Waktu Break"); } }
            });

            AddMenuButton("🚨 Data NG Cutting", flowMenu, _ngPatrolAdminView, () => _ngPatrolAdminView.LoadData());
            AddMenuButton("🖨️ Laporan / Export", flowMenu, _reportView);
            AddMenuButton("💾 Backup Database", flowMenu, _backupView);
            
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(24, 0, 24, 24) };
            AppButton btnLogout = new AppButton { Text = "🚪 Logout", Type = AppButton.ButtonType.Outline, Dock = DockStyle.Fill, ForeColor = AppColors.Danger };
            btnLogout.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnLogout);
            pnlSidebar.Controls.Add(pnlFooter);

            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = AppColors.Surface, Padding = new Padding(0) };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
        }

        private Button CreateSidebarButton(string text, int width, bool isSubMenu = false)
        {
            Button btn = new Button
            {
                Text = text,
                Width = width,
                Height = isSubMenu ? 45 : 50,
                Margin = new Padding(0, 0, 0, isSubMenu ? 0 : 8),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(isSubMenu ? 45 : 16, 0, 0, 0),
                Font = isSubMenu ? AppFonts.BodySmall : AppFonts.Body,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = AppColors.TextSecondary,
                Cursor = Cursors.Hand,
                TabStop = false
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
                _controller.NavigateTo(targetView);
                onLoadAction?.Invoke();
            };

            _menuButtons.Add(btn);
            parent.Controls.Add(btn);
        }

        private void AddDropdownMenu(string text, FlowLayoutPanel parent, Dictionary<string, Action> subMenuDict)
        {
            int btnWidth = parent.Width - parent.Padding.Left - parent.Padding.Right - 5;
            Button btnMain = CreateSidebarButton(text + "   ▼", btnWidth);
            parent.Controls.Add(btnMain);

            List<Button> subButtons = new List<Button>();

            foreach (var item in subMenuDict)
            {
                Button btnSub = CreateSidebarButton(item.Key, btnWidth, isSubMenu: true);
                btnSub.Visible = false;

                btnSub.Click += (s, e) => 
                {
                    SetActiveMenu(btnSub);
                    item.Value.Invoke();
                };

                _menuButtons.Add(btnSub);
                subButtons.Add(btnSub);
                parent.Controls.Add(btnSub);
            }

            btnMain.Click += (s, e) => 
            {
                bool isCurrentlyHidden = !subButtons[0].Visible;
                btnMain.Text = isCurrentlyHidden ? text + "   ▲" : text + "   ▼";
                btnMain.BackColor = isCurrentlyHidden ? AppColors.SurfaceHover : Color.Transparent;
                btnMain.Margin = new Padding(0, 0, 0, isCurrentlyHidden ? 0 : 8);

                foreach (var sub in subButtons)
                {
                    sub.Visible = isCurrentlyHidden;
                }
            };
        }

        private void SetActiveMenu(Button clickedButton)
        {
            foreach (var btn in _menuButtons)
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = AppColors.TextSecondary;
                btn.Font = new Font(AppFonts.Body.FontFamily, btn.Font.Size, FontStyle.Regular);
            }

            _activeMenuButton = clickedButton;
            _activeMenuButton.BackColor = AppColors.PrimaryLight;
            _activeMenuButton.ForeColor = AppColors.PrimaryDark;
            _activeMenuButton.Font = new Font(_activeMenuButton.Font.FontFamily, _activeMenuButton.Font.Size, FontStyle.Bold);
        }

        // ==========================================
        // IAdminMainView Implementation
        // ==========================================

        public void LoadView(UserControl view)
        {            
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => LoadView(view)));
                return;
            }
            pnlContent.Controls.Clear();
            view.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(view);
        }
    }
}