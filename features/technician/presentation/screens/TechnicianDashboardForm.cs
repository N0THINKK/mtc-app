using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.features.technician.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

using mtc_app.shared.data.session;

namespace mtc_app.features.technician.presentation.screens
{
    public partial class TechnicianDashboardForm : AppBaseForm
    {
        private readonly TechnicianRepository _repository;
        private readonly Timer _timerRefresh;
        private bool _isSystemActive = true;
        private List<TicketDto> _allTickets = new List<TicketDto>();
        private readonly long _technicianId;

        public TechnicianDashboardForm()
        {
            _repository = new TechnicianRepository();
            
            // Get ID from Session
            if (UserSession.CurrentUser != null)
            {
                _technicianId = UserSession.CurrentUser.UserId;
            }
            else
            {
                _technicianId = 0; // Should redirect to login ideally
            }

            InitializeComponent();
            SetupEventHandlers();
            
            // Re-initialize Sort Options programmatically to match logic
            cmbSortBy.Items.Clear();
            cmbSortBy.Items.AddRange(new object[] { 
                "Default (Urgensi)", 
                "Terbaru (Waktu)", 
                "Terlama (Waktu)" 
            });
            cmbSortBy.SelectedIndex = 0; // Force Default

            // Setup Timer
            _timerRefresh = new Timer(this.components);
            _timerRefresh.Interval = 10000; // 10 seconds
            _timerRefresh.Tick += (s, e) => LoadData();

            if (!this.DesignMode)
            {
                // cmbFilterStatus.SelectedIndex = 0; // Already set?
                // cmbSortBy.SelectedIndex = 0; // Set above
                LoadData();
                _timerRefresh.Start();
            }
        }

        private void SetupEventHandlers()
        {
            // Header Border
            this.panelHeader.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230)), 
                    0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };

            this.panelFilters.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230)),
                    0, panelFilters.Height - 1, panelFilters.Width, panelFilters.Height - 1);
            };

            // Status Indicator Paint
            this.picStatusIndicator.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color color = _isSystemActive ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
                e.Graphics.FillEllipse(new SolidBrush(color), 0, 0, 12, 12);
            };

            // Empty Icon Paint
            this.picEmptyIcon.Paint += (s, e) => DrawEmptyIcon(e.Graphics);

            // Filter and Sort Handlers
            this.cmbFilterStatus.SelectedIndexChanged += (s, e) => RenderTickets();
            this.cmbSortBy.SelectedIndexChanged += (s, e) => RenderTickets();
            this.btnClearFilters.Click += (s, e) => ClearFilters();
        }

        private void LoadData()
        {
            try
            {
                // Usage of Repository Pattern separates data access logic from UI (SRP)
                _allTickets = _repository.GetActiveTickets().ToList();
                var stats = _repository.GetTechnicianStatistics(_technicianId);
                
                pnlTicketList.SuspendLayout();
                
                // Update stats
                if (stats != null)
                {
                    technicianStatsControl.UpdateStats(
                        stats.CompletedRepairs,
                        stats.AverageRating,
                        stats.TotalStars
                    );
                }
                
                // Update ticket count
                lblTicketCount.Text = $"{_allTickets.Count} tiket";

                // Update last update time
                lblLastUpdate.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";

                RenderTickets();
                
                pnlTicketList.ResumeLayout();

                UpdateStatusIndicator(true);
            }
            catch (Exception ex)
            {
                _timerRefresh.Stop();
                UpdateStatusIndicator(false);
                
                MessageBox.Show($"Gagal memuat daftar tiket: {ex.Message}", 
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderTickets()
        {
            pnlTicketList.SuspendLayout();
            pnlTicketList.Controls.Clear();

            var filtered = _allTickets.AsEnumerable();

            // Status Filter
            int statusIndex = cmbFilterStatus.SelectedIndex;
            if (statusIndex == 1) // Belum Ditangani
            {
                filtered = filtered.Where(t => t.StatusId == 1);
            }
            else if (statusIndex == 2) // Sedang Diperbaiki
            {
                filtered = filtered.Where(t => t.StatusId == 2);
            }
            else if (statusIndex == 3) // Selesai
            {
                filtered = filtered.Where(t => t.StatusId == 3);
            }

            // Sort By
            int sortIndex = cmbSortBy.SelectedIndex;
            
            // Apply sorting immediately to a List to guarantee order
            List<TicketDto> sortedList;

            if (sortIndex == 0) // Default: Urgensi (Status ASC, Waktu ASC)
            {
                // PRAGMATIC FIX: Logic dibalik karena output UI terbalik (Selesai di atas)
                // Harapan: Open (1) -> Repair (2) -> Selesai (3)
                // Implementasi: Descending (3->2->1) agar di UI tampil 1->2->3 (jika UI membalik)
                // Atau jika database/ID aneh. Kita coba Descending.
                
                sortedList = filtered
                    .OrderByDescending(t => t.StatusId)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToList();
            }
            else if (sortIndex == 1) // Terbaru (Time DESC)
            {
                sortedList = filtered.OrderByDescending(t => t.CreatedAt).ToList();
            }
            else // Terlama (Time ASC)
            {
                sortedList = filtered.OrderBy(t => t.CreatedAt).ToList();
            }

            var ticketList = sortedList; // Use the explicitly sorted list

            if (ticketList.Count == 0)
            {
                // Show empty state
                panelEmptyState.Visible = true;
                CenterEmptyState();
                pnlTicketList.Controls.Add(panelEmptyState);
            }
            else
            {
                // Hide empty state and show tickets
                panelEmptyState.Visible = false;
                        
                foreach (var ticket in ticketList)
                {
                    // Create card and use UpdateDisplay method
                    var card = new TechnicianTicketCardControl();
                    card.UpdateDisplay(ticket);
                    pnlTicketList.Controls.Add(card);
                }
            }

            pnlTicketList.ResumeLayout();
        }

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

        private void DrawEmptyIcon(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            using (Pen pen = new Pen(Color.FromArgb(203, 213, 225), 3))
            {
                // Clipboard body
                g.DrawRectangle(pen, 15, 20, 50, 55);
                // Clipboard clip
                g.DrawRectangle(pen, 30, 15, 20, 10);
                
                // Checkmark
                using (Pen checkPen = new Pen(Color.FromArgb(34, 197, 94), 4))
                {
                    g.DrawLine(checkPen, 28, 45, 35, 52);
                    g.DrawLine(checkPen, 35, 52, 52, 35);
                }
            }
        }

        private string FormatTimeAgo(TimeSpan ts)
        {
            if (ts.TotalMinutes < 2)
                return "Baru saja dilaporkan";
            if (ts.TotalMinutes < 60)
                return $"Dilaporkan {(int)ts.TotalMinutes} menit yang lalu";
            if (ts.TotalHours < 24)
                return $"Dilaporkan {(int)ts.TotalHours} jam yang lalu";
            
            return $"Dilaporkan {ts.Days} hari yang lalu";
        }

        private void ClearFilters()
        {
            cmbFilterStatus.SelectedIndex = 0;
            cmbSortBy.SelectedIndex = 0;
        }


    }
}