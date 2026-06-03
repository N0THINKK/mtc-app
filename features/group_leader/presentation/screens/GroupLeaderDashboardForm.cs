using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.group_leader.data.dtos;
using mtc_app.features.group_leader.data.repositories;
using mtc_app.features.group_leader.presentation.components;
using mtc_app.features.group_leader.presentation.controllers;
using mtc_app.features.rating.presentation.screens;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.group_leader.presentation.screens
{
    public partial class GroupLeaderDashboardForm : AppBaseForm, IGroupLeaderDashboardView
    {
        private readonly GroupLeaderDashboardController _controller;
        private bool _isSystemActive = true;
        private bool _isLoading = false;

        public GroupLeaderDashboardForm() : this(ServiceLocator.CreateGroupLeaderRepository())
        {
        }

        public GroupLeaderDashboardForm(IGroupLeaderRepository repository)
        {
            _controller = new GroupLeaderDashboardController(this, repository);
            InitializeComponent();
            SetupEventHandlers();

            if (!this.DesignMode)
            {
                this.Shown += async (s, e) => await LoadDataAsync();
            }
        }

        // ==========================================
        // IGroupLeaderDashboardView Implementation
        // ==========================================
        
        public int SelectedStatusIndex => cmbFilterStatus.SelectedIndex;
        public int SelectedSortIndex => cmbSortTime.SelectedIndex;
        public int SelectedAreaIndex => cmbFilterArea.SelectedIndex;
        public string SelectedAreaName => cmbFilterArea.SelectedItem?.ToString();
        public int SelectedMonthIndex => cmbFilterMonth.SelectedIndex;

        public void UpdateStats(int totalTickets, int reviewedTickets, int pendingTickets)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStats(totalTickets, reviewedTickets, pendingTickets)));
                return;
            }
            lblTicketStats.Text = $"Total: {totalTickets} | Sudah Direview: {reviewedTickets} | Belum Direview: {pendingTickets}";
            lblLastUpdate.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";
        }

        public void PopulateAreaFilter(List<string> areas)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => PopulateAreaFilter(areas)));
                return;
            }

            string currentSelection = cmbFilterArea.SelectedItem?.ToString();
            cmbFilterArea.Items.Clear();
            cmbFilterArea.Items.Add("Semua");

            foreach (var area in areas)
                cmbFilterArea.Items.Add(area);

            int idx = currentSelection != null ? cmbFilterArea.Items.IndexOf(currentSelection) : -1;
            cmbFilterArea.SelectedIndex = idx >= 0 ? idx : 0;
        }

        public void UpdateGrid(List<GroupLeaderTicketDto> tickets)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateGrid(tickets)));
                return;
            }

            flowTickets.SuspendLayout();
            
            foreach (Control ctrl in flowTickets.Controls)
            {
                if (ctrl != panelEmptyState) 
                {
                    ctrl.Dispose();
                }
            }
            flowTickets.Controls.Clear();

            if (tickets.Count == 0)
            {
                panelEmptyState.Visible = true;
                CenterEmptyState();
                flowTickets.Controls.Add(panelEmptyState);
            }
            else
            {
                panelEmptyState.Visible = false;

                foreach (var ticket in tickets)
                {
                    var card = new GroupLeaderTicketCardControl(ticket);
                    card.OnValidate += Card_OnValidate;
                    flowTickets.Controls.Add(card);
                }
            }

            flowTickets.ResumeLayout();
        }

        public void UpdateSystemStatus(bool isActive)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateSystemStatus(isActive)));
                return;
            }

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

        public void ShowError(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowError(message)));
                return;
            }
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ==========================================
        // UI Events
        // ==========================================

        private async Task LoadDataAsync()
        {
            if (this.IsDisposed || _isLoading) return;
            _isLoading = true;
            try
            {
                await _controller.LoadDataAsync();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            if (_isLoading) return;
            _controller.ApplyFiltersAndRender();
        }

        private void Card_OnValidate(object sender, Guid ticketId)
        {
            using (var form = new RatingGlForm(ticketId)) 
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _ = LoadDataAsync();
                }
            }
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private void SetupEventHandlers()
        {
            this.panelHeader.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(230, 230, 230)))
                {
                    e.Graphics.DrawLine(pen, 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
                }
            };

            this.panelFilters.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(230, 230, 230)))
                {
                    e.Graphics.DrawLine(pen, 0, panelFilters.Height - 1, panelFilters.Width, panelFilters.Height - 1);
                }
            };

            this.picStatusIndicator.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color color = _isSystemActive ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
                using (var brush = new SolidBrush(color))
                {
                    e.Graphics.FillEllipse(brush, 0, 0, 12, 12);
                }
            };

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
                g.DrawRectangle(pen, 15, 30, 50, 40);
                g.DrawLine(pen, 15, 30, 25, 20);
                g.DrawLine(pen, 25, 20, 40, 20);
                g.DrawLine(pen, 40, 20, 45, 30);

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
