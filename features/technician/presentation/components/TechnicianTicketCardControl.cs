using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
        private Label lblTechnicianName;
        private Label lblTime;
        private Label lblStatusBadge; // Now a button-like label
        private PictureBox iconMachine;
        private PictureBox iconClock;
        private AppStarRating starRating;
        
        private TicketDto _currentTicket;
        public event EventHandler<long> OnCardClick;
        private DateTime _lastClickTime = DateTime.MinValue;

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
            this.lblTechnicianName = new Label();
            this.lblTime = new Label();
            this.lblStatusBadge = new Label();
            this.iconMachine = new PictureBox();
            this.iconClock = new PictureBox();
            this.starRating = new AppStarRating();
            
            this.SuspendLayout();
            this.pnlMain.SuspendLayout();
            
            // 
            // Main UserControl
            // 
            this.BackColor = Color.Transparent;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0, 0, 0, 10); // Spacing between cards
            this.Dock = DockStyle.Top;

            // 
            // Main Panel
            // 
            this.pnlMain.BackColor = Color.White;
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.Padding = new Padding(0);
            this.pnlMain.AutoSize = true;
            this.pnlMain.Paint += PnlMain_Paint;

            // 
            // Color Strip
            // 
            this.pnlColorStrip.Dock = DockStyle.Left;
            this.pnlColorStrip.Width = 6;

            // 
            // TableLayoutPanel (Horizontal Layout)
            // 
            this.layoutTable.ColumnCount = 7;
            this.layoutTable.RowCount = 1;
            this.layoutTable.Dock = DockStyle.Top;
            this.layoutTable.Padding = new Padding(15, 15, 15, 15); // Bigger padding
            this.layoutTable.AutoSize = true;
            this.layoutTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            
            // Columns: Icon | Machine | Tech | Problem | Stars | Time | Badge
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F)); // Icon
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Machine
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Technician
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Problem (Fills gap)
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Stars
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Time
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Badge
            
            // Row styling (single row, center vertically)
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 
            // Controls
            // 

            // 1. Icon
            this.iconMachine.Size = new Size(32, 32); // Bigger icon
            this.iconMachine.BackColor = Color.Transparent;
            this.iconMachine.SizeMode = PictureBoxSizeMode.CenterImage;
            this.iconMachine.Paint += (s, e) => DrawMachineIcon(e.Graphics);
            this.layoutTable.Controls.Add(this.iconMachine, 0, 0);

            // 2. Machine Name (Bigger Font)
            this.lblMachineName.Font = AppFonts.MetricSmall;
            this.lblMachineName.ForeColor = AppColors.TextPrimary;
            this.lblMachineName.AutoSize = true;
            this.lblMachineName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.lblMachineName.Margin = new Padding(0, 0, 20, 0); // Spacing
            this.layoutTable.Controls.Add(this.lblMachineName, 1, 0);

            // 3. Technician Name
            this.lblTechnicianName.Font = AppFonts.Header3;
            this.lblTechnicianName.ForeColor = AppColors.TextPrimary; // Darker
            this.lblTechnicianName.AutoSize = true;
            this.lblTechnicianName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.lblTechnicianName.Margin = new Padding(0, 0, 20, 0);
            this.layoutTable.Controls.Add(this.lblTechnicianName, 2, 0);

            // 4. Problem (Truncated if too long, fills middle)
            this.lblProblem.Font = AppFonts.Title;
            this.lblProblem.ForeColor = AppColors.TextSecondary;
            this.lblProblem.AutoSize = true;
            this.lblProblem.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.lblProblem.Margin = new Padding(0, 0, 20, 0);
            this.layoutTable.Controls.Add(this.lblProblem, 3, 0);

            // 5. Star Rating
            this.starRating.IsReadOnly = true;
            this.starRating.Visible = false;
            this.starRating.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.starRating.Margin = new Padding(0, 0, 20, 0);
            this.layoutTable.Controls.Add(this.starRating, 4, 0);

            // 6. Time (Icon + Text)
            var pnlTime = new Panel { AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            this.iconClock.Size = new Size(16, 16);
            this.iconClock.Location = new Point(0, 4);
            this.iconClock.Paint += (s, e) => DrawClockIcon(e.Graphics);
            
            this.lblTime.Font = AppFonts.Title;
            this.lblTime.ForeColor = AppColors.Danger;
            this.lblTime.Location = new Point(20, 0);
            this.lblTime.AutoSize = true;
            
            pnlTime.Controls.Add(this.iconClock);
            pnlTime.Controls.Add(this.lblTime);
            this.layoutTable.Controls.Add(pnlTime, 5, 0);

            // 7. Status Badge
            this.lblStatusBadge.Font = AppFonts.Subtitle;
            this.lblStatusBadge.AutoSize = true;
            this.lblStatusBadge.Padding = new Padding(15, 8, 15, 8); // Bigger badge
            this.lblStatusBadge.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.lblStatusBadge.TextAlign = ContentAlignment.MiddleCenter;
            // Set rounded corners? Standard label doesn't support it easily without paint, 
            // but user loved the flat badge look in previous request.
            // Screenshot shows an Orange Button-like thing "Belum Direview".
            // So we use background color.
            this.layoutTable.Controls.Add(this.lblStatusBadge, 6, 0);

            this.pnlMain.Controls.Add(this.layoutTable);
            this.pnlMain.Controls.Add(this.pnlColorStrip);
            this.Controls.Add(this.pnlMain);

            HookEvents(this.pnlMain);

            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }

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
            foreach (Control child in control.Controls) HookEvents(child);
        }

        private void HandleCardClick()
        {
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500) return;
            _lastClickTime = DateTime.Now;
            OnCardClick?.Invoke(this, _currentTicket?.TicketId ?? 0); // Technician uses Long ID
        }

        public void UpdateDisplay(TicketDto ticket)
        {
            if (ticket == null) return;
            _currentTicket = ticket;

            this.lblMachineName.Text = ticket.MachineName ?? "-";
            
            // [UI-FIX] Format multi-problem strings into a numbered list
            if (!string.IsNullOrEmpty(ticket.FailureDetails))
            {
                var problems = ticket.FailureDetails.Split(new[] { " | " }, StringSplitOptions.None);
                if (problems.Length > 1)
                {
                    var numberedProblems = problems.Select((problem, index) => $"{index + 1}. {problem}");
                    this.lblProblem.Text = string.Join(Environment.NewLine, numberedProblems);
                }
                else
                {
                    this.lblProblem.Text = ticket.FailureDetails;
                }
            }
            else
            {
                this.lblProblem.Text = "-";
            }
            
            // Technician Name
             if (!string.IsNullOrEmpty(ticket.TechnicianName))
            {
                this.lblTechnicianName.Text = ticket.TechnicianName;
                this.lblTechnicianName.Visible = true;
            }
            else
            {
                this.lblTechnicianName.Visible = false;
            }

            // Status
            UpdateStatusVisuals(ticket.StatusId);

            // Time & Rating Logic
            if (ticket.StatusId == 3) // Done
            {
                 if (ticket.FinishedAt.HasValue)
                     this.lblTime.Text = FormatFinishedTime(ticket.FinishedAt.Value);
                 else
                     this.lblTime.Text = FormatTime(ticket.CreatedAt);

                 this.starRating.Visible = true;
                 this.starRating.Rating = ticket.GlRatingScore ?? 0;
            }
            else
            {
                 // Open/Repairing
                 TimeSpan duration = DateTime.Now - (ticket.StartedAt ?? ticket.CreatedAt);
                 this.lblTime.Text = FormatDuration(duration);
                 this.starRating.Visible = false;
            }
            
            // Adjust visibility if problem text is empty to avoid gap?
            // TableLayoutPanel handles distinct columns.
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
                    stripColor = Color.FromArgb(239, 68, 68);
                    badgeBgColor = Color.FromArgb(254, 242, 242);
                    badgeTextColor = Color.FromArgb(185, 28, 28);
                    badgeText = "Open";
                    break;
                case 2: // Repairing
                    stripColor = Color.FromArgb(234, 179, 8); // Yellow/Gold
                    badgeBgColor = Color.FromArgb(254, 252, 232); // Light Yellow
                    badgeTextColor = Color.FromArgb(161, 98, 7); // Dark Yellow/Brown
                    badgeText = "Sedang Diperbaiki";
                    break;
                case 3: // Done
                    stripColor = Color.FromArgb(34, 197, 94);
                    badgeBgColor = Color.FromArgb(240, 253, 244);
                    badgeTextColor = Color.FromArgb(21, 128, 61);
                    badgeText = "Selesai";
                    break;
                default:
                    stripColor = AppColors.Primary;
                    badgeBgColor = Color.LightGray;
                    badgeTextColor = Color.Black;
                    badgeText = "Unknown";
                    break;
            }

            this.pnlColorStrip.BackColor = stripColor;
            this.lblStatusBadge.BackColor = badgeBgColor;
            this.lblStatusBadge.ForeColor = badgeTextColor;
            this.lblStatusBadge.Text = badgeText;
        }

        private string FormatFinishedTime(DateTime time)
        {
            return time.ToString("dd MMM HH:mm");
        }

        private string FormatTime(DateTime time)
        {
            return time.ToString("dd MMM HH:mm");
        }

        private string FormatDuration(TimeSpan d)
        {
            if (d.TotalHours >= 1) return $"{(int)d.TotalHours}h {d.Minutes}m";
            return $"{d.Minutes}m";
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
                // Bigger Icon Drawing for 32x32
                g.DrawRectangle(pen, 6, 6, 20, 20);
                g.DrawLine(pen, 11, 6, 11, 3);
                g.DrawLine(pen, 21, 6, 21, 3);
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