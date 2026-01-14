using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.technician.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.screens
{
    public class TechnicianDashboardForm : AppBaseForm
    {
        private System.ComponentModel.IContainer components = null;
        private FlowLayoutPanel pnlTicketList;
        private Timer timerRefresh;
        private Panel panelHeader;
        private Panel panelStatusBar;
        private Panel panelEmptyState;
        private AppLabel labelTitle;
        private Label lblTicketCount;
        private Label lblLastUpdate;
        private Label lblSystemStatus;
        private PictureBox picStatusIndicator;
        private Label lblEmptyTitle;
        private Label lblEmptyMessage;
        private PictureBox picEmptyIcon;

        public TechnicianDashboardForm()
        {
            InitializeComponent();
            if (!this.DesignMode)
            {
                LoadPendingTickets();
                timerRefresh.Start();
            }
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlTicketList = new FlowLayoutPanel();
            this.timerRefresh = new Timer(this.components);
            this.panelHeader = new Panel();
            this.panelStatusBar = new Panel();
            this.panelEmptyState = new Panel();
            this.labelTitle = new AppLabel();
            this.lblTicketCount = new Label();
            this.lblLastUpdate = new Label();
            this.lblSystemStatus = new Label();
            this.picStatusIndicator = new PictureBox();
            this.lblEmptyTitle = new Label();
            this.lblEmptyMessage = new Label();
            this.picEmptyIcon = new PictureBox();
            
            this.SuspendLayout();

            // Form
            this.Text = "Dashboard Teknisi - Daftar Tunggu Perbaikan";
            this.ClientSize = new Size(1200, 700);
            this.BackColor = Color.FromArgb(248, 250, 252);

            // Header Panel
            this.panelHeader.BackColor = Color.White;
            this.panelHeader.Dock = DockStyle.Top;
            this.panelHeader.Height = 120;
            this.panelHeader.Padding = new Padding(30, 20, 30, 20);
            this.panelHeader.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230)), 
                    0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };
            
            // Title
            this.labelTitle.Text = "Daftar Tunggu Perbaikan";
            this.labelTitle.Type = AppLabel.LabelType.Header2;
            this.labelTitle.ForeColor = AppColors.TextPrimary;
            this.labelTitle.Location = new Point(30, 25);
            this.labelTitle.AutoSize = true;

            // Ticket Count Label
            this.lblTicketCount.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            this.lblTicketCount.ForeColor = AppColors.Primary;
            this.lblTicketCount.Location = new Point(30, 65);
            this.lblTicketCount.AutoSize = true;
            this.lblTicketCount.Text = "0 tiket menunggu";

            this.panelHeader.Controls.Add(this.lblTicketCount);
            this.panelHeader.Controls.Add(this.labelTitle);

            // Status Bar Panel
            this.panelStatusBar.BackColor = Color.FromArgb(240, 253, 244);
            this.panelStatusBar.Dock = DockStyle.Top;
            this.panelStatusBar.Height = 50;
            this.panelStatusBar.Padding = new Padding(30, 0, 30, 0);

            // Status Indicator (Green Dot)
            this.picStatusIndicator.Size = new Size(12, 12);
            this.picStatusIndicator.Location = new Point(30, 19);
            this.picStatusIndicator.BackColor = Color.Transparent;
            this.picStatusIndicator.Paint += (s, e) => {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(34, 197, 94)), 0, 0, 12, 12);
            };

            // System Status Label
            this.lblSystemStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblSystemStatus.ForeColor = Color.FromArgb(21, 128, 61);
            this.lblSystemStatus.Location = new Point(50, 16);
            this.lblSystemStatus.AutoSize = true;
            this.lblSystemStatus.Text = "Sistem Aktif";

            // Last Update Label
            this.lblLastUpdate.Font = new Font("Segoe UI", 9F);
            this.lblLastUpdate.ForeColor = Color.FromArgb(100, 116, 139);
            this.lblLastUpdate.Location = new Point(150, 17);
            this.lblLastUpdate.AutoSize = true;
            this.lblLastUpdate.Text = "Terakhir diperbarui: -";

            this.panelStatusBar.Controls.Add(this.lblLastUpdate);
            this.panelStatusBar.Controls.Add(this.lblSystemStatus);
            this.panelStatusBar.Controls.Add(this.picStatusIndicator);

            // Empty State Panel
            this.panelEmptyState.BackColor = Color.Transparent;
            this.panelEmptyState.Size = new Size(400, 300);
            this.panelEmptyState.Visible = false;
            this.panelEmptyState.Anchor = AnchorStyles.None;

            // Empty State Icon
            this.picEmptyIcon.Size = new Size(80, 80);
            this.picEmptyIcon.Location = new Point(160, 40);
            this.picEmptyIcon.BackColor = Color.Transparent;
            this.picEmptyIcon.Paint += (s, e) => DrawEmptyIcon(e.Graphics);

            // Empty State Title
            this.lblEmptyTitle.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            this.lblEmptyTitle.ForeColor = AppColors.TextPrimary;
            this.lblEmptyTitle.Text = "Tidak Ada Tiket Menunggu";
            this.lblEmptyTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblEmptyTitle.Location = new Point(50, 140);
            this.lblEmptyTitle.Size = new Size(300, 30);

            // Empty State Message
            this.lblEmptyMessage.Font = new Font("Segoe UI", 10F);
            this.lblEmptyMessage.ForeColor = AppColors.TextSecondary;
            this.lblEmptyMessage.Text = "Semua tiket telah diproses atau belum ada\nlaporan masalah baru dari operator.";
            this.lblEmptyMessage.TextAlign = ContentAlignment.TopCenter;
            this.lblEmptyMessage.Location = new Point(50, 180);
            this.lblEmptyMessage.Size = new Size(300, 60);

            this.panelEmptyState.Controls.Add(this.lblEmptyMessage);
            this.panelEmptyState.Controls.Add(this.lblEmptyTitle);
            this.panelEmptyState.Controls.Add(this.picEmptyIcon);

            // FlowLayoutPanel for Ticket Cards
            this.pnlTicketList.Dock = DockStyle.Fill;
            this.pnlTicketList.AutoScroll = true;
            this.pnlTicketList.FlowDirection = FlowDirection.LeftToRight;
            this.pnlTicketList.WrapContents = true;
            this.pnlTicketList.Padding = new Padding(20);
            this.pnlTicketList.BackColor = Color.FromArgb(248, 250, 252);
            this.pnlTicketList.Controls.Add(this.panelEmptyState);

            // Refresh Timer
            this.timerRefresh.Interval = 10000; // 10 seconds
            this.timerRefresh.Tick += (s, e) => LoadPendingTickets();

            // Add controls in Z-order
            this.Controls.Add(this.pnlTicketList);
            this.Controls.Add(this.panelStatusBar);
            this.Controls.Add(this.panelHeader);

            this.ResumeLayout(false);
        }

        private void LoadPendingTickets()
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
                            t.created_at
                        FROM tickets t
                        JOIN machines m ON t.machine_id = m.machine_id
                        LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
                        LEFT JOIN failures f ON t.failure_id = f.failure_id
                        WHERE t.status_id = 1 -- WAITING
                        ORDER BY t.created_at ASC";
                    
                    var tickets = connection.Query(sql).ToList();
                    
                    pnlTicketList.SuspendLayout();
                    pnlTicketList.Controls.Clear();

                    // Update ticket count
                    lblTicketCount.Text = $"{tickets.Count} tiket menunggu";

                    // Update last update time
                    lblLastUpdate.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";

                    if (tickets.Count == 0)
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
                        
                        foreach (var ticket in tickets)
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

                    // Update status indicator to green (active)
                    UpdateStatusIndicator(true);
                }
            }
            catch (Exception ex)
            {
                timerRefresh.Stop();
                UpdateStatusIndicator(false);
                lblSystemStatus.Text = "Sistem Error";
                lblSystemStatus.ForeColor = AppColors.Danger;
                panelStatusBar.BackColor = Color.FromArgb(254, 242, 242);
                
                MessageBox.Show($"Gagal memuat daftar tiket: {ex.Message}", 
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatusIndicator(bool isActive)
        {
            if (isActive)
            {
                panelStatusBar.BackColor = Color.FromArgb(240, 253, 244);
                lblSystemStatus.Text = "Sistem Aktif";
                lblSystemStatus.ForeColor = Color.FromArgb(21, 128, 61);
                picStatusIndicator.Invalidate(); // Will paint green
            }
            else
            {
                panelStatusBar.BackColor = Color.FromArgb(254, 242, 242);
                lblSystemStatus.Text = "Sistem Error";
                lblSystemStatus.ForeColor = Color.FromArgb(185, 28, 28);
                // Repaint status indicator as red
                picStatusIndicator.Paint -= (s, e) => { }; // Clear existing
                picStatusIndicator.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(239, 68, 68)), 0, 0, 12, 12);
                };
                picStatusIndicator.Invalidate();
            }
        }

        private void CenterEmptyState()
        {
            panelEmptyState.Left = (pnlTicketList.ClientSize.Width - panelEmptyState.Width) / 2;
            panelEmptyState.Top = (pnlTicketList.ClientSize.Height - panelEmptyState.Height) / 2;
        }

        private void DrawEmptyIcon(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Draw clipboard with checkmark
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

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}