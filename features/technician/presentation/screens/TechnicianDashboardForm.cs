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
            
            this.SuspendLayout();

            // Form
            this.Text = "Dashboard Teknisi - Daftar Tunggu Perbaikan";
            this.BackColor = AppColors.Surface;
            this.ClientSize = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // FlowLayoutPanel for Ticket Cards
            this.pnlTicketList.Dock = DockStyle.Fill;
            this.pnlTicketList.AutoScroll = true;
            this.pnlTicketList.FlowDirection = FlowDirection.TopDown;
            this.pnlTicketList.WrapContents = false; // Important for vertical list
            this.pnlTicketList.Padding = new Padding(20);

            // Refresh Timer
            this.timerRefresh.Interval = 10000; // 10 seconds
            this.timerRefresh.Tick += (s, e) => LoadPendingTickets();

            // Add controls
            this.Controls.Add(this.pnlTicketList);

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
                            t.failure_details,
                            t.created_at
                        FROM tickets t
                        JOIN machines m ON t.machine_id = m.machine_id
                        WHERE t.status_id = 1 -- WAITING
                        ORDER BY t.created_at ASC";
                    
                    var tickets = connection.Query(sql).ToList();
                    
                    pnlTicketList.SuspendLayout(); // Suspend layout to prevent flickering
                    pnlTicketList.Controls.Clear();

                    foreach (var ticket in tickets)
                    {
                        TimeSpan timeSinceCreation = DateTime.Now - ticket.created_at;
                        string timeAgo = FormatTimeAgo(timeSinceCreation);

                        var card = new TicketCardControl(
                            ticket.machine_name,
                            ticket.failure_details,
                            timeAgo
                        );
                        pnlTicketList.Controls.Add(card);
                    }
                    
                    pnlTicketList.ResumeLayout();
                }
            }
            catch (Exception ex)
            {
                timerRefresh.Stop();
                MessageBox.Show($"Gagal memuat daftar tiket: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatTimeAgo(TimeSpan ts)
        {
            if (ts.TotalMinutes < 60)
                return $"Dilaporkan {(int)ts.TotalMinutes} menit yang lalu";
            if (ts.TotalHours < 24)
                return $"Dilaporkan {(int)ts.TotalHours} jam yang lalu";
            
            return $"Dilaporkan {ts.Days} hari yang lalu";
        }
    }
}
