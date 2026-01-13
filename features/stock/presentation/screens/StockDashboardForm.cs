using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app;

namespace mtc_app.features.stock.presentation.screens
{
    public partial class StockDashboardForm : Form
    {
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
                    // Join dengan Tickets untuk tahu Tiket/Mesin mana
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

                    var data = connection.Query(sql);
                    gridRequests.DataSource = data;
                }
            }
            catch (Exception ex)
            {
                // Show error for debugging
                MessageBox.Show($"Error loading stock data: {ex.Message}");
            }
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
                MessageBox.Show("Pilih request dulu!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ambil ID dari row yang dipilih
            // Note: Karena Dapper return dynamic/object, kita harus ambil value cell
            // DataGridView biasanya auto-generate column, col index 0 biasanya request_id
            
            // Safe way to get ID from current row
            if (gridRequests.CurrentRow == null) return;
            
            // Assuming first column is request_id based on query order
            var requestId = gridRequests.CurrentRow.Cells[0].Value; 

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    // Update jadi READY (2) dan set ready_at
                    string sql = "UPDATE part_requests SET status_id = 2, ready_at = NOW() WHERE request_id = @Id";
                    connection.Execute(sql, new { Id = requestId });
                }
                
                MessageBox.Show("Barang ditandai SIAP!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal update: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}