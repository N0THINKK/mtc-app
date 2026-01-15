using System;
using System.Drawing;
using System.Windows.Forms;

namespace mtc_app.features.stock.presentation.components
{
    public class StockStatusCard : Panel
    {
        private Label lblTitle;
        private Label lblValue;
        private Label lblSubtext;
        private Panel indicatorPanel;

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

            // Main panel settings
            this.BackColor = Color.White;
            this.Size = new Size(200, 120);
            this.Padding = new Padding(15);
            this.BorderStyle = BorderStyle.None;

            // Indicator panel (colored left border)
            indicatorPanel = new Panel
            {
                Width = 4,
                Dock = DockStyle.Left,
                BackColor = mtc_app.shared.presentation.styles.AppColors.Primary
            };

            // Title label
            lblTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(20, 15),
                Text = "Status"
            };

            // Value label
            lblValue = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(20, 35),
                Text = "0"
            };

            // Subtext label
            lblSubtext = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(20, 75),
                Text = "",
                Visible = false
            };

            this.Controls.Add(indicatorPanel);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblValue);
            this.Controls.Add(lblSubtext);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void UpdateColors()
        {
            Color indicatorColor;
            Color valueColor;

            switch (_statusType)
            {
                case StatusType.Pending:
                    indicatorColor = mtc_app.shared.presentation.styles.AppColors.Warning;
                    valueColor = mtc_app.shared.presentation.styles.AppColors.Warning;
                    break;
                case StatusType.Ready:
                    indicatorColor = mtc_app.shared.presentation.styles.AppColors.Success;
                    valueColor = mtc_app.shared.presentation.styles.AppColors.Success;
                    break;
                case StatusType.Completed:
                    indicatorColor = mtc_app.shared.presentation.styles.AppColors.Primary;
                    valueColor = mtc_app.shared.presentation.styles.AppColors.Primary;
                    break;
                default:
                    indicatorColor = Color.FromArgb(108, 117, 125);
                    valueColor = Color.FromArgb(33, 37, 41);
                    break;
            }

            indicatorPanel.BackColor = indicatorColor;
            lblValue.ForeColor = valueColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw shadow effect
            using (var pen = new Pen(Color.FromArgb(30, 0, 0, 0)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}