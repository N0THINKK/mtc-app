using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.features.rating.presentation.screens;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class TechnicianWorkQueueControl : UserControl
    {
        private readonly ITechnicianRepository _repository;
        private readonly Timer _timerRefresh;
        private List<TicketDto> _allTickets = new List<TicketDto>();
        private bool _isSystemActive = true;
        private bool _isLoading = false;
        private DateTime _currentStart = DateTime.Now.Date;
        private DateTime _currentEnd = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

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
            _repository = repository;
            
            _timerRefresh = new Timer();
            _timerRefresh.Interval = 30000;
            _timerRefresh.Tick += (s, e) => LoadData();

            InitializeComponent();
            SetupEventHandlers();
        }

        public void StartAutoRefresh()
        {
            if (!this.DesignMode)
            {
                LoadData();
                _timerRefresh.Start();
            }
        }

        public void StopAutoRefresh()
        {
            _timerRefresh.Stop();
        }

        // ========================================================
        // UI Construction
        // ========================================================
        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Surface;

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
            
            // [UPDATE] Menambahkan "Inspeksi" ke dalam dropdown
            cmbFilterStatus.Items.AddRange(new object[] { "Semua", "Belum Ditangani", "Sedang Diperbaiki", "Inspeksi", "Selesai" });
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
            btnClearFilters.FlatAppearance.BorderColor = AppColors.Separator;

            flowFilters.Controls.AddRange(new Control[] { lblFilterStatus, cmbFilterStatus, lblSortBy, cmbSortBy, btnClearFilters });
            filters.Controls.Add(flowFilters);

            return filters;
        }

        private Panel BuildTicketListPanel()
        {
            var ticketList = new Panel
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
            cmbFilterStatus.SelectedIndexChanged += (s, e) => RenderTickets();
            cmbSortBy.SelectedIndexChanged += (s, e) => RenderTickets();
            btnClearFilters.Click += (s, e) => ClearFilters();
            
            panelHeader.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(AppColors.Separator), 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };
            
            panelFilters.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(AppColors.Separator), 0, panelFilters.Height - 1, panelFilters.Width, panelFilters.Height - 1);
            };
        }

        // ========================================================
        // Data Loading & Rendering
        // ========================================================
        // [UPDATE] Mempertahankan fitur Mesin Beroperasi (a/b)
        public async void LoadData(DateTime? start = null, DateTime? end = null)
        {
            if (start.HasValue) _currentStart = start.Value;
            if (end.HasValue) _currentEnd = end.Value;
            
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                var ticketsRaw = await _repository.GetActiveTicketsAsync(_currentStart, _currentEnd);
                _allTickets = ticketsRaw.ToList();
                
                pnlTicketList.SuspendLayout();
                
                int openCount = _allTickets.Count(t => t.StatusId == 1);
                int processCount = _allTickets.Count(t => t.StatusId == 2);
                int doneCount = _allTickets.Count(t => t.StatusId == 3);
                
                // Ambil indikator Run/Total Mesin 10 menit
                var machineStats = await _repository.GetMachineRunStatsAsync();

                statsControl.UpdateStats(openCount, processCount, doneCount, machineStats.Running, machineStats.Total);
                lblLastUpdate.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";
                
                RenderTickets();
                
                pnlTicketList.ResumeLayout();
                UpdateStatusIndicator(true);
            }
            catch (Exception ex)
            {
                _timerRefresh.Stop();
                UpdateStatusIndicator(false);
                MessageBox.Show($"Gagal memuat daftar tiket: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void RenderTickets()
        {
            pnlTicketList.SuspendLayout();
            
            foreach (Control ctrl in pnlTicketList.Controls)
            {
                if (ctrl != panelEmptyState) 
                {
                    ctrl.Dispose();
                }
            }
            pnlTicketList.Controls.Clear();

            var filtered = _allTickets.AsEnumerable();

            int statusIndex = cmbFilterStatus.SelectedIndex;
            if (statusIndex == 1) filtered = filtered.Where(t => t.StatusId == 1);       // Belum Ditangani
            else if (statusIndex == 2) filtered = filtered.Where(t => t.StatusId == 2);  // Sedang Diperbaiki
            else if (statusIndex == 3) filtered = filtered.Where(t => t.StatusId == 3);  // Inspeksi [BARU] maps to DB Status 3
            else if (statusIndex == 4) filtered = filtered.Where(t => t.StatusId == 4);  // Selesai maps to DB Status 4
            else if (statusIndex == 5) filtered = filtered.Where(t => t.IsMachineRunning == 0); // Mesin Error (Run=0)

            int sortIndex = cmbSortBy.SelectedIndex;
            List<TicketDto> sortedList;

            // [UPDATE PENTING] Custom OrderBy untuk Urgensi
            if (sortIndex == 0)
            {
                // Urutan dipaksa menjadi: Open (1) -> Sedang Diperbaiki (2) -> Inspeksi (4) -> Selesai (3)
                sortedList = filtered.OrderByDescending(t => t.StatusId).ThenByDescending(t => t.CreatedAt).ToList();
            }
            else if (sortIndex == 1)
            {
                sortedList = filtered.OrderBy(t => t.CreatedAt).ToList();
            }
            else
            {
                sortedList = filtered.OrderByDescending(t => t.CreatedAt).ToList();
            }

            if (sortedList.Count == 0)
            {
                panelEmptyState.Visible = true;
                CenterEmptyState();
                pnlTicketList.Controls.Add(panelEmptyState);
            }
            else
            {
                panelEmptyState.Visible = false;
                foreach (var ticket in sortedList)
                {
                    var card = new TechnicianTicketCardControl();
                    card.UpdateDisplay(ticket);
                    card.OnCardClick += Card_OnCardClick;
                    pnlTicketList.Controls.Add(card);
                }
            }

            pnlTicketList.ResumeLayout();
        }

        private void Card_OnCardClick(object sender, long ticketId)
        {
            using (var form = new RatingTechnicianForm(ticketId))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        // ========================================================
        // Status & UI Helpers
        // ========================================================
        private void UpdateStatusIndicator(bool isActive)
        {
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

        private void CenterEmptyState()
        {
            panelEmptyState.Left = (pnlTicketList.ClientSize.Width - panelEmptyState.Width) / 2;
            panelEmptyState.Top = (pnlTicketList.ClientSize.Height - panelEmptyState.Height) / 2;
        }

        private void ClearFilters()
        {
            cmbFilterStatus.SelectedIndex = 0;
            cmbSortBy.SelectedIndex = 0;
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
}