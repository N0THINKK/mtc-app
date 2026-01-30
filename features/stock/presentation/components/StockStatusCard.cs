using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace mtc_app.features.stock.presentation.components
{
    public class StockStatusCard : Panel
    {
        private Label lblTitle;
        private Label lblValue;
        private Label lblSubtext;
        private Panel pnlAccent;

        public enum StatusType
        {
            Pending,
            Ready,
            Completed,
            Info
        }

        private StatusType _statusType;

        public StatusType Type
        {
            get => _statusType;
            set
            {
                _statusType = value;
                UpdateColors();
            }
        }

        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value;
        }

        public string Value
        {
            get => lblValue.Text;
            set => lblValue.Text = value;
        }

        public string Subtext
        {
            get => lblSubtext.Text;
            set
            {
                lblSubtext.Text = value;
                lblSubtext.Visible = !string.IsNullOrEmpty(value);
            }
        }

        public StockStatusCard()
        {
            InitializeComponent();
            _statusType = StatusType.Info;
            UpdateColors();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Main panel settings - modern flat design
            this.BackColor = Color.White;
            this.Size = new Size(300, 140);
            this.Padding = new Padding(0);
            this.BorderStyle = BorderStyle.None;

            // Accent bar at the top (modern touch)
            pnlAccent = new Panel
            {
                Height = 4,
                Dock = DockStyle.Top,
                BackColor = mtc_app.shared.presentation.styles.AppColors.Primary
            };

            // Title label
            lblTitle = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(20, 20),
                Size = new Size(260, 25),
                Text = "Status"
            };

            // Value label - big and bold
            lblValue = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 36F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(18, 50),
                Size = new Size(260, 60),
                Text = "0"
            };

            // Subtext label
            lblSubtext = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(20, 110),
                Size = new Size(260, 25),
                Text = "",
                Visible = false
            };

            this.Controls.Add(pnlAccent);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblValue);
            this.Controls.Add(lblSubtext);

            this.ResumeLayout(false);
        }

        private void UpdateColors()
        {
            Color accentColor;
            Color valueColor;

            switch (_statusType)
            {
                case StatusType.Pending:
                    accentColor = mtc_app.shared.presentation.styles.AppColors.Warning;
                    valueColor = mtc_app.shared.presentation.styles.AppColors.Warning;
                    break;
                case StatusType.Ready:
                    accentColor = mtc_app.shared.presentation.styles.AppColors.Success;
                    valueColor = mtc_app.shared.presentation.styles.AppColors.Success;
                    break;
                case StatusType.Completed:
                    accentColor = mtc_app.shared.presentation.styles.AppColors.Primary;
                    valueColor = mtc_app.shared.presentation.styles.AppColors.Primary;
                    break;
                default:
                    accentColor = Color.FromArgb(108, 117, 125);
                    valueColor = Color.FromArgb(33, 37, 41);
                    break;
            }

            pnlAccent.BackColor = accentColor;
            lblValue.ForeColor = valueColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Modern subtle shadow effect
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var pen = new Pen(Color.FromArgb(40, 0, 0, 0), 1))
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