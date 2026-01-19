using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class MetricCard : Panel
    {
        private Label lblTitle;
        private Label lblValue;
        private Label lblSubtext;
        private Panel pnlAccent;

        [Category("Data")]
        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value;
        }

        [Category("Data")]
        public string Value
        {
            get => lblValue.Text;
            set => lblValue.Text = value;
        }

        [Category("Data")]
        public string Subtext
        {
            get => lblSubtext.Text;
            set
            {
                lblSubtext.Text = value;
                lblSubtext.Visible = !string.IsNullOrEmpty(value);
            }
        }

        [Category("Appearance")]
        public Color AccentColor
        {
            get => pnlAccent.BackColor;
            set 
            {
                pnlAccent.BackColor = value;
                // Optional: Update value text color to match accent? 
                // StockStatusCard did this: `lblValue.ForeColor = valueColor;`
                lblValue.ForeColor = value;
            }
        }

        public MetricCard()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Main panel settings
            this.BackColor = Color.White;
            this.Size = new Size(220, 120);
            this.Padding = new Padding(0);
            this.BorderStyle = BorderStyle.None;

            // Accent bar
            pnlAccent = new Panel
            {
                Height = 4,
                Dock = DockStyle.Top,
                BackColor = AppColors.Primary
            };

            // Title label
            lblTitle = new Label
            {
                AutoSize = false,
                Font = AppFonts.BodySmall,
                ForeColor = AppColors.TextSecondary, // Gray
                Location = new Point(20, 18),
                Size = new Size(180, 20),
                Text = "Title"
            };

            // Value label
            lblValue = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 28F, FontStyle.Bold), // Big Value
                ForeColor = AppColors.TextPrimary,
                Location = new Point(18, 38),
                Size = new Size(180, 45),
                Text = "0"
            };

            // Subtext label
            lblSubtext = new Label
            {
                AutoSize = false,
                Font = AppFonts.Caption,
                ForeColor = AppColors.TextSecondary,
                Location = new Point(20, 88),
                Size = new Size(180, 20),
                Text = "",
                Visible = false
            };

            this.Controls.Add(pnlAccent);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblValue);
            this.Controls.Add(lblSubtext);

            this.ResumeLayout(false);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Subtle border/shadow simulation
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(20, 0, 0, 0), 1)) // Very faint border
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.BackColor = Color.FromArgb(250, 251, 252);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.BackColor = Color.White;
        }
    }
}
