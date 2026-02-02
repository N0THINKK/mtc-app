using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
        private Label lblProblem;
        private Label lblTechnicianName;
        private Label lblTime;
        private Label lblStatusBadge;
        private PictureBox iconMachine;
        private PictureBox iconClock;
        private AppStarRating starRating;
        
        private GroupLeaderTicketDto _currentTicket;
        public event EventHandler<Guid> OnValidate;
        private DateTime _lastClickTime = DateTime.MinValue;

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
            
            this.BackColor = Color.Transparent;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0, 0, 0, 10);
            this.Dock = DockStyle.Top;

            this.pnlMain.BackColor = Color.White;
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.Padding = new Padding(0);
            this.pnlMain.AutoSize = true;
            this.pnlMain.Paint += PnlMain_Paint;

            this.pnlColorStrip.Dock = DockStyle.Left;
            this.pnlColorStrip.Width = 6;

            // Table Layout - Same as Technician
            this.layoutTable.ColumnCount = 7;
            this.layoutTable.RowCount = 1;
            this.layoutTable.Dock = DockStyle.Top;
            this.layoutTable.Padding = new Padding(15, 15, 15, 15);
            this.layoutTable.AutoSize = true;
            this.layoutTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F)); 
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); 
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); 
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); 
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); 
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); 
            this.layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); 
            
            this.layoutTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Controls
            this.iconMachine.Size = new Size(32, 32);
            this.iconMachine.BackColor = Color.Transparent;
            this.iconMachine.SizeMode = PictureBoxSizeMode.CenterImage;
            this.iconMachine.Paint += (s, e) => DrawMachineIcon(e.Graphics);
            this.layoutTable.Controls.Add(this.iconMachine, 0, 0);

            this.lblMachineName.Font = AppFonts.Header3;
            this.lblMachineName.ForeColor = AppColors.TextPrimary;
            this.lblMachineName.AutoSize = true;
            this.lblMachineName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.lblMachineName.Margin = new Padding(0, 0, 20, 0);
            this.layoutTable.Controls.Add(this.lblMachineName, 1, 0);

            this.lblTechnicianName.Font = AppFonts.Subtitle;
            this.lblTechnicianName.ForeColor = AppColors.TextPrimary;
            this.lblTechnicianName.AutoSize = true;
            this.lblTechnicianName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.lblTechnicianName.Margin = new Padding(0, 0, 20, 0);
            this.layoutTable.Controls.Add(this.lblTechnicianName, 2, 0);

            this.lblProblem.Font = AppFonts.BodySmall;
            this.lblProblem.ForeColor = AppColors.TextSecondary;
            this.lblProblem.AutoSize = true;
            this.lblProblem.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.lblProblem.Margin = new Padding(0, 0, 20, 0);
            this.layoutTable.Controls.Add(this.lblProblem, 3, 0);

            this.starRating.IsReadOnly = true;
            this.starRating.Visible = false;
            this.starRating.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.starRating.Margin = new Padding(0, 0, 20, 0);
            this.layoutTable.Controls.Add(this.starRating, 4, 0);

            var pnlTime = new Panel { AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            this.iconClock.Size = new Size(16, 16);
            this.iconClock.Location = new Point(0, 4);
            this.iconClock.Paint += (s, e) => DrawClockIcon(e.Graphics);
            
            this.lblTime.Font = AppFonts.Body;
            this.lblTime.ForeColor = AppColors.Danger;
            this.lblTime.Location = new Point(20, 0);
            this.lblTime.AutoSize = true;
            
            pnlTime.Controls.Add(this.iconClock);
            pnlTime.Controls.Add(this.lblTime);
            this.layoutTable.Controls.Add(pnlTime, 5, 0);

            this.lblStatusBadge.Font = AppFonts.BodySmall;
            this.lblStatusBadge.AutoSize = true;
            this.lblStatusBadge.Padding = new Padding(15, 8, 15, 8);
            this.lblStatusBadge.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.lblStatusBadge.TextAlign = ContentAlignment.MiddleCenter;
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
            if (_currentTicket != null)
                OnValidate?.Invoke(this, _currentTicket.TicketUuid);
        }

        public void UpdateDisplay(GroupLeaderTicketDto ticket)
        {
            if (ticket == null) return;
            _currentTicket = ticket;

            this.lblMachineName.Text = ticket.MachineName ?? "-";

            // [UI-FIX] Format multi-problem strings into a numbered list
            if (!string.IsNullOrEmpty(ticket.FailureDetails))
            {
                var problems = ticket.FailureDetails.Split(new[] { " | " }, StringSplitOptions.None);
                this.lblProblem.Text = string.Join(Environment.NewLine, problems.Select((p, i) => $"{i + 1}. {p}"));
            }
            else
            {
                this.lblProblem.Text = "-";
            }
            
            if (!string.IsNullOrEmpty(ticket.TechnicianName))
            {
                this.lblTechnicianName.Text = ticket.TechnicianName;
                this.lblTechnicianName.Visible = true;
            }
            else
            {
                this.lblTechnicianName.Visible = false;
            }

            // GL Specific Status Logic
            bool isReviewed = ticket.GlValidatedAt.HasValue || (ticket.GlRatingScore.HasValue && ticket.GlRatingScore > 0);

            if (isReviewed)
            {
                // Sudah Direview
                this.pnlColorStrip.BackColor = Color.FromArgb(34, 197, 94); // Green
                this.lblStatusBadge.Text = "Sudah Direview";
                this.lblStatusBadge.BackColor = Color.FromArgb(240, 253, 244);
                this.lblStatusBadge.ForeColor = Color.FromArgb(21, 128, 61);
                
                this.starRating.Visible = true;
                this.starRating.Rating = ticket.GlRatingScore ?? 0;
            }
            else
            {
                // Belum Direview
                this.pnlColorStrip.BackColor = Color.FromArgb(249, 115, 22); // Orange
                this.lblStatusBadge.Text = "Belum Direview";
                this.lblStatusBadge.BackColor = Color.FromArgb(255, 247, 237);
                this.lblStatusBadge.ForeColor = Color.FromArgb(194, 65, 12);
                
                this.starRating.Visible = false;
            }

            this.lblTime.Text = FormatTime(ticket.CreatedAt);
        }

        private string FormatTime(DateTime time)
        {
            if (time.Date < DateTime.Now.Date) return time.ToString("dd MMM HH:mm");
            return time.ToString("HH:mm");
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
