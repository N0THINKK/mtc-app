using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app;
using mtc_app.shared.presentation.components;
using mtc_app.features.stock.presentation.components;

namespace mtc_app.features.stock.presentation.screens
{
    public partial class StockDashboardForm : AppBaseForm
    {
        private int pendingCount = 0;
        private int readyCount = 0;
        // private int todayCompletedCount = 0;

        public StockDashboardForm()
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
                    
                    // Query untuk mengambil request yang PENDING (status_id = 1)
                    string sql = @"
                        SELECT 
                            pr.request_id,
                            pr.requested_at AS 'Waktu Request',
                            IFNULL(t.ticket_display_code, 'N/A') AS 'Kode Tiket',
                            pr.part_name_manual AS 'Nama Barang',
                            pr.qty AS 'Jumlah',
                            IFNULL(ts.status_name, 'Unknown') AS 'Status'
                        FROM part_requests pr
                        LEFT JOIN tickets t ON pr.ticket_id = t.ticket_id
                        LEFT JOIN request_statuses ts ON pr.status_id = ts.status_id
                        WHERE pr.status_id = 1
                        ORDER BY pr.requested_at ASC";

                    var data = connection.Query(sql).ToList();
                    pendingCount = data.Count;

                    // Get ready count
                    string readySql = "SELECT COUNT(*) FROM part_requests WHERE status_id = 2";
                    readyCount = connection.QuerySingle<int>(readySql);

                    // Get today's completed count
                    // string completedSql = @"
                    //     SELECT COUNT(*) 
                    //     FROM part_requests 
                    //     WHERE status_id = 3 
                    //     AND DATE(picked_at) = CURDATE()";
                    // todayCompletedCount = connection.QuerySingle<int>(completedSql);

                    // Update status cards
                    UpdateStatusCards();

                    // Update grid visibility
                    if (data.Count > 0)
                    {
                        gridRequests.Visible = true;
                        emptyStatePanel.Visible = false;
                        gridRequests.AutoGenerateColumns = true;
                        gridRequests.DataSource = data;
                        
                        // Style the grid
                        StyleDataGrid();
                    }
                    else
                    {
                        gridRequests.Visible = false;
                        emptyStatePanel.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stock data: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StyleDataGrid()
        {
            // Header style
            gridRequests.EnableHeadersVisualStyles = false;
            gridRequests.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 58, 64);
            gridRequests.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridRequests.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            gridRequests.ColumnHeadersDefaultCellStyle.Padding = new Padding(5);
            gridRequests.ColumnHeadersHeight = 40;

            // Row style
            gridRequests.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            gridRequests.DefaultCellStyle.SelectionBackColor = mtc_app.shared.presentation.styles.AppColors.Primary;
            gridRequests.DefaultCellStyle.SelectionForeColor = Color.White;
            gridRequests.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            gridRequests.RowTemplate.Height = 35;

            // Hide request_id column
            if (gridRequests.Columns.Count > 0)
            {
                gridRequests.Columns[0].Visible = false;
            }
        }

        private void UpdateStatusCards()
        {
            cardPending.Value = pendingCount.ToString();
            cardPending.Subtext = pendingCount == 0 ? "All clear!" : "Needs processing";

            cardReady.Value = readyCount.ToString();
            cardReady.Subtext = readyCount == 0 ? "None waiting" : "Awaiting pickup";

            // cardCompleted.Value = todayCompletedCount.ToString();
            // cardCompleted.Subtext = "Completed today";

            // Update last refresh time
            lblLastUpdate.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            LoadData();
        }

        private void btnReady_Click(object sender, EventArgs e)
        {
            if (gridRequests.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih request dulu!", "Info", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (gridRequests.CurrentRow == null) return;
            
            var requestId = gridRequests.CurrentRow.Cells[0].Value;

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string sql = "UPDATE part_requests SET status_id = 2, ready_at = NOW() WHERE request_id = @Id";
                    connection.Execute(sql, new { Id = requestId });
                }
                
                MessageBox.Show("Barang ditandai SIAP!", "Sukses", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal update: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}