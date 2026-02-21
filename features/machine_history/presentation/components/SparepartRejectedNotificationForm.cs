using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.components
{
    public class SparepartRejectedNotificationForm : Form
    {
        private AppLabel lblTitle;
        private AppLabel lblMessage;
        private AppButton btnOk;

        public SparepartRejectedNotificationForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Notifikasi Teknisi";
            this.Size = new Size(550, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppColors.CardBackground;
            this.TopMost = true; // Always on top

            // Title
            lblTitle = new AppLabel
            {
                Text = "❌ PERMINTAAN DITOLAK!",
                Font = AppFonts.Header2,
                ForeColor = AppColors.Danger, // Red
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };
            this.Controls.Add(lblTitle);

            // Button Panel (Bottom)
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                Padding = new Padding(20)
            };
            this.Controls.Add(pnlBottom);

            btnOk = new AppButton
            {
                Text = "OKE, SAYA MENGERTI",
                Dock = DockStyle.Fill,
                Type = AppButton.ButtonType.Danger,
                Font = AppFonts.Header3
            };
            btnOk.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnOk);

            // Message (Center)
            lblMessage = new AppLabel
            {
                Text = "Sparepart yang Anda minta telah DITOLAK oleh gudang.\nSilakan hubungi supervisor atau bagian gudang.",
                Font = AppFonts.Subtitle, // Medium Font
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            this.Controls.Add(lblMessage);
            lblMessage.BringToFront();
        }
    }
}
