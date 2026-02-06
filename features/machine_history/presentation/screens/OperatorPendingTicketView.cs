using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    /// <summary>
    /// Operator's view for pending tickets (Status 1: Waiting, Status 2: Repairing).
    /// Uses timestamp-based timer calculation to persist elapsed time across app restarts.
    /// </summary>
    public class OperatorPendingTicketView : Form
    {
        private readonly MachineHistoryDto _ticket;
        private Timer _timer;
        private Label _lblTimer;
        private Label _lblStatus;

        public OperatorPendingTicketView(MachineHistoryDto ticket)
        {
            _ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));
            InitializeComponent();
            SetupTimer();
        }

        private void InitializeComponent()
        {
            this.Text = $"Ticket: {_ticket.TicketCode}";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppColors.CardBackground;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(30)
            };

            // Row 1: Ticket Code
            var lblCode = new Label
            {
                Text = _ticket.TicketCode ?? "PENDING",
                Font = AppFonts.Title,
                ForeColor = AppColors.Primary,
                AutoSize = true,
                Anchor = AnchorStyles.None
            };
            mainPanel.Controls.Add(lblCode, 0, 0);

            // Row 2: Machine Name
            var lblMachine = new Label
            {
                Text = _ticket.MachineName ?? "Unknown Machine",
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Anchor = AnchorStyles.None
            };
            mainPanel.Controls.Add(lblMachine, 0, 1);

            // Row 3: Issue
            var lblIssue = new Label
            {
                Text = $"Masalah: {_ticket.Issue ?? "-"}",
                Font = AppFonts.Body,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                MaximumSize = new Size(400, 0)
            };
            mainPanel.Controls.Add(lblIssue, 0, 2);

            // Row 4: Status (Dynamic)
            _lblStatus = new Label
            {
                Font = AppFonts.Subtitle,
                AutoSize = true,
                Anchor = AnchorStyles.None
            };
            UpdateStatusLabel();
            mainPanel.Controls.Add(_lblStatus, 0, 3);

            // Row 5: Timer (CRITICAL: Timestamp-based)
            _lblTimer = new Label
            {
                Text = "00:00:00",
                Font = new Font("Consolas", 36, FontStyle.Bold),
                ForeColor = (_ticket.StatusId == 1) ? Color.DarkOrange : AppColors.Success,
                AutoSize = true,
                Anchor = AnchorStyles.None
            };
            UpdateTimerLabel(); // Initial calculation
            mainPanel.Controls.Add(_lblTimer, 0, 4);

            // Row 6: Close Button
            var btnClose = new AppButton
            {
                Text = "Tutup",
                Type = AppButton.ButtonType.Secondary,
                Width = 150,
                Height = 45,
                Anchor = AnchorStyles.None
            };
            btnClose.Click += (s, e) => this.Close();
            mainPanel.Controls.Add(btnClose, 0, 5);

            // Row Styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Code
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Machine
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Issue
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Status
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Timer (fill)
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Button

            this.Controls.Add(mainPanel);
        }

        private void UpdateStatusLabel()
        {
            if (_ticket.StatusId == 1)
            {
                _lblStatus.Text = "⏳ Menunggu Teknisi...";
                _lblStatus.ForeColor = Color.DarkOrange;
            }
            else if (_ticket.StatusId == 2)
            {
                string techName = _ticket.TechnicianName ?? "Teknisi";
                _lblStatus.Text = $"🔧 Sedang Diperbaiki oleh {techName}";
                _lblStatus.ForeColor = AppColors.Success;
            }
        }

        private void SetupTimer()
        {
            _timer = new Timer { Interval = 1000 }; // 1 second
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTimerLabel();
        }

        /// <summary>
        /// CRITICAL: Calculate elapsed time from database timestamp, NOT from a counter variable.
        /// This ensures the timer persists correctly across app restarts.
        /// </summary>
        private void UpdateTimerLabel()
        {
            // Determine base time based on status
            DateTime baseTime;

            if (_ticket.StatusId == 1)
            {
                // Status 1 (Waiting): Timer from CreatedAt
                baseTime = _ticket.CreatedAt;
            }
            else if (_ticket.StatusId == 2 && _ticket.StartedAt.HasValue)
            {
                // Status 2 (Repairing): Timer from StartedAt
                baseTime = _ticket.StartedAt.Value;
            }
            else
            {
                // Fallback to CreatedAt
                baseTime = _ticket.CreatedAt;
            }

            TimeSpan elapsed = DateTime.Now - baseTime;
            
            // Handle negative elapsed (clock skew)
            if (elapsed.TotalSeconds < 0)
            {
                elapsed = TimeSpan.Zero;
            }

            _lblTimer.Text = elapsed.ToString(@"hh\:mm\:ss");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
