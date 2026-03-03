using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.data.session;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public class OperatorMainMenuForm : AppBaseForm
    {
        private Label lblWelcome;
        private Label lblSubtitle;
        private AppButton btnHistory;
        private AppButton btnChecksheet;
        private AppButton btnLogout;

        public OperatorMainMenuForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Menu Utama Operator";
            this.Size = new Size(600, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.None; 

            string userName = UserSession.CurrentUser?.Username ?? "Operator";

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

            btnHistory = new AppButton
            {
                Text = "Lapor Mesin Bermasalah\n(Machine History)",
                Type = AppButton.ButtonType.Primary,
                Size = new Size(500, 90),
                Location = new Point(50, 140),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnHistory.Click += BtnHistory_Click;

            btnChecksheet = new AppButton
            {
                Text = "Isi Patroli Harian\n(Digital Checksheet)",
                Type = AppButton.ButtonType.Secondary, 
                Size = new Size(500, 90),
                Location = new Point(50, 250),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChecksheet.Click += BtnChecksheet_Click;

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
            this.Controls.Add(btnHistory);
            this.Controls.Add(btnChecksheet);
            this.Controls.Add(btnLogout);
        }

        private void BtnHistory_Click(object sender, EventArgs e)
        {
            var historyForm = new MachineHistoryFormOperator();
            this.Hide();
            historyForm.FormClosed += (s, args) => this.Show(); 
            historyForm.Show();
        }

        private void BtnChecksheet_Click(object sender, EventArgs e)
        {
            // TAHAP 4 SELESAI: Panggil Form Checksheet khusus Operator!
            var checkForm = new ChecksheetForm(isTeknisiMode: false);
            this.Hide();
            checkForm.FormClosed += (s, args) => this.Show();
            checkForm.Show();
        }
    }
}