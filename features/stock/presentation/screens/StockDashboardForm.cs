using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.stock.presentation.components;
using mtc_app.shared.presentation.components;

namespace mtc_app.features.stock.presentation.screens
{
    public partial class StockDashboardForm : AppBaseForm
    {
        private int pendingCount = 0;
        private int readyCount = 0;
        private int todayCompletedCount = 0;

        public StockDashboardForm()
        {
            InitializeComponent();
            LoadData();
            timerRefresh.Start();
        }

        private void LoadData()
        {
            // Prevent interaction during refresh
            this.Enabled = false;
            
            flowLayoutPanelRequests.Controls.Clear();
            
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    // Query untuk mengambil request yang PENDING (status_id = 1)
                    string sql = @"
                        SELECT 
                            pr.request_id,
                            pr.requested_at,
                            pr.part_name_manual,
                            u.full_name AS technician_name
                        FROM part_requests pr
                        LEFT JOIN tickets t ON pr.ticket_id = t.ticket_id
                        LEFT JOIN users u ON t.technician_id = u.user_id
                        WHERE pr.status_id = 1
                        ORDER BY pr.requested_at ASC";

                    var requests = connection.Query(sql).ToList();
                    pendingCount = requests.Count;

                    // Get ready count
                    string readySql = "SELECT COUNT(*) FROM part_requests WHERE status_id = 2";
                    readyCount = connection.QuerySingle<int>(readySql);

                    // Get today's completed count
                    string completedSql = @"
                        SELECT COUNT(*) 
                        FROM part_requests 
                        WHERE status_id = 3 
                        AND DATE(picked_at) = CURDATE()";
                    todayCompletedCount = connection.QuerySingle<int>(completedSql);

                    // Update status cards
                    UpdateStatusCards();

                    // Display requests as cards or empty state
                    if (requests.Any())
                    {
                        emptyStatePanel.Visible = false;
                        flowLayoutPanelRequests.Visible = true;
                        
                        foreach (var req in requests)
                        {
                            var card = new StockRequestCardControl(
                                (int)req.request_id,
                                (string)req.part_name_manual,
                                (string)req.technician_name ?? "N/A",
                                (DateTime)req.requested_at
                            );
                            card.OnReadyClicked += Card_OnReadyClicked;
                            flowLayoutPanelRequests.Controls.Add(card);
                        }
                    }
                    else
                    {
                        flowLayoutPanelRequests.Visible = false;
                        emptyStatePanel.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stock data: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void UpdateStatusCards()
        {
            cardPending.Value = pendingCount.ToString();
            cardPending.Subtext = pendingCount == 0 ? "All clear!" : "Needs processing";

            cardReady.Value = readyCount.ToString();
            cardReady.Subtext = readyCount == 0 ? "None waiting" : "Awaiting pickup";

            cardCompleted.Value = todayCompletedCount.ToString();
            cardCompleted.Subtext = "Completed today";

            // Update last refresh time
            lblLastUpdate.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
        }

        private void Card_OnReadyClicked(object sender, int requestId)
        {
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

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            LoadData();
        }
    }
}