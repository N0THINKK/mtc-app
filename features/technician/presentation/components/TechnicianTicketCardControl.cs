using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class TechnicianTicketCardControl : UserControl
    {
        private Panel pnlMain;
        private Panel pnlColorStrip;
        private Label lblMachineName;
        private Label lblProblem;
        private Label lblTime;
        private Label lblStatusBadge;
        private PictureBox iconMachine;
        private PictureBox iconClock;

        private int _statusId;

        public TechnicianTicketCardControl(string machineName, string problem, string timeAgo, int statusId)
        {
            _statusId = statusId;
            InitializeComponent();
            this.lblMachineName.Text = machineName;
            this.lblProblem.Text = problem;
            this.lblTime.Text = timeAgo;
            UpdateStatusVisuals();
        }

        private void InitializeComponent()
        {
            this.pnlMain = new Panel();
            this.pnlColorStrip = new Panel();
            this.lblMachineName = new Label();
            this.lblProblem = new Label();
            this.lblTime = new Label();
            this.lblStatusBadge = new Label();
            this.iconMachine = new PictureBox();
            this.iconClock = new PictureBox();
            
            this.SuspendLayout();
            this.pnlMain.SuspendLayout();
            
            // 
            // Main UserControl
            // 
            this.BackColor = Color.Transparent;
            this.Size = new Size(360, 140);
            this.Margin = new Padding(10);
            this.Padding = new Padding(0);

            // 
            // Main Panel (Card Container)
            // 
            this.pnlMain.BackColor = Color.White;
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.Padding = new Padding(0);
            this.pnlMain.Paint += PnlMain_Paint; // For rounded corners and shadow

            // 
            // Color Strip (Left Border)
            // 
            this.pnlColorStrip.Dock = DockStyle.Left;
            this.pnlColorStrip.Width = 6;

            // 
            // Machine Icon
            // 
            this.iconMachine.Size = new Size(24, 24);
            this.iconMachine.Location = new Point(20, 18);
            this.iconMachine.BackColor = Color.Transparent;
            this.iconMachine.Paint += (s, e) => DrawMachineIcon(e.Graphics);

            // 
            // Machine Name Label
            // 
            this.lblMachineName.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            this.lblMachineName.ForeColor = AppColors.TextPrimary;
            this.lblMachineName.Location = new Point(52, 16);
            this.lblMachineName.AutoSize = true;
            this.lblMachineName.MaximumSize = new Size(290, 0);

            // 
            // Problem Label
            // 
            this.lblProblem.Font = new Font("Segoe UI", 9.5F);
            this.lblProblem.ForeColor = AppColors.TextSecondary;
            this.lblProblem.Location = new Point(20, 55);
            this.lblProblem.MaximumSize = new Size(320, 50);
            this.lblProblem.AutoSize = true;

            // 
            // Clock Icon
            // 
            this.iconClock.Size = new Size(16, 16);
            this.iconClock.Location = new Point(20, 110);
            this.iconClock.BackColor = Color.Transparent;
            this.iconClock.Paint += (s, e) => DrawClockIcon(e.Graphics);

            // 
            // Time Ago Label
            // 
            this.lblTime.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblTime.ForeColor = AppColors.Danger;
            this.lblTime.Location = new Point(42, 108);
            this.lblTime.AutoSize = true;

            // 
            // Status Badge
            // 
            this.lblStatusBadge.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            this.lblStatusBadge.AutoSize = true;
            this.lblStatusBadge.Padding = new Padding(8, 4, 8, 4);
            this.lblStatusBadge.Location = new Point(240, 16);

            // Add controls to main panel
            this.pnlMain.Controls.Add(this.lblStatusBadge);
            this.pnlMain.Controls.Add(this.lblTime);
            this.pnlMain.Controls.Add(this.iconClock);
            this.pnlMain.Controls.Add(this.lblProblem);
            this.pnlMain.Controls.Add(this.lblMachineName);
            this.pnlMain.Controls.Add(this.iconMachine);
            this.pnlMain.Controls.Add(this.pnlColorStrip);

            // Add main panel to UserControl
            this.Controls.Add(this.pnlMain);

            // Hover effect
            this.pnlMain.MouseEnter += (s, e) => {
                this.pnlMain.BackColor = Color.FromArgb(248, 250, 252);
                this.Cursor = Cursors.Hand;
            };
            this.pnlMain.MouseLeave += (s, e) => {
                this.pnlMain.BackColor = Color.White;
                this.Cursor = Cursors.Default;
            };

            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }

        private void PnlMain_Paint(object sender, PaintEventArgs e)
        {
            // Rounded corners and shadow effect
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Shadow
            using (GraphicsPath shadowPath = GetRoundedRectangle(new Rectangle(2, 2, this.Width - 4, this.Height - 4), 8))
            {
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(20, 0, 0, 0);
                    shadowBrush.SurroundColors = new[] { Color.FromArgb(0, 0, 0, 0) };
                    g.FillPath(shadowBrush, shadowPath);
                }
            }

            // Card background
            using (GraphicsPath path = GetRoundedRectangle(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 8))
            {
                g.FillPath(new SolidBrush(pnlMain.BackColor), path);
                g.DrawPath(new Pen(Color.FromArgb(230, 230, 230), 1), path);
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

        private void DrawMachineIcon(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(AppColors.Primary, 2))
            {
                // Simple machine/gear icon
                g.DrawRectangle(pen, 4, 4, 16, 16);
                g.DrawLine(pen, 8, 4, 8, 2);
                g.DrawLine(pen, 16, 4, 16, 2);
                g.DrawLine(pen, 8, 20, 8, 22);
                g.DrawLine(pen, 16, 20, 16, 22);
            }
        }

        private void DrawClockIcon(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(AppColors.Danger, 1.5f))
            {
                // Clock circle
                g.DrawEllipse(pen, 2, 2, 12, 12);
                // Clock hands
                g.DrawLine(pen, 8, 8, 8, 5);
                g.DrawLine(pen, 8, 8, 11, 8);
            }
        }

        private void UpdateStatusVisuals()
        {
            Color stripColor;
            Color badgeBgColor;
            Color badgeTextColor;
            string badgeText;

            switch (_statusId)
            {
                case 1: // Not Repaired Yet
                    stripColor = Color.FromArgb(239, 68, 68); // Red
                    badgeBgColor = Color.FromArgb(254, 242, 242);
                    badgeTextColor = Color.FromArgb(185, 28, 28);
                    badgeText = "Open";
                    break;
                case 2: // Repairing
                    stripColor = Color.FromArgb(249, 115, 22); // Orange
                    badgeBgColor = Color.FromArgb(255, 247, 237);
                    badgeTextColor = Color.FromArgb(194, 65, 12);
                    badgeText = "Sedang Diperbaiki";
                    break;
                case 3: // Done
                    stripColor = Color.FromArgb(34, 197, 94); // Green
                    badgeBgColor = Color.FromArgb(240, 253, 244);
                    badgeTextColor = Color.FromArgb(21, 128, 61);
                    badgeText = "Selesai";
                    break;
                default:
                    stripColor = AppColors.Primary;
                    badgeBgColor = Color.FromArgb(240, 240, 240);
                    badgeTextColor = AppColors.TextSecondary;
                    badgeText = "Unknown";
                    break;
            }

            this.pnlColorStrip.BackColor = stripColor;
            this.lblStatusBadge.BackColor = badgeBgColor;
            this.lblStatusBadge.ForeColor = badgeTextColor;
            this.lblStatusBadge.Text = badgeText;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }
    }
}