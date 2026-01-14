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
        public StockDashboardForm()
        {
            InitializeComponent();
            LoadData();
            timerRefresh.Start();
        }

        private void LoadData()
        {
            // To prevent issues if user clicks while refresh is happening
            this.Enabled = false; 
            
            flowLayoutPanelRequests.Controls.Clear();
            
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string sql = @"
                        SELECT 
                            pr.request_id,
                            pr.requested_at,
                            t.ticket_display_code,
                            pr.part_name_manual,
                            pr.qty
                        FROM part_requests pr
                        LEFT JOIN tickets t ON pr.ticket_id = t.ticket_id
                        WHERE pr.status_id = 1
                        ORDER BY pr.requested_at ASC";

                    var requests = connection.Query(sql).ToList();
                    
                    if (requests.Any())
                    {
                        foreach (var req in requests)
                        {
                            var card = new StockRequestCardControl(
                                (int)req.request_id,
                                (string)req.part_name_manual,
                                (int)req.qty,
                                (string)req.ticket_display_code,
                                (DateTime)req.requested_at
                            );
                            card.OnReadyClicked += Card_OnReadyClicked;
                            flowLayoutPanelRequests.Controls.Add(card);
                        }
                    }
                    else
                    {
                        var lblEmpty = new AppLabel { 
                            Text = "Tidak ada permintaan sparepart.",
                            Font = new Font("Segoe UI", 14F, FontStyle.Italic),
                            ForeColor = Color.Gray,
                            Margin = new Padding(20)
                        };
                        flowLayoutPanelRequests.Controls.Add(lblEmpty);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stock data: {ex.Message}");
            }
            finally
            {
                this.Enabled = true;
            }
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
                
                // Animate removal (optional, for now just reload)
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal update: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            LoadData();
        }
    }
}
