using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.features.technician.data.dtos;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.features.technician.presentation.components
{
    public class TechnicianTicketCardControl : UserControl
    {
        // Layout Panels
        private Panel pnlMain;
        private Panel pnlColorStrip;
        private TableLayoutPanel layoutTable;
        
        // Content Controls
        private Label lblMachineName;
        private Label lblProblem;
        private Label lblTime;
        private Label lblStatusBadge;
        private Label lblTechnicianName;
        private PictureBox iconMachine;
        private PictureBox iconClock;
        private AppStarRating starRating;
        
        private TicketDto _currentTicket;

        public TechnicianTicketCardControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.pnlMain = new Panel();
            this.pnlColorStrip = new Panel();
            this.layoutTable = new TableLayoutPanel();
            this.lblMachineName = new Label();
            this.lblProblem = new Label();
            this.lblTime = new Label();
            this.lblStatusBadge = new Label();
            this.lblTechnicianName = new Label();
            this.iconMachine = new PictureBox();
            this.iconClock = new PictureBox();
            this.starRating = new AppStarRating();
            
            this.SuspendLayout();
            this.pnlMain.SuspendLayout();
            
            // 
            // Main UserControl - Responsive (Full Width like Excel row)
            // 
            this.BackColor = Color.Transparent;
            this.MinimumSize = new Size(300, 0); // Allow height to shrink
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Margin = new Padding(0); // Margin handled by Padding below
            this.Padding = new Padding(0, 0, 0, 15); // Bottom padding creates the gap between cards
            this.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.Dock = DockStyle.Top; // Takes full horizontal space

            // 
            // Main Panel (Card Container)
            // 
            this.pnlMain.BackColor = Color.White;
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.Padding = new Padding(0);
            this.pnlMain.AutoSize = true;
            this.pnlMain.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.pnlMain.Paint += PnlMain_Paint;

            // 
            // Color Strip (Left Border)
            // 
            this.pnlColorStrip.Dock = DockStyle.Left;
            this.pnlColorStrip.Width = 6;

            // 
            // TableLayoutPanel - Responsive Layout
            // 
            this.layoutTable.ColumnCount = 2;
            this.layoutTable.RowCount = 5;
            this.layoutTable.Dock = DockStyle.Top; // Changed to Top to work with AutoSize
            this.layoutTable.Padding = new Padding(15, 10, 15, 10);
            this.layoutTable.AutoSize = true;
            this.layoutTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            
            // Column styles: Icon column (fixed) and Content column (fill)
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35F));
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
            // Row styles: Auto-size rows
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 0: Machine icon + name + badge
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 1: Problem
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 2: Technician name
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 3: Rating
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 4: Time

            // 
            // Machine Icon
            // 
            this.iconMachine.Size = new Size(24, 24);
            this.iconMachine.BackColor = Color.Transparent;
            this.iconMachine.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.iconMachine.Paint += (s, e) => DrawMachineIcon(e.Graphics);
            this.layoutTable.Controls.Add(this.iconMachine, 0, 0);

            // 
            // Container for Machine Name and Status Badge
            // 
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(0),
                MinimumSize = new Size(0, 30)
            };
            
            // Machine Name Label
            this.lblMachineName.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            this.lblMachineName.ForeColor = AppColors.TextPrimary;
            this.lblMachineName.AutoSize = true;
            this.lblMachineName.MaximumSize = new Size(0, 0); // Will be set dynamically
            this.lblMachineName.Dock = DockStyle.Left;
            this.lblMachineName.Text = "Machine";
            
            // Status Badge
            this.lblStatusBadge.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblStatusBadge.AutoSize = true;
            this.lblStatusBadge.Padding = new Padding(12, 6, 12, 6);
            this.lblStatusBadge.Dock = DockStyle.Right;
            this.lblStatusBadge.Text = "Open";
            
            pnlHeader.Controls.Add(this.lblStatusBadge);
            pnlHeader.Controls.Add(this.lblMachineName);
            this.layoutTable.Controls.Add(pnlHeader, 1, 0);
            this.layoutTable.SetColumnSpan(pnlHeader, 1);

            // 
            // Problem Label (spans both columns for full width)
            // 
            this.lblProblem.Font = new Font("Segoe UI", 9.5F);
            this.lblProblem.ForeColor = AppColors.TextSecondary;
            this.lblProblem.AutoSize = true;
            this.lblProblem.MaximumSize = new Size(0, 60); // Will expand horizontally, limit vertically
            this.lblProblem.Dock = DockStyle.Fill;
            this.lblProblem.Padding = new Padding(0, 5, 0, 5);
            this.lblProblem.Text = "Problem description...";
            this.layoutTable.Controls.Add(this.lblProblem, 0, 1);
            this.layoutTable.SetColumnSpan(this.lblProblem, 2);

            // 
            // Technician Name Label (spans both columns)
            // 
            this.lblTechnicianName.Font = new Font("Segoe UI", 9F);
            this.lblTechnicianName.ForeColor = AppColors.TextPrimary;
            this.lblTechnicianName.AutoSize = true;
            this.lblTechnicianName.Dock = DockStyle.Fill;
            this.lblTechnicianName.Padding = new Padding(0, 3, 0, 3);
            this.lblTechnicianName.Text = "👤 Teknisi: John Doe";
            this.lblTechnicianName.Visible = false;
            this.layoutTable.Controls.Add(this.lblTechnicianName, 0, 2);
            this.layoutTable.SetColumnSpan(this.lblTechnicianName, 2);

            // 
            // Star Rating (spans both columns)
            // 
            this.starRating.Dock = DockStyle.Left;
            this.starRating.Padding = new Padding(0, 3, 0, 3);
            this.starRating.IsReadOnly = true; // Display only, not interactive
            this.starRating.Visible = false;
            // Don't set MinimumSize here - will be set conditionally based on status
            this.layoutTable.Controls.Add(this.starRating, 0, 3);
            this.layoutTable.SetColumnSpan(this.starRating, 2);

            // 
            // Time Container (Clock icon + time label)
            // 
            var pnlTime = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Height = 20
            };

            this.iconClock.Size = new Size(16, 16);
            this.iconClock.Location = new Point(0, 2);
            this.iconClock.BackColor = Color.Transparent;
            this.iconClock.Paint += (s, e) => DrawClockIcon(e.Graphics);

            this.lblTime.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblTime.ForeColor = AppColors.Danger;
            this.lblTime.Location = new Point(22, 0);
            this.lblTime.AutoSize = true;
            this.lblTime.Text = "Time";

            pnlTime.Controls.Add(this.lblTime);
            pnlTime.Controls.Add(this.iconClock);
            this.layoutTable.Controls.Add(pnlTime, 0, 4);
            this.layoutTable.SetColumnSpan(pnlTime, 2);

            // Add controls to main panel
            this.pnlMain.Controls.Add(this.layoutTable);
            this.pnlMain.Controls.Add(this.pnlColorStrip);

            // Add main panel to UserControl
            this.Controls.Add(this.pnlMain);

            // Hook interaction events
            HookEvents(this.pnlMain);

            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }

        // Event for card click
        public event EventHandler<long> OnCardClick;
        private DateTime _lastClickTime = DateTime.MinValue;

        private void HookEvents(Control control)
        {
            // 1. Hook the current control
            control.Click += (s, e) => HandleCardClick();
            
            // Cursor effect
            control.MouseEnter += (s, e) => {
                this.Cursor = Cursors.Hand;
                this.pnlMain.BackColor = Color.FromArgb(248, 250, 252);
            };
            
            control.MouseLeave += (s, e) => {
                // Only reset if we truly left the main panel bounds
                Point p = this.pnlMain.PointToClient(Cursor.Position);
                if (!this.pnlMain.ClientRectangle.Contains(p))
                {
                    this.Cursor = Cursors.Default;
                    this.pnlMain.BackColor = Color.White;
                }
            };

            // 2. Recurse for children
            foreach (Control child in control.Controls)
            {
                HookEvents(child);
            }
        }

        private void HandleCardClick()
        {
            // Debounce
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500) return;
            _lastClickTime = DateTime.Now;

            OnCardClick?.Invoke(this, _currentTicket.TicketId);
        }

        /// <summary>
        /// Updates the card display based on ticket data and status
        /// </summary>
        public void UpdateDisplay(TicketDto ticket)
        {
            if (ticket == null) return;
            
            _currentTicket = ticket;
            
            // Update common fields
            this.lblMachineName.Text = ticket.MachineName ?? "Unknown Machine";
            this.lblProblem.Text = ticket.FailureDetails ?? "No details provided";
            
            // Update status visuals
            UpdateStatusVisuals(ticket.StatusId);
            
            // Status-specific logic
            switch (ticket.StatusId)
            {
                case 1: // Open
                    UpdateForOpenStatus(ticket);
                    break;
                    
                case 2: // Repairing
                    UpdateForRepairingStatus(ticket);
                    break;
                    
                case 3: // Done
                    UpdateForDoneStatus(ticket);
                    break;
                    
                default:
                    // Fallback
                    this.lblTime.Text = FormatTime(ticket.CreatedAt);
                    this.lblTechnicianName.Visible = false;
                    this.starRating.Visible = false;
                    break;
            }
            
            // Adjust maximum size for responsive behavior
            AdjustMaximumSizes();
        }

        private void UpdateForOpenStatus(TicketDto ticket)
        {
            // Time Display: Show duration since created (how long it's been open)
            TimeSpan duration = DateTime.Now - ticket.CreatedAt;
            this.lblTime.Text = FormatDuration(duration);
            
            // Technician Name: Hidden
            this.lblTechnicianName.Visible = false;
            
            // Star Rating: Show 0 stars (not rated yet)
            this.starRating.MinimumSize = new Size(150, 30);
            this.starRating.Rating = 0;
            this.starRating.Visible = true;
        }

        private void UpdateForRepairingStatus(TicketDto ticket)
        {
            // Time Display: Show duration in repair (NOW - started_at)
            if (ticket.StartedAt.HasValue)
            {
                TimeSpan duration = DateTime.Now - ticket.StartedAt.Value;
                this.lblTime.Text = FormatDuration(duration);
            }
            else
            {
                // Fallback: show how long since created
                TimeSpan duration = DateTime.Now - ticket.CreatedAt;
                this.lblTime.Text = FormatDuration(duration);
            }
            
            // Technician Name: Visible
            if (!string.IsNullOrEmpty(ticket.TechnicianName))
            {
                this.lblTechnicianName.Text = $"👤 Teknisi: {ticket.TechnicianName}";
                this.lblTechnicianName.Visible = true;
            }
            else
            {
                this.lblTechnicianName.Visible = false;
            }
            
            // Star Rating: Show 0 stars (not rated yet)
            this.starRating.MinimumSize = new Size(150, 30);
            this.starRating.Rating = 0;
            this.starRating.Visible = true;
        }

        private void UpdateForDoneStatus(TicketDto ticket)
        {
            // Time Display: Always show with date format based on FinishedAt
            if (ticket.FinishedAt.HasValue)
            {
                this.lblTime.Text = FormatFinishedTime(ticket.FinishedAt.Value);
            }
            else
            {
                // Fallback if FinishedAt is null
                this.lblTime.Text = ticket.CreatedAt.ToString("dd MMM HH:mm");
            }
            
            // Technician Name: Visible
            if (!string.IsNullOrEmpty(ticket.TechnicianName))
            {
                this.lblTechnicianName.Text = $"👤 Teknisi: {ticket.TechnicianName}";
                this.lblTechnicianName.Visible = true;
            }
            else
            {
                this.lblTechnicianName.Visible = false;
            }
            
            // Star Rating: Always visible for Done status (show 0 if not reviewed)
            // Reserve space to keep time in same position as other statuses
            this.starRating.MinimumSize = new Size(150, 30);
            if (ticket.GlRatingScore.HasValue && ticket.GlRatingScore.Value > 0)
            {
                this.starRating.Rating = ticket.GlRatingScore.Value;
            }
            else
            {
                // Show 0 stars if not reviewed (display only)
                this.starRating.Rating = 0;
            }
            this.starRating.Visible = true;
        }

        /// <summary>
        /// Formats finished time - shows date if day has changed
        /// </summary>
        private string FormatFinishedTime(DateTime finishedAt)
        {
            // Check if the day has changed (not the same calendar day)
            if (finishedAt.Date == DateTime.Now.Date)
            {
                // Same day: Show HH:mm only
                return finishedAt.ToString("HH:mm");
            }
            else
            {
                // Different day: Show dd MMM HH:mm
                return finishedAt.ToString("dd MMM HH:mm");
            }
        }

        /// <summary>
        /// Formats duration in human-readable format (e.g., "2h 30m" or "45m")
        /// </summary>
        private string FormatDuration(TimeSpan duration)
        {
            int hours = (int)duration.TotalHours;
            int minutes = duration.Minutes;
            
            if (hours > 0)
            {
                return $"{hours}h {minutes}m";
            }
            else if (minutes > 0)
            {
                return $"{minutes}m";
            }
            else
            {
                return "< 1m";
            }
        }

        /// <summary>
        /// Generic time formatter (helper method)
        /// </summary>
        private string FormatTime(DateTime time)
        {
            return time.ToString("HH:mm");
        }

        /// <summary>
        /// Adjusts maximum sizes based on parent width for responsive behavior
        /// </summary>
        private void AdjustMaximumSizes()
        {
            if (this.Parent != null && this.Width > 0)
            {
                int availableWidth = this.Width - 70; // Account for padding and margins
                this.lblMachineName.MaximumSize = new Size(availableWidth - 120, 0); // Reserve space for badge
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

        private void UpdateStatusVisuals(int statusId)
        {
            Color stripColor;
            Color badgeBgColor;
            Color badgeTextColor;
            string badgeText;

            switch (statusId)
            {
                case 1: // Open
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