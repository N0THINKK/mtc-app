using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class TechnicianStatsControl : UserControl
    {
        private Panel pnlJumlahPerbaikan;
        private Panel pnlAverageBintang;
        private Panel pnlTotalBintang;
        
        private Label lblJumlahValue;
        private Label lblJumlahLabel;
        private PictureBox iconJumlah;
        
        private Label lblAverageValue;
        private Label lblAverageLabel;
        private PictureBox iconAverage;
        
        private Label lblTotalValue;
        private Label lblTotalLabel;
        private PictureBox iconTotal;

        private int _jumlahPerbaikan;
        private decimal _averageBintang;
        private int _totalBintang;

        public TechnicianStatsControl()
        {
            InitializeComponent();
        }

        public void UpdateStats(int jumlahPerbaikan, decimal averageBintang, int totalBintang)
        {
            _jumlahPerbaikan = jumlahPerbaikan;
            _averageBintang = averageBintang;
            _totalBintang = totalBintang;

            lblJumlahValue.Text = jumlahPerbaikan.ToString();
            lblAverageValue.Text = averageBintang.ToString("0.0");
            lblTotalValue.Text = totalBintang.ToString();
        }

        private void InitializeComponent()
        {
            this.pnlJumlahPerbaikan = new Panel();
            this.pnlAverageBintang = new Panel();
            this.pnlTotalBintang = new Panel();
            
            this.lblJumlahValue = new Label();
            this.lblJumlahLabel = new Label();
            this.iconJumlah = new PictureBox();
            
            this.lblAverageValue = new Label();
            this.lblAverageLabel = new Label();
            this.iconAverage = new PictureBox();
            
            this.lblTotalValue = new Label();
            this.lblTotalLabel = new Label();
            this.iconTotal = new PictureBox();

            this.SuspendLayout();
            
            // Main UserControl
            this.BackColor = Color.Transparent;
            this.Size = new Size(900, 100);
            this.Padding = new Padding(0);

            // Setup three stat cards
            SetupStatCard(pnlJumlahPerbaikan, iconJumlah, lblJumlahValue, lblJumlahLabel,
                0, "0", "Jumlah Perbaikan", Color.FromArgb(59, 130, 246), Color.FromArgb(239, 246, 255));

            SetupStatCard(pnlAverageBintang, iconAverage, lblAverageValue, lblAverageLabel,
                310, "0.0", "Rata-rata Bintang", Color.FromArgb(234, 179, 8), Color.FromArgb(254, 252, 232));

            SetupStatCard(pnlTotalBintang, iconTotal, lblTotalValue, lblTotalLabel,
                620, "0", "Total Bintang", Color.FromArgb(34, 197, 94), Color.FromArgb(240, 253, 244));

            // Add panels to control
            this.Controls.Add(pnlJumlahPerbaikan);
            this.Controls.Add(pnlAverageBintang);
            this.Controls.Add(pnlTotalBintang);

            this.ResumeLayout(false);
        }

        private void SetupStatCard(Panel panel, PictureBox icon, Label valueLabel, Label textLabel,
            int xPosition, string defaultValue, string labelText, Color accentColor, Color bgColor)
        {
            // Panel
            panel.BackColor = Color.White;
            panel.Size = new Size(290, 100);
            panel.Location = new Point(xPosition, 0);
            panel.Paint += (s, e) => DrawStatCard(e.Graphics, panel.ClientRectangle, accentColor);

            // Icon
            icon.Size = new Size(48, 48);
            icon.Location = new Point(20, 26);
            icon.BackColor = Color.Transparent;
            
            if (labelText.Contains("Jumlah"))
                icon.Paint += (s, e) => DrawChecklistIcon(e.Graphics, accentColor);
            else if (labelText.Contains("Rata-rata"))
                icon.Paint += (s, e) => DrawStarIcon(e.Graphics, accentColor);
            else
                icon.Paint += (s, e) => DrawTrophyIcon(e.Graphics, accentColor);

            // Value Label
            valueLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            valueLabel.ForeColor = AppColors.TextPrimary;
            valueLabel.Location = new Point(85, 20);
            valueLabel.AutoSize = true;
            valueLabel.Text = defaultValue;

            // Text Label
            textLabel.Font = new Font("Segoe UI", 10F);
            textLabel.ForeColor = AppColors.TextSecondary;
            textLabel.Location = new Point(85, 58);
            textLabel.AutoSize = true;
            textLabel.Text = labelText;

            // Add controls to panel
            panel.Controls.Add(textLabel);
            panel.Controls.Add(valueLabel);
            panel.Controls.Add(icon);

            // Hover effect
            panel.MouseEnter += (s, e) => {
                panel.BackColor = bgColor;
                panel.Cursor = Cursors.Hand;
            };
            panel.MouseLeave += (s, e) => {
                panel.BackColor = Color.White;
                panel.Cursor = Cursors.Default;
            };
        }

        private void DrawStatCard(Graphics g, Rectangle bounds, Color accentColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Card background with rounded corners
            using (GraphicsPath path = GetRoundedRectangle(new Rectangle(0, 0, bounds.Width - 1, bounds.Height - 1), 8))
            {
                g.FillPath(new SolidBrush(Color.White), path);
                g.DrawPath(new Pen(Color.FromArgb(230, 230, 230), 1), path);
            }

            // Top accent line
            using (Pen accentPen = new Pen(accentColor, 3))
            {
                g.DrawLine(accentPen, 8, 0, bounds.Width - 8, 0);
            }
        }

        private GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }

        private void DrawChecklistIcon(Graphics g, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 3))
            {
                // Clipboard outline
                g.DrawRectangle(pen, 8, 12, 32, 28);
                // Clip
                g.DrawRectangle(pen, 18, 8, 12, 6);
                // Checkmark
                g.DrawLine(pen, 16, 24, 20, 28);
                g.DrawLine(pen, 20, 28, 32, 16);
            }
        }

        private void DrawStarIcon(Graphics g, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            PointF[] starPoints = new PointF[]
            {
                new PointF(24, 8),      // Top
                new PointF(28, 18),     // Top right inner
                new PointF(38, 18),     // Right
                new PointF(30, 26),     // Bottom right inner
                new PointF(32, 36),     // Bottom right
                new PointF(24, 30),     // Bottom inner
                new PointF(16, 36),     // Bottom left
                new PointF(18, 26),     // Bottom left inner
                new PointF(10, 18),     // Left
                new PointF(20, 18)      // Top left inner
            };

            using (Brush brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, starPoints);
            }
        }

        private void DrawTrophyIcon(Graphics g, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 3))
            {
                // Cup
                g.DrawArc(pen, 14, 10, 20, 20, 0, 180);
                g.DrawLine(pen, 14, 20, 16, 28);
                g.DrawLine(pen, 34, 20, 32, 28);
                // Base
                g.DrawLine(pen, 16, 28, 32, 28);
                g.DrawLine(pen, 20, 28, 20, 32);
                g.DrawLine(pen, 28, 28, 28, 32);
                g.DrawLine(pen, 16, 32, 32, 32);
                // Handles
                g.DrawArc(pen, 8, 12, 8, 12, 90, 180);
                g.DrawArc(pen, 32, 12, 8, 12, 270, 180);
            }
        }
    }
}
