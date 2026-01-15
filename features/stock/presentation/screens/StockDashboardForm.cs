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
        private string _currentFilter = "PENDING"; // PENDING, READY, ALL
        private string _currentSort = "DESC"; // ASC, DESC

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

                    LoadRequestsWithFilter(connection);
                    LoadStatusCounts(connection);
                    UpdateStatusCards();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error memuat data: {ex.Message}");
            }
        }

        private void LoadRequestsWithFilter(IDbConnection connection)
        {
            string whereClause = GetWhereClauseForFilter();
            string orderClause = _currentSort == "ASC" ? "ASC" : "DESC";

            string sql = $@"
                SELECT 
                    pr.request_id AS 'ID',
                    pr.requested_at AS 'Waktu Request',
                    pr.part_name_manual AS 'Nama Barang',
                    IFNULL(u.full_name, 'N/A') AS 'Nama Teknisi',
                    pr.qty AS 'Jumlah',
                    rs.status_name AS 'Status'
                FROM part_requests pr
                LEFT JOIN tickets t ON pr.ticket_id = t.ticket_id
                LEFT JOIN users u ON t.technician_id = u.user_id
                LEFT JOIN request_statuses rs ON pr.status_id = rs.status_id
                {whereClause}
                ORDER BY pr.requested_at {orderClause}";

            var data = connection.Query(sql).ToList();
            DisplayRequests(data);
        }

        private string GetWhereClauseForFilter()
        {
            switch (_currentFilter)
            {
                case "PENDING":
                    return "WHERE pr.status_id = 1";
                case "READY":
                    return "WHERE pr.status_id = 2";
                case "ALL":
                    return "WHERE pr.status_id IN (1, 2)";
                default:
                    return "WHERE pr.status_id = 1";
            }
        }

        private void LoadStatusCounts(IDbConnection connection)
        {
            _pendingCount = connection.QuerySingle<int>(
                "SELECT COUNT(*) FROM part_requests WHERE status_id = 1"
            );

            _readyCount = connection.QuerySingle<int>(
                "SELECT COUNT(*) FROM part_requests WHERE status_id = 2"
            );
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

        private void HideIdColumnSafely()
        {
            if (gridRequests.Columns.Contains("ID"))
            {
                gridRequests.Columns["ID"].Visible = false;
            }
        }

        private void ShowGridView(dynamic data)
        {
            gridRequests.Visible = true;
            emptyStatePanel.Visible = false;

            gridRequests.AutoGenerateColumns = false;
            gridRequests.DataSource = null;
            gridRequests.Columns.Clear();
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn {
                Name = "Waktu Request",
                DataPropertyName = "Waktu Request",
                HeaderText = "Waktu Request",
                Width = 150
            });

            gridRequests.Columns.Add(new DataGridViewTextBoxColumn {
                Name = "Nama Barang",
                DataPropertyName = "Nama Barang",
                HeaderText = "Nama Barang",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            gridRequests.Columns.Add(new DataGridViewTextBoxColumn {
                Name = "Nama Teknisi",
                DataPropertyName = "Nama Teknisi",
                HeaderText = "Nama Teknisi",
                Width = 150
            });

            gridRequests.Columns.Add(new DataGridViewTextBoxColumn {
                Name = "Jumlah",
                DataPropertyName = "Jumlah",
                HeaderText = "Jumlah",
                Width = 80
            });

            gridRequests.Columns.Add(new DataGridViewTextBoxColumn {
                Name = "Status",
                DataPropertyName = "Status",
                HeaderText = "Status",
                Width = 120
            });

            gridRequests.DataSource = data;
            StyleDataGrid();
        }
        

        private void ShowEmptyState()
        {
            gridRequests.Visible = false;
            emptyStatePanel.Visible = true;
            
            UpdateEmptyStateMessage();
        }

        private void UpdateEmptyStateMessage()
        {
            switch (_currentFilter)
            {
                case "PENDING":
                    emptyStatePanel.Title = "Tidak Ada Permintaan Pending";
                    emptyStatePanel.Description = "Semua permintaan sudah diproses atau ditandai siap.";
                    emptyStatePanel.Icon = "✅";
                    break;
                case "READY":
                    emptyStatePanel.Title = "Tidak Ada Barang Siap";
                    emptyStatePanel.Description = "Belum ada barang yang ditandai siap untuk diambil.";
                    emptyStatePanel.Icon = "📦";
                    break;
                case "ALL":
                    emptyStatePanel.Title = "Tidak Ada Data";
                    emptyStatePanel.Description = "Tidak ada permintaan part yang tersedia.";
                    emptyStatePanel.Icon = "📋";
                    break;
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

            // Hide ID column
            // if (gridRequests.Columns.Count > 0)
            // {
            //     gridRequests.Columns[0].Visible = false;
                
            //     // Set column widths
            //     if (gridRequests.Columns.Count > 1)
            //     {
            //         gridRequests.Columns[1].Width = 150; // Waktu Request
            //         gridRequests.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // Nama Barang
            //         gridRequests.Columns[3].Width = 150; // Nama Teknisi
            //         gridRequests.Columns[4].Width = 80; // Jumlah
            //         gridRequests.Columns[5].Width = 120; // Status
            //     }
            // }
        }

        private void UpdateStatusCards()
        {
            UpdatePendingCard();
            UpdateReadyCard();
            UpdateLastUpdateTime();
        }

        private void UpdatePendingCard()
        {
            cardPending.Value = _pendingCount.ToString();
            cardPending.Subtext = _pendingCount == 0 ? "Semua selesai!" : "Perlu diproses";
        }

        private void UpdateReadyCard()
        {
            cardReady.Value = _readyCount.ToString();
            cardReady.Subtext = _readyCount == 0 ? "Tidak ada" : "Menunggu diambil";
        }

        private void UpdateLastUpdateTime()
        {
            lblLastUpdate.Text = $"🕐 Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";
        }

        private void UpdateFilterButtons()
        {
            // Reset all to secondary
            btnFilterPending.Type = AppButton.ButtonType.Secondary;
            btnFilterReady.Type = AppButton.ButtonType.Secondary;
            btnFilterAll.Type = AppButton.ButtonType.Secondary;

            // Highlight active filter
            switch (_currentFilter)
            {
                case "PENDING":
                    btnFilterPending.Type = AppButton.ButtonType.Primary;
                    break;
                case "READY":
                    btnFilterReady.Type = AppButton.ButtonType.Primary;
                    break;
                case "ALL":
                    btnFilterAll.Type = AppButton.ButtonType.Primary;
                    break;
            }
        }

        private void UpdateSortButtons()
        {
            btnSortAsc.Type = _currentSort == "ASC" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnSortDesc.Type = _currentSort == "DESC" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
        }

        // Filter button events
        private void btnFilterPending_Click(object sender, EventArgs e)
        {
            _currentFilter = "PENDING";
            UpdateFilterButtons();
            LoadData();
        }

        private void btnFilterReady_Click(object sender, EventArgs e)
        {
            _currentFilter = "READY";
            UpdateFilterButtons();
            LoadData();
        }

        private void btnFilterAll_Click(object sender, EventArgs e)
        {
            _currentFilter = "ALL";
            UpdateFilterButtons();
            LoadData();
        }

        // Sort button events
        private void btnSortAsc_Click(object sender, EventArgs e)
        {
            _currentSort = "ASC";
            UpdateSortButtons();
            LoadData();
        }

        private void btnSortDesc_Click(object sender, EventArgs e)
        {
            _currentSort = "DESC";
            UpdateSortButtons();
            LoadData();
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
                ShowInfo("Pilih permintaan terlebih dahulu!");
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
                "Tandai barang sebagai SIAP untuk diambil?",
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

                ShowSuccess("Barang berhasil ditandai SIAP!");
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