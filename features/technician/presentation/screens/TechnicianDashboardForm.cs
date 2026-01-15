using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.technician.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.screens
{
    public partial class TechnicianDashboardForm : AppBaseForm
    {
        private Timer timerRefresh;
        private bool _isSystemActive = true;
        private List<TicketDto> _allTickets = new List<TicketDto>();

        public TechnicianDashboardForm()
        {
            InitializeComponent();
            SetupEventHandlers();
            
            // Setup Timer
            this.timerRefresh = new Timer(this.components);
            this.timerRefresh.Interval = 10000; // 10 seconds
            this.timerRefresh.Tick += (s, e) => LoadData();

            if (!this.DesignMode)
            {
                cmbFilterStatus.SelectedIndex = 0;
                cmbSortTime.SelectedIndex = 0;
                LoadData();
                timerRefresh.Start();
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
            this.cmbSortTime.SelectedIndexChanged += (s, e) => RenderTickets();
        }

        private void LoadData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = @"
                        SELECT 
                            t.ticket_id,
                            m.machine_name,
                            CONCAT(
                                IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                                IFNULL(f.failure_name, IFNULL(t.failure_remarks, 'Unknown')),
                                IF(t.applicator_code IS NOT NULL, CONCAT(' (App: ', t.applicator_code, ')'), '')
                            ) AS failure_details,
                            t.created_at,
                            t.status_id,
                            t.gl_rating_score,
                            t.gl_validated_at
                        FROM tickets t
                        JOIN machines m ON t.machine_id = m.machine_id
                        LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
                        LEFT JOIN failures f ON t.failure_id = f.failure_id
                        WHERE t.status_id >= 1";
                    
                    _allTickets = connection.Query<TicketDto>(sql).ToList();
                    
                    pnlTicketList.SuspendLayout();
                    
                    // Update ticket count
                    lblTicketCount.Text = $"{_allTickets.Count} tiket";

                    // Update last update time
                    lblLastUpdate.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";

                    RenderTickets();
                    
                    pnlTicketList.ResumeLayout();

                    UpdateStatusIndicator(true);
                }
            }
            catch (Exception ex)
            {
                timerRefresh.Stop();
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
                filtered = filtered.Where(t => t.status_id == 1);
            }
            else if (statusIndex == 2) // Sudah Direview GL
            {
                filtered = filtered.Where(t => t.gl_validated_at.HasValue || (t.gl_rating_score.HasValue && t.gl_rating_score > 0));
            }

            // Sort Time
            int sortIndex = cmbSortTime.SelectedIndex;
            if (sortIndex == 0) // Terbaru
            {
                filtered = filtered.OrderByDescending(t => t.created_at);
            }
            else // Terlama
            {
                filtered = filtered.OrderBy(t => t.created_at);
            }

            var ticketList = filtered.ToList();

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
                    TimeSpan timeSinceCreation = DateTime.Now - ticket.created_at;
                    string timeAgo = FormatTimeAgo(timeSinceCreation);

                    var card = new TechnicianTicketCardControl(
                        ticket.machine_name,
                        ticket.failure_details,
                        timeAgo
                    );
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

        private class TicketDto
        {
            public long ticket_id { get; set; }
            public string machine_name { get; set; }
            public string failure_details { get; set; }
            public DateTime created_at { get; set; }
            public int status_id { get; set; }
            public int? gl_rating_score { get; set; }
            public DateTime? gl_validated_at { get; set; }
        }
    }
}