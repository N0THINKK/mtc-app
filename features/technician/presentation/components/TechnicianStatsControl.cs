using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.features.technician.presentation.components
{
    public class TechnicianStatsControl : UserControl
    {
        private Panel pnlJumlahPerbaikan;
        private Panel pnlAverageBintang;
        
        private Label lblJumlahValue;
        private Label lblJumlahLabel;
        private PictureBox iconJumlah;
        
        private Label lblAverageValue;
        private Label lblAverageLabel;
        private PictureBox iconAverage;

        public TechnicianStatsControl()
        {
            InitializeComponent();
        }

        public void UpdateStats(int jumlahPerbaikan, decimal averageBintang)
        {
            lblJumlahValue.Text = jumlahPerbaikan.ToString();
            lblAverageValue.Text = averageBintang.ToString("0.0");
        }

        private void InitializeComponent()
        {
            this.pnlJumlahPerbaikan = new Panel();
            this.pnlAverageBintang = new Panel();
            
            this.lblJumlahValue = new Label();
            this.lblJumlahLabel = new Label();
            this.iconJumlah = new PictureBox();
            
            this.lblAverageValue = new Label();
            this.lblAverageLabel = new Label();
            this.iconAverage = new PictureBox();

            this.SuspendLayout();
            
            this.BackColor = Color.Transparent;
            this.Size = new Size(900, 100);
            this.Padding = new Padding(0);

            // Use TableLayoutPanel with percentage columns for responsive card widths
            var cardsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cardsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            SetupStatCard(pnlJumlahPerbaikan, iconJumlah, lblJumlahValue, lblJumlahLabel,
                "0", "Jumlah Perbaikan", Color.FromArgb(59, 130, 246), Color.FromArgb(239, 246, 255),
                DrawChecklistIcon);

            SetupStatCard(pnlAverageBintang, iconAverage, lblAverageValue, lblAverageLabel,
                "0.0", "Rata-rata Bintang", Color.FromArgb(234, 179, 8), Color.FromArgb(254, 252, 232),
                DrawStarIcon);

            cardsLayout.Controls.Add(pnlJumlahPerbaikan, 0, 0);
            cardsLayout.Controls.Add(pnlAverageBintang, 1, 0);

            this.Controls.Add(cardsLayout);

            this.ResumeLayout(false);
        }

        private void SetupStatCard(Panel panel, PictureBox icon, Label valueLabel, Label textLabel,
            string defaultValue, string labelText, Color accentColor, Color bgColor,
            Action<Graphics, Color> drawIcon)
        {
            panel.BackColor = AppColors.CardBackground;
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 0, AppDimens.GapStandard, 0);
            panel.Paint += (s, e) => DrawStatCard(e.Graphics, panel.ClientRectangle, accentColor);

            // Internal layout: 2 columns (Icon fixed, Content fill) x 2 rows
            var cardLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(AppDimens.PaddingStandard, AppDimens.PaddingSmall, AppDimens.PaddingStandard, AppDimens.PaddingSmall),
                Margin = new Padding(0)
            };
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F)); // Icon column
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Content column
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F)); // Value row
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F)); // Label row

            // Icon (spans 2 rows)
            icon.Size = new Size(48, 48);
            icon.BackColor = Color.Transparent;
            icon.Anchor = AnchorStyles.None; // Center in cell
            icon.Paint += (s, e) => drawIcon(e.Graphics, accentColor);
            cardLayout.Controls.Add(icon, 0, 0);
            cardLayout.SetRowSpan(icon, 2);

            // Value Label
            valueLabel.Font = AppFonts.MetricMedium;
            valueLabel.ForeColor = AppColors.TextPrimary;
            valueLabel.AutoSize = true;
            valueLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            valueLabel.Text = defaultValue;
            cardLayout.Controls.Add(valueLabel, 1, 0);

            // Text Label
            textLabel.Font = AppFonts.BodySmall;
            textLabel.ForeColor = AppColors.TextSecondary;
            textLabel.AutoSize = true;
            textLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            textLabel.Text = labelText;
            cardLayout.Controls.Add(textLabel, 1, 1);

            panel.Controls.Add(cardLayout);

            // Hover effect
            panel.MouseEnter += (s, e) => { panel.BackColor = bgColor; panel.Cursor = Cursors.Hand; };
            panel.MouseLeave += (s, e) => { panel.BackColor = AppColors.CardBackground; panel.Cursor = Cursors.Default; };
        }

        private void DrawStatCard(Graphics g, Rectangle bounds, Color accentColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = GraphicsUtils.GetRoundedRectangle(new Rectangle(0, 0, bounds.Width - 1, bounds.Height - 1), 8))
            {
                g.FillPath(new SolidBrush(AppColors.CardBackground), path);
                g.DrawPath(new Pen(Color.FromArgb(230, 230, 230), 1), path);
            }

            using (Pen accentPen = new Pen(accentColor, 3))
            {
                g.DrawLine(accentPen, 8, 0, bounds.Width - 8, 0);
            }
        }

        private void DrawChecklistIcon(Graphics g, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 3))
            {
                g.DrawRectangle(pen, 8, 12, 32, 28);
                g.DrawRectangle(pen, 18, 8, 12, 6);
                g.DrawLine(pen, 16, 24, 20, 28);
                g.DrawLine(pen, 20, 28, 32, 16);
            }
        }

        private void DrawStarIcon(Graphics g, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            PointF[] starPoints = new PointF[]
            {
                new PointF(24, 8),
                new PointF(28, 18),
                new PointF(38, 18),
                new PointF(30, 26),
                new PointF(32, 36),
                new PointF(24, 30),
                new PointF(16, 36),
                new PointF(18, 26),
                new PointF(10, 18),
                new PointF(20, 18)
            };

            using (Brush brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, starPoints);
            }
        }
    }
}
