using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.features.technician.presentation.controllers;
using mtc_app.features.rating.presentation.screens;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class TechnicianWorkQueueControl : UserControl, ITechnicianWorkQueueView
    {
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
        private const int WM_SETREDRAW = 0x000B;

        private readonly TechnicianWorkQueueController _controller;
        private readonly Timer _timerRefresh;
        
        private DateTime _currentStart = DateTime.Now.Date;
        private DateTime _currentEnd = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
        private bool _isSystemActive = true;

        // UI Controls
        private Panel panelHeader;
        private Panel panelStatusBar;
        private Panel panelFilters;
        private Panel pnlTicketList;
        private Panel panelEmptyState;
        
        private TechnicianWorkQueueStatsControl statsControl;
        private Label lblLastUpdate;
        private Label lblSystemStatus;
        private PictureBox picStatusIndicator;
        private Label lblEmptyTitle;
        private Label lblEmptyMessage;
        private PictureBox picEmptyIcon;
        
        private ComboBox cmbFilterStatus;
        private ComboBox cmbSortBy;
        private Button btnClearFilters;
        
        public TechnicianWorkQueueControl(ITechnicianRepository repository)
        {
            _controller = new TechnicianWorkQueueController(this, repository);
            
            _timerRefresh = new Timer();
            _timerRefresh.Interval = 60000; // 1 menit
            _timerRefresh.Tick += async (s, e) => await _controller.LoadDataAsync(_currentStart, _currentEnd);

            InitializeComponent();
            SetupEventHandlers();
        }

        public async void StartAutoRefresh()
        {
            if (!this.DesignMode)
            {
                await _controller.LoadDataAsync(_currentStart, _currentEnd);
                _timerRefresh.Start();
            }
        }

        public void StopAutoRefresh()
        {
            _timerRefresh.Stop();
        }

        public async void LoadData(DateTime? start = null, DateTime? end = null)
        {
            if (start.HasValue) _currentStart = start.Value;
            if (end.HasValue) _currentEnd = end.Value;
            await _controller.LoadDataAsync(_currentStart, _currentEnd);
        }

        // ========================================================
        // ITechnicianWorkQueueView Implementation
        // ========================================================
        
        public int SelectedStatusFilterIndex => cmbFilterStatus.SelectedIndex;
        public int SelectedSortIndex => cmbSortBy.SelectedIndex;

        public void UpdateStatusIndicator(bool isActive, string timestampText)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatusIndicator(isActive, timestampText)));
                return;
            }

            lblLastUpdate.Text = timestampText;
            _isSystemActive = isActive;
            
            if (isActive)
            {
                panelStatusBar.BackColor = Color.FromArgb(240, 253, 244);
                lblSystemStatus.Text = "Sistem Aktif";
                lblSystemStatus.ForeColor = Color.FromArgb(21, 128, 61);
            }
            else
            {
                panelStatusBar.BackColor = Color.FromArgb(254, 242, 242);
                lblSystemStatus.Text = "Sistem Error";
                lblSystemStatus.ForeColor = Color.FromArgb(185, 28, 28);
            }
            picStatusIndicator.Invalidate();
        }

        public void UpdateStats(int openCount, int processCount, int doneCount, int machineRunning, int machineTotal)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStats(openCount, processCount, doneCount, machineRunning, machineTotal)));
                return;
            }
            statsControl.UpdateStats(openCount, processCount, doneCount, machineRunning, machineTotal);
        }

        public void RenderTickets(List<TicketDto> tickets)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => RenderTickets(tickets)));
                return;
            }

            SendMessage(pnlTicketList.Handle, WM_SETREDRAW, false, 0);
            try
            {
                pnlTicketList.SuspendLayout();
                
                var toDispose = pnlTicketList.Controls.Cast<Control>()
                    .Where(c => c != panelEmptyState).ToList();
                pnlTicketList.Controls.Clear();
                foreach (var ctrl in toDispose) ctrl.Dispose();

                panelEmptyState.Visible = false;
                foreach (var ticket in tickets)
                {
                    var card = new TechnicianTicketCardControl();
                    card.UpdateDisplay(ticket);
                    card.OnCardClick += Card_OnCardClick;
                    pnlTicketList.Controls.Add(card);
                }

                pnlTicketList.ResumeLayout(true);
            }
            finally
            {
                SendMessage(pnlTicketList.Handle, WM_SETREDRAW, true, 0);
                pnlTicketList.Invalidate(true);
                pnlTicketList.Update();
            }
        }

        public void ShowEmptyState(string title, string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowEmptyState(title, message)));
                return;
            }

            var toDispose = pnlTicketList.Controls.Cast<Control>()
                    .Where(c => c != panelEmptyState).ToList();
            pnlTicketList.Controls.Clear();
            foreach (var ctrl in toDispose) ctrl.Dispose();

            lblEmptyTitle.Text = title;
            lblEmptyMessage.Text = message;
            panelEmptyState.Visible = true;
            CenterEmptyState();
            pnlTicketList.Controls.Add(panelEmptyState);
        }

        public void ShowError(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowError(message)));
                return;
            }
            MessageBox.Show($"Gagal memuat daftar tiket: {message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ========================================================
        // UI Construction
        // ========================================================
        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Surface;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            panelHeader = BuildHeaderPanel();
            panelStatusBar = BuildStatusBar();
            panelFilters = BuildFilterPanel();
            pnlTicketList = BuildTicketListPanel();

            this.Controls.Add(pnlTicketList); 
            this.Controls.Add(panelFilters);  
            this.Controls.Add(panelHeader);   
            this.Controls.Add(panelStatusBar);
        }

        private Panel BuildHeaderPanel()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = AppDimens.HeaderHeightLarge,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(AppDimens.SpacingXL, AppDimens.MarginLarge, AppDimens.SpacingXL, AppDimens.MarginLarge)
            };

            statsControl = new TechnicianWorkQueueStatsControl
            {
                Location = new Point(20, 10),
                Size = new Size(1340, 100),
                BackColor = Color.Transparent
            };
            header.Controls.Add(statsControl);

            return header;
        }

        private Panel BuildStatusBar()
        {
            var statusBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(AppDimens.SpacingXL, 0, AppDimens.SpacingXL, 0)
            };

            var flowStatus = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            picStatusIndicator = new PictureBox
            {
                Size = new Size(12, 12),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 22, AppDimens.MarginSmall, 0)
            };
            picStatusIndicator.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color color = _isSystemActive ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
                e.Graphics.FillEllipse(new SolidBrush(color), 0, 0, 12, 12);
            };

            lblSystemStatus = new Label
            {
                Text = "Sistem Aktif",
                Font = AppFonts.Subtitle,
                ForeColor = Color.FromArgb(21, 128, 61),
                AutoSize = true,
                Margin = new Padding(0, 18, AppDimens.MarginLarge, 0)
            };

            lblLastUpdate = new Label
            {
                Text = "Terakhir diperbarui: -",
                Font = AppFonts.BodySmall,
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 19, 0, 0)
            };

            flowStatus.Controls.AddRange(new Control[] { picStatusIndicator, lblSystemStatus, lblLastUpdate });
            statusBar.Controls.Add(flowStatus);

            return statusBar;
        }

        private Panel BuildFilterPanel()
        {
            var filters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = AppColors.CardBackground
            };

            var flowFilters = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Padding = new Padding(AppDimens.SpacingXL, 0, 0, 0),
                BackColor = Color.Transparent
            };

            var lblFilterStatus = new Label 
            { 
                Text = "Filter:", 
                AutoSize = true, 
                Font = AppFonts.Body,
                Margin = new Padding(0, 23, AppDimens.MarginSmall, 0)
            };

            cmbFilterStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(160, 28),
                Font = AppFonts.Body,
                Margin = new Padding(0, 20, AppDimens.MarginLarge, 0)
            };
            
            cmbFilterStatus.Items.AddRange(new object[] { "Semua", "Belum Ditangani", "Sedang Diperbaiki", "Inspeksi", "Selesai", "Mesin Error" });
            cmbFilterStatus.SelectedIndex = 0;

            var lblSortBy = new Label 
            { 
                Text = "Urutkan:", 
                AutoSize = true, 
                Font = AppFonts.Body,
                Margin = new Padding(0, 23, AppDimens.MarginSmall, 0)
            };

            cmbSortBy = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(200, 28),
                Font = AppFonts.Body,
                Margin = new Padding(0, 20, AppDimens.MarginLarge, 0)
            };
            cmbSortBy.Items.AddRange(new object[] { "Default (Urgensi)", "Terbaru (Waktu)", "Terlama (Waktu)" });
            cmbSortBy.SelectedIndex = 0;

            btnClearFilters = new Button
            {
                Text = "Reset",
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppColors.Surface,
                ForeColor = AppColors.TextSecondary,
                Margin = new Padding(0, 19, 0, 0)
            };
            flowFilters.Controls.AddRange(new Control[] { lblFilterStatus, cmbFilterStatus, lblSortBy, cmbSortBy, btnClearFilters });
            filters.Controls.Add(flowFilters);

            return filters;
        }

        private Panel BuildTicketListPanel()
        {
            var ticketList = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(AppDimens.MarginLarge),
                BackColor = AppColors.Surface
            };

            panelEmptyState = new Panel
            {
                Size = new Size(300, 200),
                Visible = false,
                BackColor = Color.Transparent
            };

            picEmptyIcon = new PictureBox { Size = new Size(60, 60), Location = new Point(120, 20), BackColor = Color.Transparent };
            picEmptyIcon.Paint += (s, e) => DrawEmptyIcon(e.Graphics);

            lblEmptyTitle = new Label
            {
                Text = "Tidak Ada Tiket",
                Font = AppFonts.Header3,
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(50, 90),
                Size = new Size(200, 25)
            };

            lblEmptyMessage = new Label
            {
                Text = "Semua tiket telah diproses.",
                Font = AppFonts.BodySmall,
                ForeColor = AppColors.TextSecondary,
                TextAlign = ContentAlignment.TopCenter,
                Location = new Point(50, 120),
                Size = new Size(200, 40)
            };

            panelEmptyState.Controls.AddRange(new Control[] { picEmptyIcon, lblEmptyTitle, lblEmptyMessage });
            ticketList.Controls.Add(panelEmptyState);

            return ticketList;
        }

        // ========================================================
        // Event Handlers
        // ========================================================
        private void SetupEventHandlers()
        {
            cmbFilterStatus.SelectedIndexChanged += (s, e) => _controller.ForceReRender();
            cmbSortBy.SelectedIndexChanged += (s, e) => _controller.ForceReRender();
            btnClearFilters.Click += (s, e) => {
                cmbFilterStatus.SelectedIndex = 0;
                cmbSortBy.SelectedIndex = 0;
            };
            
            panelHeader.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(AppColors.Separator), 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };
            
            panelFilters.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(AppColors.Separator), 0, panelFilters.Height - 1, panelFilters.Width, panelFilters.Height - 1);
            };
        }

        private async void Card_OnCardClick(object sender, long ticketId)
        {
            using (var form = new RatingTechnicianForm(ticketId))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    await _controller.LoadDataAsync(_currentStart, _currentEnd);
                }
            }
        }

        // ========================================================
        // UI Helpers
        // ========================================================

        private void CenterEmptyState()
        {
            panelEmptyState.Left = (pnlTicketList.ClientSize.Width - panelEmptyState.Width) / 2;
            panelEmptyState.Top = (pnlTicketList.ClientSize.Height - panelEmptyState.Height) / 2;
        }

        private void DrawEmptyIcon(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Color.FromArgb(203, 213, 225), 2))
            {
                g.DrawRectangle(pen, 10, 15, 40, 40);
                g.DrawRectangle(pen, 22, 10, 16, 8);
                using (Pen checkPen = new Pen(Color.FromArgb(34, 197, 94), 3))
                {
                    g.DrawLine(checkPen, 20, 35, 26, 41);
                    g.DrawLine(checkPen, 26, 41, 40, 27);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerRefresh?.Stop();
                _timerRefresh?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            this.UpdateStyles();
        }
    }
}