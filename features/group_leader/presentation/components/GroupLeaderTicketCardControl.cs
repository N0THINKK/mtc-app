using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.group_leader.presentation.components
{
    public class GroupLeaderTicketCardControl : UserControl
    {
        private Panel pnlMain;
        private Panel pnlColorStrip;
        private Label lblMachineName;
        private Label lblTechnician;
        private Label lblTime;
        private Label lblStatusBadge;
        private AppStarRating stars;
        private PictureBox iconMachine;
        private PictureBox iconTechnician;
        private PictureBox iconClock;
        private Panel pnlRatingSection;
        private Label lblRatingLabel;

        public long TicketId { get; private set; }
        public event EventHandler CardClicked;

        public GroupLeaderTicketCardControl(long ticketId, string machineName, string technicianName, 
            DateTime createdAt, int? ratingScore, bool isReviewed)
        {
            this.TicketId = ticketId;
            InitializeComponent();
            
            lblMachineName.Text = machineName ?? "Unknown Machine";
            lblTechnician.Text = technicianName ?? "Belum ditugaskan";
            lblTime.Text = createdAt.ToString("dd MMM yyyy HH:mm");
            
            if (ratingScore.HasValue && ratingScore.Value > 0)
            {
                stars.Rating = ratingScore.Value;
                lblRatingLabel.Text = GetRatingText(ratingScore.Value);
            }
            else
            {
                stars.Rating = 0;
                lblRatingLabel.Text = "Belum ada rating";
                lblRatingLabel.ForeColor = AppColors.TextSecondary;
            }

            // Set status badge
            if (isReviewed)
            {
                lblStatusBadge.Text = "✓ Sudah Direview";
                lblStatusBadge.BackColor = Color.FromArgb(220, 252, 231);
                lblStatusBadge.ForeColor = Color.FromArgb(21, 128, 61);
                pnlColorStrip.BackColor = Color.FromArgb(34, 197, 94);
            }
            else
            {
                lblStatusBadge.Text = "⏱ Belum Direview";
                lblStatusBadge.BackColor = Color.FromArgb(254, 249, 195);
                lblStatusBadge.ForeColor = Color.FromArgb(161, 98, 7);
                pnlColorStrip.BackColor = Color.FromArgb(234, 179, 8);
            }
        }

        private string GetRatingText(int rating)
        {
            switch (rating)
            {
                case 5: return "Sangat Baik";
                case 4: return "Baik";
                case 3: return "Cukup";
                case 2: return "Kurang";
                case 1: return "Sangat Kurang";
                default: return "";
            }
        }

        private void InitializeComponent()
        {
            this.pnlMain = new Panel();
            this.pnlColorStrip = new Panel();
            this.lblMachineName = new Label();
            this.lblTechnician = new Label();
            this.lblTime = new Label();
            this.lblStatusBadge = new Label();
            this.stars = new AppStarRating();
            this.iconMachine = new PictureBox();
            this.iconTechnician = new PictureBox();
            this.iconClock = new PictureBox();
            this.pnlRatingSection = new Panel();
            this.lblRatingLabel = new Label();
            
            this.SuspendLayout();
            this.pnlMain.SuspendLayout();
            
            // Main UserControl
            this.BackColor = Color.Transparent;
            this.Size = new Size(380, 200);
            this.Margin = new Padding(10);
            this.Padding = new Padding(0);
            this.Cursor = Cursors.Hand;

            // Main Panel (Card Container)
            this.pnlMain.BackColor = Color.White;
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.Padding = new Padding(0);
            this.pnlMain.Paint += PnlMain_Paint;
            this.pnlMain.Click += (s, e) => CardClicked?.Invoke(this, e);

            // Color Strip (Left Border)
            this.pnlColorStrip.BackColor = AppColors.Primary;
            this.pnlColorStrip.Dock = DockStyle.Left;
            this.pnlColorStrip.Width = 6;

            // Status Badge (Top Right)
            this.lblStatusBadge.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.lblStatusBadge.AutoSize = false;
            this.lblStatusBadge.Size = new Size(140, 24);
            this.lblStatusBadge.Location = new Point(220, 15);
            this.lblStatusBadge.TextAlign = ContentAlignment.MiddleCenter;
            this.lblStatusBadge.Paint += (s, e) => {
                var lbl = s as Label;
                using (GraphicsPath path = GetRoundedRectangle(new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1), 4))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(new SolidBrush(lbl.BackColor), path);
                }
            };

            // Machine Icon
            this.iconMachine.Size = new Size(22, 22);
            this.iconMachine.Location = new Point(20, 18);
            this.iconMachine.BackColor = Color.Transparent;
            this.iconMachine.Paint += (s, e) => DrawMachineIcon(e.Graphics);

            // Machine Name Label
            this.lblMachineName.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            this.lblMachineName.ForeColor = AppColors.TextPrimary;
            this.lblMachineName.Location = new Point(50, 16);
            this.lblMachineName.AutoSize = true;
            this.lblMachineName.MaximumSize = new Size(260, 0);
            this.lblMachineName.Click += (s, e) => CardClicked?.Invoke(this, e);

            // Technician Icon
            this.iconTechnician.Size = new Size(18, 18);
            this.iconTechnician.Location = new Point(20, 54);
            this.iconTechnician.BackColor = Color.Transparent;
            this.iconTechnician.Paint += (s, e) => DrawTechnicianIcon(e.Graphics);

            // Technician Label
            this.lblTechnician.Font = new Font("Segoe UI", 9.5F);
            this.lblTechnician.ForeColor = AppColors.TextSecondary;
            this.lblTechnician.Location = new Point(45, 52);
            this.lblTechnician.AutoSize = true;
            this.lblTechnician.MaximumSize = new Size(320, 0);
            this.lblTechnician.Click += (s, e) => CardClicked?.Invoke(this, e);

            // Clock Icon
            this.iconClock.Size = new Size(16, 16);
            this.iconClock.Location = new Point(20, 80);
            this.iconClock.BackColor = Color.Transparent;
            this.iconClock.Paint += (s, e) => DrawClockIcon(e.Graphics);

            // Time Label
            this.lblTime.Font = new Font("Segoe UI", 9F);
            this.lblTime.ForeColor = AppColors.TextSecondary;
            this.lblTime.Location = new Point(42, 78);
            this.lblTime.AutoSize = true;
            this.lblTime.Click += (s, e) => CardClicked?.Invoke(this, e);

            // Rating Section Panel
            this.pnlRatingSection.BackColor = Color.FromArgb(249, 250, 251);
            this.pnlRatingSection.Location = new Point(15, 110);
            this.pnlRatingSection.Size = new Size(350, 70);
            this.pnlRatingSection.Paint += (s, e) => {
                using (GraphicsPath path = GetRoundedRectangle(new Rectangle(0, 0, pnlRatingSection.Width - 1, pnlRatingSection.Height - 1), 6))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(new SolidBrush(pnlRatingSection.BackColor), path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(229, 231, 235)), path);
                }
            };
            this.pnlRatingSection.Click += (s, e) => CardClicked?.Invoke(this, e);

            // Stars
            this.stars.ReadOnly = true;
            this.stars.Location = new Point(10, 12);
            this.stars.BackColor = Color.Transparent;
            this.stars.Click += (s, e) => CardClicked?.Invoke(this, e);

            // Rating Label
            this.lblRatingLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblRatingLabel.ForeColor = AppColors.Primary;
            this.lblRatingLabel.Location = new Point(10, 42);
            this.lblRatingLabel.AutoSize = true;
            this.lblRatingLabel.Click += (s, e) => CardClicked?.Invoke(this, e);

            // Add controls to rating section
            this.pnlRatingSection.Controls.Add(this.lblRatingLabel);
            this.pnlRatingSection.Controls.Add(this.stars);

            // Add controls to main panel
            this.pnlMain.Controls.Add(this.pnlRatingSection);
            this.pnlMain.Controls.Add(this.lblTime);
            this.pnlMain.Controls.Add(this.iconClock);
            this.pnlMain.Controls.Add(this.lblTechnician);
            this.pnlMain.Controls.Add(this.iconTechnician);
            this.pnlMain.Controls.Add(this.lblMachineName);
            this.pnlMain.Controls.Add(this.iconMachine);
            this.pnlMain.Controls.Add(this.lblStatusBadge);
            this.pnlMain.Controls.Add(this.pnlColorStrip);

            // Add main panel to UserControl
            this.Controls.Add(this.pnlMain);

            // Hover effect
            this.pnlMain.MouseEnter += (s, e) => {
                this.pnlMain.BackColor = Color.FromArgb(248, 250, 252);
            };
            this.pnlMain.MouseLeave += (s, e) => {
                this.pnlMain.BackColor = Color.White;
            };

            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }

        private void PnlMain_Paint(object sender, PaintEventArgs e)
        {
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
                g.DrawRectangle(pen, 3, 3, 16, 16);
                g.DrawLine(pen, 7, 3, 7, 1);
                g.DrawLine(pen, 15, 3, 15, 1);
                g.DrawLine(pen, 7, 19, 7, 21);
                g.DrawLine(pen, 15, 19, 15, 21);
            }
        }

        private void DrawTechnicianIcon(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(AppColors.TextSecondary, 1.5f))
            {
                // Head
                g.DrawEllipse(pen, 5, 2, 8, 8);
                // Body
                g.DrawArc(pen, 2, 10, 14, 10, 0, 180);
            }
        }

        private void DrawClockIcon(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(AppColors.TextSecondary, 1.5f))
            {
                g.DrawEllipse(pen, 2, 2, 12, 12);
                g.DrawLine(pen, 8, 8, 8, 5);
                g.DrawLine(pen, 8, 8, 11, 8);
            }
        }
    }
}