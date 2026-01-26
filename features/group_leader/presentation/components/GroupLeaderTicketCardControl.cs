using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.features.group_leader.data.dtos;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.features.group_leader.presentation.components
{
    public class GroupLeaderTicketCardControl : UserControl
    {
        // Layout Panels
        private Panel pnlMain;
        private Panel pnlColorStrip;
        private TableLayoutPanel layoutTable;
        
        // Content Controls
        private Label lblMachineName;
        private Label lblProblem; // Added to match Technician
        private Label lblTechnicianName;
        private Label lblTime;
        private Label lblStatusBadge;
        private PictureBox iconMachine;
        private PictureBox iconClock;
        private AppStarRating starRating;
        
        // Data
        private GroupLeaderTicketDto _currentTicket;
        
        // Output Event (Guid for GL flow)
        public event EventHandler<Guid> OnValidate;
        
        public GroupLeaderTicketCardControl(GroupLeaderTicketDto ticket)
        {
            InitializeComponent();
            UpdateDisplay(ticket);
        }

        private void InitializeComponent()
        {
            this.pnlMain = new Panel();
            this.pnlColorStrip = new Panel();
            this.layoutTable = new TableLayoutPanel();
            this.lblMachineName = new Label();
            this.lblProblem = new Label();
            this.lblTechnicianName = new Label();
            this.lblTime = new Label();
            this.lblStatusBadge = new Label();
            this.iconMachine = new PictureBox();
            this.iconClock = new PictureBox();
            this.starRating = new AppStarRating();
            
            this.SuspendLayout();
            this.pnlMain.SuspendLayout();
            
            // 
            // Main UserControl - Responsive
            // 
            this.BackColor = Color.Transparent;
            this.MinimumSize = new Size(300, 0);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0, 0, 0, 15); // Gap between cards
            this.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.Dock = DockStyle.Top;

            // 
            // Main Panel
            // 
            this.pnlMain.BackColor = Color.White;
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.Padding = new Padding(0);
            this.pnlMain.AutoSize = true;
            this.pnlMain.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.pnlMain.Paint += PnlMain_Paint;

            // 
            // Color Strip
            // 
            this.pnlColorStrip.Dock = DockStyle.Left;
            this.pnlColorStrip.Width = 6;

            // 
            // TableLayoutPanel
            // 
            this.layoutTable.ColumnCount = 2;
            this.layoutTable.RowCount = 5;
            this.layoutTable.Dock = DockStyle.Top;
            this.layoutTable.Padding = new Padding(15, 10, 15, 10);
            this.layoutTable.AutoSize = true;
            this.layoutTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35F));
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 0: Header
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 1: Problem
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 2: Technician
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 3: Stars
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 4: Time

            // 
            // Icon Machine
            // 
            this.iconMachine.Size = new Size(24, 24);
            this.iconMachine.BackColor = Color.Transparent;
            this.iconMachine.Paint += (s, e) => DrawMachineIcon(e.Graphics);
            this.layoutTable.Controls.Add(this.iconMachine, 0, 0);

            // 
            // Header Panel (Machine + Badge)
            // 
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(0),
                MinimumSize = new Size(0, 30)
            };
            
            this.lblMachineName.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            this.lblMachineName.ForeColor = AppColors.TextPrimary;
            this.lblMachineName.AutoSize = true;
            this.lblMachineName.Dock = DockStyle.Left;
            
            this.lblStatusBadge.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblStatusBadge.AutoSize = true;
            this.lblStatusBadge.Padding = new Padding(12, 6, 12, 6);
            this.lblStatusBadge.Dock = DockStyle.Right;
            
            pnlHeader.Controls.Add(this.lblStatusBadge);
            pnlHeader.Controls.Add(this.lblMachineName);
            this.layoutTable.Controls.Add(pnlHeader, 1, 0);
            this.layoutTable.SetColumnSpan(pnlHeader, 1);

            // 
            // Problem Label
            // 
            this.lblProblem.Font = new Font("Segoe UI", 9.5F);
            this.lblProblem.ForeColor = AppColors.TextSecondary;
            this.lblProblem.AutoSize = true;
            this.lblProblem.MaximumSize = new Size(0, 60);
            this.lblProblem.Dock = DockStyle.Fill;
            this.lblProblem.Padding = new Padding(0, 5, 0, 5);
            this.layoutTable.Controls.Add(this.lblProblem, 0, 1);
            this.layoutTable.SetColumnSpan(this.lblProblem, 2);

            // 
            // Technician Name
            // 
            this.lblTechnicianName.Font = new Font("Segoe UI", 9F);
            this.lblTechnicianName.ForeColor = AppColors.TextPrimary;
            this.lblTechnicianName.AutoSize = true;
            this.lblTechnicianName.Dock = DockStyle.Fill;
            this.lblTechnicianName.Padding = new Padding(0, 3, 0, 3);
            this.layoutTable.Controls.Add(this.lblTechnicianName, 0, 2);
            this.layoutTable.SetColumnSpan(this.lblTechnicianName, 2);

            // 
            // Star Rating
            // 
            this.starRating.Dock = DockStyle.Left;
            this.starRating.Padding = new Padding(0, 3, 0, 3);
            this.starRating.IsReadOnly = true;
            this.starRating.Visible = false;
            this.layoutTable.Controls.Add(this.starRating, 0, 3);
            this.layoutTable.SetColumnSpan(this.starRating, 2);

            // 
            // Time Container
            // 
            var pnlTime = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Height = 24
            };

            this.iconClock.Size = new Size(16, 16);
            this.iconClock.Location = new Point(0, 4);
            this.iconClock.BackColor = Color.Transparent;
            this.iconClock.Paint += (s, e) => DrawClockIcon(e.Graphics);

            this.lblTime.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblTime.ForeColor = AppColors.Danger;
            this.lblTime.Location = new Point(22, 2);
            this.lblTime.AutoSize = true;

            pnlTime.Controls.Add(this.lblTime);
            pnlTime.Controls.Add(this.iconClock);
            this.layoutTable.Controls.Add(pnlTime, 0, 4);
            this.layoutTable.SetColumnSpan(pnlTime, 2);

            this.pnlMain.Controls.Add(this.layoutTable);
            this.pnlMain.Controls.Add(this.pnlColorStrip);
            this.Controls.Add(this.pnlMain);

            // Hook Events
            HookEvents(this.pnlMain);

            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }

        private DateTime _lastClickTime = DateTime.MinValue;

        private void HookEvents(Control control)
        {
            control.Click += (s, e) => HandleCardClick();
            
            control.MouseEnter += (s, e) => {
                this.Cursor = Cursors.Hand;
                this.pnlMain.BackColor = Color.FromArgb(248, 250, 252);
            };
            
            control.MouseLeave += (s, e) => {
                Point p = this.pnlMain.PointToClient(Cursor.Position);
                if (!this.pnlMain.ClientRectangle.Contains(p))
                {
                    this.Cursor = Cursors.Default;
                    this.pnlMain.BackColor = Color.White;
                }
            };

            foreach (Control child in control.Controls)
            {
                 if (child is AppButton) continue;
                 HookEvents(child);
            }
        }

        private void HandleCardClick()
        {
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500) return;
            _lastClickTime = DateTime.Now;

            if (_currentTicket != null)
                OnValidate?.Invoke(this, _currentTicket.TicketUuid);
        }

        public void UpdateDisplay(GroupLeaderTicketDto ticket)
        {
            if (ticket == null) return;
            _currentTicket = ticket;

            this.lblMachineName.Text = ticket.MachineName ?? "Unknown Machine";
            this.lblProblem.Text = ticket.FailureDetails ?? "No details provided";
            this.lblTechnicianName.Text = $"👤 Teknisi: {ticket.TechnicianName ?? "-"}";

            bool isReviewed = ticket.GlValidatedAt.HasValue || (ticket.GlRatingScore.HasValue && ticket.GlRatingScore > 0);

            // Status Visuals
            if (isReviewed)
            {
                // Sudah Direview
                this.pnlColorStrip.BackColor = Color.FromArgb(34, 197, 94); // Green
                this.lblStatusBadge.Text = "Sudah Direview";
                this.lblStatusBadge.BackColor = Color.FromArgb(240, 253, 244);
                this.lblStatusBadge.ForeColor = Color.FromArgb(21, 128, 61);
                
                // Show Rating
                this.starRating.Visible = true;
                this.starRating.MinimumSize = new Size(150, 30);
                this.starRating.Rating = ticket.GlRatingScore ?? 0;
            }
            else
            {
                // Belum Direview
                this.pnlColorStrip.BackColor = Color.FromArgb(249, 115, 22); // Orange
                this.lblStatusBadge.Text = "Belum Direview";
                this.lblStatusBadge.BackColor = Color.FromArgb(255, 247, 237);
                this.lblStatusBadge.ForeColor = Color.FromArgb(194, 65, 12);
                
                // Show empty rating placeholder or nothing? 
                // Technician card shows 0 stars if pending.
                this.starRating.Visible = true;
                this.starRating.MinimumSize = new Size(150, 30);
                this.starRating.Rating = 0;
            }

            this.lblTime.Text = FormatTime(ticket.CreatedAt);
            
            AdjustMaximumSizes();
        }

        private string FormatTime(DateTime time)
        {
            if (time.Date < DateTime.Now.Date)
            {
                return time.ToString("dd MMM HH:mm");
            }
            return time.ToString("HH:mm");
        }
        
        private void AdjustMaximumSizes()
        {
            if (this.Parent != null && this.Width > 0)
            {
                int availableWidth = this.Width - 70;
                this.lblMachineName.MaximumSize = new Size(availableWidth - 120, 0);
                this.lblProblem.MaximumSize = new Size(availableWidth, 60);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustMaximumSizes();
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

        private void DrawMachineIcon(Graphics g)
        {
             g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(AppColors.Primary, 2))
            {
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
                g.DrawEllipse(pen, 2, 2, 12, 12);
                g.DrawLine(pen, 8, 8, 8, 5);
                g.DrawLine(pen, 8, 8, 11, 8);
            }
        }
    }
}
