using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.stock.presentation.components
{
    public class NotificationForm : Form
    {
        private AppLabel lblTitle;
        private AppLabel lblMessage;
        private AppButton btnOk;

        public NotificationForm(string partName)
        {
            InitializeComponent();
            lblMessage.Text = $"Permintaan Baru:\n{partName}";
        }

        private void InitializeComponent()
        {
            this.Text = "Notifikasi Gudang";
            this.Size = new Size(500, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.TopMost = true; // Always on top

            // Title
            lblTitle = new AppLabel
            {
                Text = "⚠️ ADA REQUEST BARU!",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.Danger,
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
                Text = "SIAP LAKSANAKAN (OK)",
                Dock = DockStyle.Fill,
                Type = AppButton.ButtonType.Primary,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold)
            };
            btnOk.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnOk);

            // Message (Center)
            lblMessage = new AppLabel
            {
                Text = "-",
                Font = new Font("Segoe UI", 24F, FontStyle.Regular), // Huge Font
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            this.Controls.Add(lblMessage);
            lblMessage.BringToFront();
        }
    }
}
