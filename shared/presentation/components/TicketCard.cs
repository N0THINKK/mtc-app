using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.shared.presentation.components
{
    public class TicketCard : UserControl
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

        public TicketCard(string machineName, string problem, string timeAgo, int statusId)
        {
            InitializeComponent();
            
            this.lblMachineName.Text = machineName;
            this.lblProblem.Text = problem;
            this.lblTime.Text = timeAgo;
            this._statusId = statusId;
            
            UpdateStatusVisuals();
        }

        public void UpdateStatus(int statusId)
        {
            _statusId = statusId;
            UpdateStatusVisuals();
        }

        private void UpdateStatusVisuals()
        {
            Color stripColor = TicketStatusUtils.GetStatusStripColor(_statusId);
            this.pnlColorStrip.BackColor = stripColor;

            var badgeColors = TicketStatusUtils.GetStatusBadgeColors(_statusId);
            this.lblStatusBadge.BackColor = badgeColors.Background;
            this.lblStatusBadge.ForeColor = badgeColors.Text;
            this.lblStatusBadge.Text = TicketStatusUtils.GetStatusText(_statusId).ToUpper();
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
            this.pnlMain.Paint += PnlMain_Paint;

            // 
            // Color Strip (Left Border)
            // 
            this.pnlColorStrip.Dock = DockStyle.Left;
            this.pnlColorStrip.Width = 6;
            
            // 
            // Status Badge
            // 
            this.lblStatusBadge.AutoSize = true;
            this.lblStatusBadge.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            this.lblStatusBadge.Location = new Point(240, 15); // Top Right absolute pos
            this.lblStatusBadge.Padding = new Padding(6, 2, 6, 2);
            // Default colors set in constructor/update
            
            // 
            // Machine Icon
            // 
            this.iconMachine.Size = new Size(16, 16);
            this.iconMachine.Location = new Point(20, 20);
            this.iconMachine.Paint += (s, e) => GraphicsUtils.DrawMachineIcon(e.Graphics, new Rectangle(0, 0, 16, 16), AppColors.IconColor);
            
            // 
            // Machine Label
            // 
            this.lblMachineName.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.lblMachineName.ForeColor = AppColors.TextPrimary;
            this.lblMachineName.Location = new Point(42, 18);
            this.lblMachineName.Size = new Size(190, 20);
            this.lblMachineName.AutoEllipsis = true;

            // 
            // Problem Label
            // 
            this.lblProblem.Font = new Font("Segoe UI", 10F);
            this.lblProblem.ForeColor = AppColors.TextSecondary;
            this.lblProblem.Location = new Point(20, 50);
            this.lblProblem.Size = new Size(320, 45); // Multi-line
            this.lblProblem.AutoEllipsis = true;

            // 
            // Clock Icon
            // 
            this.iconClock.Size = new Size(16, 16);
            this.iconClock.Location = new Point(20, 105);
            this.iconClock.Paint += (s, e) => GraphicsUtils.DrawClockIcon(e.Graphics, new Rectangle(0, 0, 16, 16), AppColors.TextSecondary);

            // 
            // Time Label
            // 
            this.lblTime.Font = new Font("Segoe UI", 9F);
            this.lblTime.ForeColor = AppColors.TextSecondary;
            this.lblTime.Location = new Point(42, 105);
            this.lblTime.AutoSize = true;

            // 
            // Add Controls
            // 
            this.pnlMain.Controls.Add(this.lblStatusBadge);
            this.pnlMain.Controls.Add(this.lblTime);
            this.pnlMain.Controls.Add(this.iconClock);
            this.pnlMain.Controls.Add(this.lblProblem);
            this.pnlMain.Controls.Add(this.lblMachineName);
            this.pnlMain.Controls.Add(this.iconMachine);
            this.pnlMain.Controls.Add(this.pnlColorStrip);
            
            this.Controls.Add(this.pnlMain);
            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }

        private void PnlMain_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath path = GraphicsUtils.GetRoundedRectangle(bounds, 8))
            {
                g.FillPath(new SolidBrush(pnlMain.BackColor), path);
                g.DrawPath(new Pen(Color.FromArgb(230, 230, 230), 1), path);
            }
        }
    }
}
