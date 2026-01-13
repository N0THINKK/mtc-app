using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app;
using mtc_app.shared.presentation.components;

namespace mtc_app.features.admin.presentation.screens
{
    public partial class AdminDashboardForm : AppBaseForm
    {
        public AdminDashboardForm()
        {
            InitializeComponent();
            LoadData();
            timerRefresh.Start();
        }

        private void LoadData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    // Comprehensive Query for Admin
                    string sql = @"
                        SELECT 
                            t.ticket_display_code AS 'Kode Tiket',
                            ts.status_name AS 'Status',
                            m.machine_name AS 'Mesin',
                            u_op.full_name AS 'Operator',
                            u_tech.full_name AS 'Teknisi',
                            t.failure_details AS 'Keluhan',
                            t.created_at AS 'Waktu Lapor',
                            t.started_at AS 'Mulai Perbaikan',
                            t.technician_finished_at AS 'Selesai',
                            TIMEDIFF(t.technician_finished_at, t.created_at) AS 'Total Downtime'
                        FROM tickets t
                        LEFT JOIN machines m ON t.machine_id = m.machine_id
                        LEFT JOIN users u_op ON t.operator_id = u_op.user_id
                        LEFT JOIN users u_tech ON t.technician_id = u_tech.user_id
                        LEFT JOIN ticket_statuses ts ON t.status_id = ts.status_id
                        ORDER BY t.created_at DESC
                        LIMIT 50"; // Limit for performance

                    var data = connection.Query(sql).ToList();
                    
                    // Save current selection to restore after refresh
                    int selectedRowIndex = -1;
                    if (gridTickets.SelectedRows.Count > 0)
                        selectedRowIndex = gridTickets.SelectedRows[0].Index;

                    gridTickets.DataSource = data;

                    // Restore selection
                    if (selectedRowIndex >= 0 && selectedRowIndex < gridTickets.Rows.Count)
                        gridTickets.Rows[selectedRowIndex].Selected = true;
                }
            }
            catch (Exception ex)
            {
                // On error stop timer to avoid spamming errors
                timerRefresh.Stop();
                MessageBox.Show($"Gagal memuat data: {ex.Message}", "Error Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TimerRefresh_Tick(object sender, EventArgs e)
        {
            LoadData();
        }
    }
}