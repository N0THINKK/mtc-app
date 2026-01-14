using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.features.rating.presentation.screens;
using mtc_app.features.group_leader.presentation.components;

namespace mtc_app.features.group_leader.presentation.screens
{
    public partial class GroupLeaderDashboardForm : AppBaseForm
    {
        private List<TicketDto> _allTickets = new List<TicketDto>();
        private Timer timerRefresh;

        public GroupLeaderDashboardForm()
        {
            InitializeComponent(); // Ini akan memanggil method di file Designer.cs

            // Setup Timer di sini karena Timer adalah Component (non-visual)
            this.timerRefresh = new Timer(this.components);
            this.timerRefresh.Interval = 15000; // 15 seconds
            this.timerRefresh.Tick += (s, e) => LoadData();

            if (!this.DesignMode)
            {
                LoadData();
                timerRefresh.Start();
            }
        }

        private void LoadData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string sql = @"
                        SELECT 
                            t.ticket_id,
                            m.machine_name,
                            u.full_name AS technician_name,
                            t.gl_rating_score,
                            t.created_at,
                            t.gl_validated_at
                        FROM tickets t
                        LEFT JOIN machines m ON t.machine_id = m.machine_id
                        LEFT JOIN users u ON t.technician_id = u.user_id
                        WHERE t.status_id >= 2
                        ORDER BY t.created_at DESC";

                    _allTickets = connection.Query<TicketDto>(sql).ToList();

                    // Update stats
                    int totalTickets = _allTickets.Count;
                    int reviewedTickets = _allTickets.Count(t => t.gl_validated_at.HasValue || (t.gl_rating_score.HasValue && t.gl_rating_score > 0));
                    int pendingTickets = totalTickets - reviewedTickets;

                    lblTicketStats.Text = $"Total: {totalTickets} | Sudah Direview: {reviewedTickets} | Belum Direview: {pendingTickets}";
                    lblLastUpdate.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";

                    RenderTickets();
                    UpdateStatusIndicator(true);
                }
            }
            catch (Exception ex)
            {
                timerRefresh.Stop();
                UpdateStatusIndicator(false);
                MessageBox.Show($"Gagal memuat data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            RenderTickets();
        }

        private void RenderTickets()
        {
            flowTickets.SuspendLayout();
            flowTickets.Controls.Clear();

            var filtered = _allTickets.AsEnumerable();

            // Status Filter
            int statusIndex = cmbFilterStatus.SelectedIndex;
            if (statusIndex == 1) // Sudah Direview
            {
                filtered = filtered.Where(t => t.gl_validated_at.HasValue || (t.gl_rating_score.HasValue && t.gl_rating_score > 0));
            }
            else if (statusIndex == 2) // Belum Direview
            {
                filtered = filtered.Where(t => !t.gl_validated_at.HasValue && (!t.gl_rating_score.HasValue || t.gl_rating_score == 0));
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
                flowTickets.Controls.Add(panelEmptyState);
            }
            else
            {
                // Hide empty state and show tickets
                panelEmptyState.Visible = false;

                foreach (var ticket in ticketList)
                {
                    bool isReviewed = ticket.gl_validated_at.HasValue || (ticket.gl_rating_score.HasValue && ticket.gl_rating_score > 0);

                    var card = new GroupLeaderTicketCardControl(
                        ticket.ticket_id,
                        ticket.machine_name,
                        ticket.technician_name,
                        ticket.created_at,
                        ticket.gl_rating_score,
                        isReviewed
                    );

                    card.CardClicked += (s, e) => {
                        using (var form = new RatingGlForm(ticket.ticket_id))
                        {
                            form.ShowDialog();
                            LoadData();
                        }
                    };

                    flowTickets.Controls.Add(card);
                }
            }

            flowTickets.ResumeLayout();
        }

        private void UpdateStatusIndicator(bool isActive)
        {
            if (isActive)
            {
                panelStatusBar.BackColor = Color.FromArgb(240, 253, 244);
                lblSystemStatus.Text = "Sistem Aktif";
                lblSystemStatus.ForeColor = Color.FromArgb(21, 128, 61);
                picStatusIndicator.Invalidate();
            }
            else
            {
                panelStatusBar.BackColor = Color.FromArgb(254, 242, 242);
                lblSystemStatus.Text = "Sistem Error";
                lblSystemStatus.ForeColor = Color.FromArgb(185, 28, 28);
                // Clear previous events to avoid stacking
                // Note: In real scenarios, use a dedicated method/variable for paint logic
                picStatusIndicator.Invalidate();
            }
        }

        // Method helper untuk menggambar status error (merah)
        private void DrawErrorStatus(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillEllipse(new SolidBrush(Color.FromArgb(239, 68, 68)), 0, 0, 12, 12);
        }

        private void CenterEmptyState()
        {
            if (panelEmptyState != null && flowTickets != null)
            {
                panelEmptyState.Left = (flowTickets.ClientSize.Width - panelEmptyState.Width) / 2;
                panelEmptyState.Top = (flowTickets.ClientSize.Height - panelEmptyState.Height) / 2;
            }
        }

        private void DrawEmptyIcon(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen pen = new Pen(Color.FromArgb(203, 213, 225), 3))
            {
                // Folder
                g.DrawRectangle(pen, 15, 30, 50, 40);
                g.DrawLine(pen, 15, 30, 25, 20);
                g.DrawLine(pen, 25, 20, 40, 20);
                g.DrawLine(pen, 40, 20, 45, 30);

                // Star for rating
                using (Pen starPen = new Pen(Color.FromArgb(234, 179, 8), 2))
                {
                    PointF[] starPoints = {
                        new PointF(40, 42), new PointF(43, 48), new PointF(50, 48),
                        new PointF(44, 53), new PointF(47, 60), new PointF(40, 55),
                        new PointF(33, 60), new PointF(36, 53), new PointF(30, 48),
                        new PointF(37, 48)
                    };
                    g.DrawPolygon(starPen, starPoints);
                }
            }
        }

        private class TicketDto
        {
            public long ticket_id { get; set; }
            public string machine_name { get; set; }
            public string technician_name { get; set; }
            public int? gl_rating_score { get; set; }
            public DateTime created_at { get; set; }
            public DateTime? gl_validated_at { get; set; }
        }
    }
}