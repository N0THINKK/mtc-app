using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.features.admin.presentation.views;
using mtc_app.features.authentication.presentation.screens;

namespace mtc_app.features.admin.presentation.screens
{
    public partial class AdminMainForm : AppBaseForm
    {
        private System.ComponentModel.IContainer components = null;
        
        // UI Components
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Panel pnlHeader;
        private Label lblHeaderTitle;
        private PictureBox logoBox;
        
        // Menu Buttons
        private Button btnMenuDashboard;
        private Button btnMenuMaster;
        private Button btnMenuBackup;
        private Button btnLogout;

        public AdminMainForm()
        {
            InitializeComponent();
            // Load Default View
            BtnMenuDashboard_Click(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void LoadView(UserControl view)
        {
            pnlContent.Controls.Clear();
            view.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(view);
        }

        private void BtnMenuDashboard_Click(object sender, EventArgs e)
        {
            LoadView(new MonitoringView());
            lblHeaderTitle.Text = "MONITORING DASHBOARD";
        }
        
        private void BtnMenuMaster_Click(object sender, EventArgs e)
        {
            LoadView(new MasterDataView());
            lblHeaderTitle.Text = "KELOLA DATA MASTER";
        }

        private void BtnMenuBackup_Click(object sender, EventArgs e)
        {
            LoadView(new BackupView());
            lblHeaderTitle.Text = "BACKUP DATABASE";
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            this.Hide();
            LoginForm loginForm = new LoginForm();
            loginForm.Show();
            this.Close();
        }
    }
}
