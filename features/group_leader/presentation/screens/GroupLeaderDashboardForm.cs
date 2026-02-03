using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.group_leader.data.dtos;
using mtc_app.features.group_leader.data.repositories;
using mtc_app.features.group_leader.presentation.components;
using mtc_app.features.rating.presentation.screens;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.group_leader.presentation.screens
{
    public partial class GroupLeaderDashboardForm : AppBaseForm
    {
        private readonly IGroupLeaderRepository _repository;
        private List<GroupLeaderTicketDto> _allTickets = new List<GroupLeaderTicketDto>();
        private bool _isSystemActive = true;
        private Timer timerRefresh;

        // Composition Root: Default constructor uses ServiceLocator for offline support
        public GroupLeaderDashboardForm() : this(ServiceLocator.CreateGroupLeaderRepository())
        {
        }

        public GroupLeaderDashboardForm(IGroupLeaderRepository repository)
        {
            _repository = repository;
            InitializeComponent();
            SetupEventHandlers();

            // Setup Timer
            this.timerRefresh = new Timer(this.components);
            this.timerRefresh.Interval = 15000; // 15 seconds
            this.timerRefresh.Tick += async (s, e) => await LoadDataAsync();

            if (!this.DesignMode)
            {
                // Trigger initial load
                this.Shown += async (s, e) => await LoadDataAsync();
                timerRefresh.Start();
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // We fetch all relevant tickets. Filters are applied in memory for smooth UX (unless dataset is huge)
                // Repo method 'GetTicketsAsync' loads tickets with status >= 2 (Repairing or Done)
                var tickets = await _repository.GetTicketsAsync();
                _allTickets = tickets.ToList();

                UpdateStats();
                RenderTickets();
                UpdateStatusIndicator(true);
            }
            catch (Exception ex)
            {
                timerRefresh.Stop(); // Stop refreshing if error persists
                UpdateStatusIndicator(false);
                MessageBox.Show($"Gagal memuat data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStats()
        {
            int totalTickets = _allTickets.Count;
            int reviewedTickets = _allTickets.Count(t => t.GlValidatedAt.HasValue || (t.GlRatingScore.HasValue && t.GlRatingScore > 0));
            int pendingTickets = totalTickets - reviewedTickets;

            lblTicketStats.Text = $"Total: {totalTickets} | Sudah Direview: {reviewedTickets} | Belum Direview: {pendingTickets}";
            lblLastUpdate.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            RenderTickets();
        }

        private void RenderTickets()
        {
            flowTickets.SuspendLayout();
            
            // [OPTIMIZATION] Dispose old controls to prevent memory leaks
            foreach (Control ctrl in flowTickets.Controls)
            {
                ctrl.Dispose();
            }
            flowTickets.Controls.Clear();

            var filtered = _allTickets.AsEnumerable();

            // Status Filter
            int statusIndex = cmbFilterStatus.SelectedIndex;
            if (statusIndex == 1) // Sudah Direview
            {
                filtered = filtered.Where(t => t.GlValidatedAt.HasValue || (t.GlRatingScore.HasValue && t.GlRatingScore > 0));
            }
            else if (statusIndex == 2) // Belum Direview
            {
                filtered = filtered.Where(t => !t.GlValidatedAt.HasValue && (!t.GlRatingScore.HasValue || t.GlRatingScore == 0));
            }

            // Sort Time
            int sortIndex = cmbSortTime.SelectedIndex;
            if (sortIndex == 0) // Terbaru
            {
                filtered = filtered.OrderBy(t => t.CreatedAt);
            }
            else // Terlama
            {
                filtered = filtered.OrderByDescending(t => t.CreatedAt);
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
                panelEmptyState.Visible = false;

                foreach (var ticket in ticketList)
                {
                    // bool isReviewed = ... (Already calculated in ticket properties mostly, 
                    // but let's just pass the ticket, logic is inside Card now)
                    
                    var card = new GroupLeaderTicketCardControl(ticket);

                    // Subscribe to Event (Dumb Component Pattern)
                    card.OnValidate += Card_OnValidate;

                    flowTickets.Controls.Add(card);
                }
            }

            flowTickets.ResumeLayout();
        }

        private void Card_OnValidate(object sender, Guid ticketId)
        {
            // Open Rating Form
            using (var form = new RatingGlForm(ticketId)) 
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Refresh Data after rating
                    _ = LoadDataAsync();
                }
            }
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            timerRefresh?.Stop();
            timerRefresh?.Dispose();
            base.OnFormClosing(e);
        }

        private void SetupEventHandlers()
        {
            // Panel Header Border
            this.panelHeader.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(230, 230, 230)))
                {
                    e.Graphics.DrawLine(pen, 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
                }
            };

            // Panel Filters Border
            this.panelFilters.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(230, 230, 230)))
                {
                    e.Graphics.DrawLine(pen, 0, panelFilters.Height - 1, panelFilters.Width, panelFilters.Height - 1);
                }
            };

            // Status Indicator Paint
            this.picStatusIndicator.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color color = _isSystemActive ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
                using (var brush = new SolidBrush(color))
                {
                    e.Graphics.FillEllipse(brush, 0, 0, 12, 12);
                }
            };

            // Empty State Icon (Ideally use AppEmptyState here)
            // But leaving custom paint for now to avoid altering Designer file drastically unless needed
            this.picEmptyIcon.Paint += (s, e) => DrawEmptyIcon(e.Graphics);
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
    }
}
