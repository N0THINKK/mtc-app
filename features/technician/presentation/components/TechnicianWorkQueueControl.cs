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

        // UI Controls
        private Panel panelHeader;
        private Panel panelStatusBar;
        private Panel panelFilters;
        private Panel pnlTicketList;
        private Panel panelEmptyState;
        
        private Label lblTicketCount;
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
            _timerRefresh.Interval = 10000;
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



        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(248, 250, 252);

            // Header Panel
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 20)
            };

            lblTicketCount = new Label
            {
                Text = "0 tiket",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = AppColors.Primary,
                Location = new Point(30, 35),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblTicketCount);



            // Status Bar
            panelStatusBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(30, 0, 30, 0)
            };

            picStatusIndicator = new PictureBox
            {
                Size = new Size(12, 12),
                Location = new Point(30, 14),
                BackColor = Color.Transparent
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
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 128, 61),
                Location = new Point(50, 12),
                AutoSize = true
            };

            lblLastUpdate = new Label
            {
                Text = "Terakhir diperbarui: -",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(150, 12),
                AutoSize = true
            };

            panelStatusBar.Controls.Add(picStatusIndicator);
            panelStatusBar.Controls.Add(lblSystemStatus);
            panelStatusBar.Controls.Add(lblLastUpdate);

            // Filter Panel
            panelFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White
            };

            var lblFilterStatus = new Label { Text = "Filter:", Location = new Point(30, 16), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            cmbFilterStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(80, 12),
                Size = new Size(140, 23),
                Font = new Font("Segoe UI", 9F)
            };
            cmbFilterStatus.Items.AddRange(new object[] { "Semua", "Belum Ditangani", "Sedang Diperbaiki", "Selesai" });
            cmbFilterStatus.SelectedIndex = 0;

            var lblSortBy = new Label { Text = "Urutkan:", Location = new Point(240, 16), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            cmbSortBy = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(305, 12),
                Size = new Size(180, 23),
                Font = new Font("Segoe UI", 9F)
            };
            cmbSortBy.Items.AddRange(new object[] { "Default (Urgensi)", "Terbaru (Waktu)", "Terlama (Waktu)" });
            cmbSortBy.SelectedIndex = 0;

            btnClearFilters = new Button
            {
                Text = "Reset",
                Location = new Point(500, 11),
                Size = new Size(70, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = AppColors.TextSecondary
            };
            btnClearFilters.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);

            panelFilters.Controls.AddRange(new Control[] { lblFilterStatus, cmbFilterStatus, lblSortBy, cmbSortBy, btnClearFilters });

            // Ticket List Panel
            pnlTicketList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // Empty State
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
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(50, 90),
                Size = new Size(200, 25)
            };

            lblEmptyMessage = new Label
            {
                Text = "Semua tiket telah diproses.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppColors.TextSecondary,
                TextAlign = ContentAlignment.TopCenter,
                Location = new Point(50, 120),
                Size = new Size(200, 40)
            };

            panelEmptyState.Controls.AddRange(new Control[] { picEmptyIcon, lblEmptyTitle, lblEmptyMessage });
            pnlTicketList.Controls.Add(panelEmptyState);

            // Add all panels (order matters for Dock)
            this.Controls.Add(pnlTicketList);
            this.Controls.Add(panelFilters);
            this.Controls.Add(panelStatusBar);
            this.Controls.Add(panelHeader);
        }

        private void SetupEventHandlers()
        {
            cmbFilterStatus.SelectedIndexChanged += (s, e) => RenderTickets();
            cmbSortBy.SelectedIndexChanged += (s, e) => RenderTickets();
            btnClearFilters.Click += (s, e) => ClearFilters();
            
            panelHeader.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230)), 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };
            
            panelFilters.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230)), 0, panelFilters.Height - 1, panelFilters.Width, panelFilters.Height - 1);
            };
        }

        public void LoadData()
        {
            try
            {
                _allTickets = _repository.GetActiveTickets().ToList();
                
                pnlTicketList.SuspendLayout();
                lblTicketCount.Text = $"{_allTickets.Count} tiket";
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
        }

        private void RenderTickets()
        {
            pnlTicketList.SuspendLayout();
            pnlTicketList.Controls.Clear();

            var filtered = _allTickets.AsEnumerable();

            int statusIndex = cmbFilterStatus.SelectedIndex;
            if (statusIndex == 1) filtered = filtered.Where(t => t.StatusId == 1);
            else if (statusIndex == 2) filtered = filtered.Where(t => t.StatusId == 2);
            else if (statusIndex == 3) filtered = filtered.Where(t => t.StatusId == 3);

            int sortIndex = cmbSortBy.SelectedIndex;
            List<TicketDto> sortedList;

            if (sortIndex == 0)
                sortedList = filtered.OrderByDescending(t => t.StatusId).ThenByDescending(t => t.CreatedAt).ToList();
            else if (sortIndex == 1)
                sortedList = filtered.OrderBy(t => t.CreatedAt).ToList();
            else
                sortedList = filtered.OrderByDescending(t => t.CreatedAt).ToList();

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
