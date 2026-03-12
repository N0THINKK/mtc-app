using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.data.session;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.features.machine_history.presentation.screens;

namespace mtc_app.features.technician.presentation.screens
{
    public class TechnicianMainMenuForm : AppBaseForm
    {
        private Label lblWelcome;
        private Label lblSubtitle;
        private AppButton btnChecksheet;
        private AppButton btnMicrometer;
        private AppButton btnLogout;

        public TechnicianMainMenuForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Menu Utama Teknisi";
            this.Size = new Size(600, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.None; 

            string userName = UserSession.CurrentUser?.FullName ?? UserSession.CurrentUser?.Username ?? "Teknisi";

            lblWelcome = new Label
            {
                Text = $"Selamat Datang, {userName}!",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(50, 40)
            };

            lblSubtitle = new Label
            {
                Text = "Silakan pilih tugas yang ingin Anda lakukan saat ini:",
                Font = new Font("Segoe UI", 12F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(50, 80)
            };

            btnChecksheet = new AppButton
            {
                Text = "Isi Patroli Harian\n(Mesin)",
                Type = AppButton.ButtonType.Primary, 
                Size = new Size(500, 90),
                Location = new Point(50, 140),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChecksheet.Click += BtnChecksheet_Click;

            btnMicrometer = new AppButton
            {
                Text = "Isi Patroli Harian\n(Mikrometer)",
                Type = AppButton.ButtonType.Secondary,
                Size = new Size(500, 90),
                Location = new Point(50, 250),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMicrometer.Click += BtnMicrometer_Click;

            btnLogout = new AppButton
            {
                Text = "Logout / Kembali",
                Type = AppButton.ButtonType.Danger, 
                Size = new Size(200, 45),
                Location = new Point(200, 380),
                Cursor = Cursors.Hand
            };
            btnLogout.Click += (s, e) => this.Close(); 

            this.Controls.Add(lblWelcome);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(btnChecksheet);
            this.Controls.Add(btnMicrometer);
            this.Controls.Add(btnLogout);
        }

        private void BtnChecksheet_Click(object sender, EventArgs e)
        {
            var checkForm = new ChecksheetForm(isTeknisiMode: true);
            this.Hide();
            checkForm.FormClosed += (s, args) => this.Show();
            checkForm.Show();
        }

        private void BtnMicrometer_Click(object sender, EventArgs e)
        {
            var microForm = new mtc_app.features.micrometer_patrol.presentation.screens.MicrometerPatrolForm(
                mtc_app.shared.infrastructure.ServiceLocator.CreateMicrometerPatrolRepository(),
                mtc_app.shared.infrastructure.ServiceLocator.CreateMasterDataRepository());
            this.Hide();
            microForm.FormClosed += (s, args) => this.Show();
            microForm.Show();
        }
    }
}
