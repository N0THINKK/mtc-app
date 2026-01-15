using System;
using System.Drawing;
using System.Windows.Forms;

namespace mtc_app.features.stock.presentation.components
{
    public class EmptyStatePanel : Panel
    {
        private Label lblIcon;
        private Label lblTitle;
        private Label lblDescription;

        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value;
        }

        public string Description
        {
            get => lblDescription.Text;
            set => lblDescription.Text = value;
        }

        public string Icon
        {
            get => lblIcon.Text;
            set => lblIcon.Text = value;
        }

        public EmptyStatePanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Main panel settings
            this.BackColor = Color.White;
            this.Dock = DockStyle.Fill;

            // Icon label (using emoji/symbol)
            lblIcon = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 48F, FontStyle.Regular),
                ForeColor = Color.FromArgb(206, 212, 218),
                Text = "📦",
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Title label
            lblTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(108, 117, 125),
                Text = "No Pending Requests",
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Description label
            lblDescription = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(173, 181, 189),
                Text = "All requests have been processed. The system is working correctly.",
                TextAlign = ContentAlignment.MiddleCenter,
                MaximumSize = new Size(400, 0)
            };

            this.Controls.Add(lblIcon);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblDescription);

            this.Resize += EmptyStatePanel_Resize;

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void EmptyStatePanel_Resize(object sender, EventArgs e)
        {
            CenterControls();
        }

        private void CenterControls()
        {
            int centerX = this.Width / 2;
            int startY = (this.Height / 2) - 100;

            lblIcon.Location = new Point(centerX - (lblIcon.Width / 2), startY);
            lblTitle.Location = new Point(centerX - (lblTitle.Width / 2), startY + 80);
            lblDescription.Location = new Point(centerX - (lblDescription.Width / 2), startY + 115);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            CenterControls();
        }
    }
}