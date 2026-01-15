using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.features.stock.presentation.components;

namespace mtc_app.features.stock.presentation.screens
{
    public partial class StockDashboardForm : AppBaseForm
    {
        private int _pendingCount = 0;
        private int _readyCount = 0;
        private int _todayCompletedCount = 0;

        public StockDashboardForm()
        {
            InitializeComponent();
            InitializeDashboard();
        }

        private void InitializeDashboard()
        {
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

                    LoadPendingRequests(connection);
                    LoadStatusCounts(connection);
                    UpdateStatusCards();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading stock data: {ex.Message}");
            }
        }

        private void LoadPendingRequests(IDbConnection connection)
        {
            const string sql = @"
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
            _pendingCount = data.Count;

            DisplayRequests(data);
        }

        private void LoadStatusCounts(IDbConnection connection)
        {
            _readyCount = connection.QuerySingle<int>(
                "SELECT COUNT(*) FROM part_requests WHERE status_id = 2"
            );

            // _todayCompletedCount = connection.QuerySingle<int>(@"
            //     SELECT COUNT(*) 
            //     FROM part_requests 
            //     WHERE status_id = 3 
            //     AND DATE(picked_at) = CURDATE()"
            // );
        }

        private void DisplayRequests(dynamic data)
        {
            if (data.Count > 0)
            {
                ShowGridView(data);
            }
            else
            {
                ShowEmptyState();
            }
        }

        private void ShowGridView(dynamic data)
        {
            gridRequests.Visible = true;
            emptyStatePanel.Visible = false;
            gridRequests.AutoGenerateColumns = true;
            gridRequests.DataSource = data;

            StyleDataGrid();
        }

        private void ShowEmptyState()
        {
            gridRequests.Visible = false;
            emptyStatePanel.Visible = true;
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
            UpdatePendingCard();
            UpdateReadyCard();
            // UpdateCompletedCard();
            UpdateLastUpdateTime();
        }

        private void UpdatePendingCard()
        {
            cardPending.Value = _pendingCount.ToString();
            cardPending.Subtext = _pendingCount == 0 ? "All clear!" : "Needs processing";
        }

        private void UpdateReadyCard()
        {
            cardReady.Value = _readyCount.ToString();
            cardReady.Subtext = _readyCount == 0 ? "None waiting" : "Awaiting pickup";
        }

        // private void UpdateCompletedCard()
        // {
        //     cardCompleted.Value = _todayCompletedCount.ToString();
        //     cardCompleted.Subtext = "Completed today";
        // }

        private void UpdateLastUpdateTime()
        {
            lblLastUpdate.Text = $"🕐 Last updated: {DateTime.Now:HH:mm:ss}";
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
            if (!ValidateSelection()) return;

            var requestId = GetSelectedRequestId();
            if (requestId == null) return;

            if (!ConfirmReadyAction()) return;

            MarkRequestAsReady(requestId);
        }

        private bool ValidateSelection()
        {
            if (gridRequests.SelectedRows.Count == 0)
            {
                ShowInfo("Pilih request terlebih dahulu!");
                return false;
            }
            return true;
        }

        private object GetSelectedRequestId()
        {
            return gridRequests.CurrentRow?.Cells[0].Value;
        }

        private bool ConfirmReadyAction()
        {
            var result = MessageBox.Show(
                "Tandai barang sebagai READY?",
                "Konfirmasi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            return result == DialogResult.Yes;
        }

        private void MarkRequestAsReady(object requestId)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    const string sql = @"
                        UPDATE part_requests 
                        SET status_id = 2, ready_at = NOW() 
                        WHERE request_id = @Id";

                    connection.Execute(sql, new { Id = requestId });
                }

                ShowSuccess("Barang berhasil ditandai READY!");
                LoadData();
            }
            catch (Exception ex)
            {
                ShowError($"Gagal update: {ex.Message}");
            }
        }

        private void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}