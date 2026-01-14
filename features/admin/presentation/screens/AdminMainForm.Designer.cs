using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.screens
{
    partial class AdminMainForm
    {
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlSidebar = new Panel();
            this.pnlContent = new Panel();
            this.pnlHeader = new Panel();
            this.lblHeaderTitle = new Label();
            this.logoBox = new PictureBox();

            this.btnMenuDashboard = new Button();
            this.btnMenuMaster = new Button();
            this.btnMenuBackup = new Button();
            this.btnMenuLaporan = new Button();
            this.btnLogout = new Button();
            
            this.pnlSidebar.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();

            // 
            // Form
            // 
            this.Text = "Admin Control Panel";
            this.MinimumSize = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 
            // Sidebar
            // 
            this.pnlSidebar.Dock = DockStyle.Left;
            this.pnlSidebar.Width = 240;
            this.pnlSidebar.BackColor = AppColors.PrimaryDark;
            this.pnlSidebar.Controls.Add(this.logoBox);
            this.pnlSidebar.Controls.Add(this.btnMenuDashboard);
            this.pnlSidebar.Controls.Add(this.btnMenuMaster);
            this.pnlSidebar.Controls.Add(this.btnMenuLaporan);
            this.pnlSidebar.Controls.Add(this.btnMenuBackup);
            this.pnlSidebar.Controls.Add(this.btnLogout);

            // Logo
            this.logoBox.Image = null; // Placeholder for logo
            this.logoBox.BackColor = Color.FromArgb(20, 255, 255, 255); // Slightly transparent white
            this.logoBox.Dock = DockStyle.Top;
            this.logoBox.Height = 80;
            this.logoBox.Padding = new Padding(10);
            this.logoBox.SizeMode = PictureBoxSizeMode.Zoom;
            
            // --- Menu Buttons ---
            ConfigureMenuButton(btnMenuDashboard, "Dashboard", 100);
            ConfigureMenuButton(btnMenuMaster, "Data Master", 150);
            ConfigureMenuButton(btnMenuLaporan, "Laporan", 200);
            ConfigureMenuButton(btnMenuBackup, "Backup", 250);
            ConfigureMenuButton(btnLogout, "Logout", 0, true); 
            
            this.btnMenuDashboard.Click += BtnMenuDashboard_Click;
            this.btnMenuMaster.Click += BtnMenuMaster_Click;
            this.btnMenuLaporan.Click += BtnMenuLaporan_Click;
            this.btnMenuBackup.Click += BtnMenuBackup_Click;
            this.btnLogout.Click += BtnLogout_Click;

            // 
            // Header
            // 
            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Height = 60;
            this.pnlHeader.BackColor = Color.White;
            this.pnlHeader.Padding = new Padding(20, 0, 20, 0);
            this.pnlHeader.Controls.Add(this.lblHeaderTitle);

            // Header Title
            this.lblHeaderTitle.Dock = DockStyle.Fill;
            this.lblHeaderTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblHeaderTitle.ForeColor = AppColors.TextPrimary;
            this.lblHeaderTitle.TextAlign = ContentAlignment.MiddleLeft;
            
            // 
            // Content Panel
            // 
            this.pnlContent.Dock = DockStyle.Fill;
            this.pnlContent.BackColor = AppColors.Surface;
            this.pnlContent.Padding = new Padding(20);
            
            // Add controls to form in correct Z-order
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlSidebar);

            this.pnlSidebar.ResumeLayout(false);
            this.pnlHeader.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private void ConfigureMenuButton(Button btn, string text, int top, bool isBottom = false)
        {
            btn.Text = "   " + text; // Add padding for icon later
            if (isBottom) {
                btn.Dock = DockStyle.Bottom;
            } else {
                btn.Dock = DockStyle.Top;
                btn.Top = top;
            }
            btn.Height = 50;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btn.ForeColor = Color.White;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(20, 0, 0, 0);

            // Hover effects
            btn.MouseEnter += (s, e) => btn.BackColor = AppColors.Primary;
            btn.MouseLeave += (s, e) => btn.BackColor = AppColors.PrimaryDark;
        }
    }
}
