using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.stock.data.dtos;
using mtc_app.features.stock.data.enums;
using mtc_app.features.stock.data.repositories;
using mtc_app.features.stock.presentation.components; // Import custom component
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

using StockSortOrder = mtc_app.features.stock.data.enums.SortOrder;

namespace mtc_app.features.stock.presentation.screens
{
    public partial class StockDashboardForm : AppBaseForm
    {
        private readonly IStockRepository _repository;
        private RequestStatus _currentFilter = RequestStatus.Pending;
        private StockSortOrder _currentSort = StockSortOrder.Descending;
        
        // Notification Logic
        private Timer _timerNotifSound;
        private int _previousPendingCount = 0;
        private bool _isNotificationShowing = false;
        
        // Custom UI components (replacing designer placeholders if needed)
        private StatCard cardPendingNew;
        private StatCard cardReadyNew;
        private AppEmptyState emptyStateNew;

        public StockDashboardForm() : this(new StockRepository())
        {
        }

        public StockDashboardForm(IStockRepository repository)
        {
            _repository = repository;
            InitializeComponent();
            InitializeCustomComponents();
            InitializeNotificationTimer();
            InitializeDashboard();
        }

        private void InitializeNotificationTimer()
        {
            _timerNotifSound = new Timer();
            _timerNotifSound.Interval = 1500; // Loop sound every 1.5 seconds
            _timerNotifSound.Tick += TimerNotifSound_Tick;
        }

        private void TimerNotifSound_Tick(object sender, EventArgs e)
        {
            // Play Asterisk sound repeatedly until user dismisses the alert
            SystemSounds.Asterisk.Play();
        }

        private void InitializeCustomComponents()
        {
            // Initialize Stat Cards directly adding to pnlStatusCards
            // pnlStatusCards is available because we are in partial class
            
            // Pending Card
            cardPendingNew = new StatCard
            {
                Title = "Permintaan Pending",
                IconType = StatIconType.Checklist,
                AccentColor = AppColors.Warning,
                Location = new Point(25, 25),
                Size = new Size(300, 140), // Larger Size
            };
            pnlStatusCards.Controls.Add(cardPendingNew);

            // Ready Card - Adjusted X position to avoid overlap (25 + 300 + 20 gap = 345)
            cardReadyNew = new StatCard
            {
                Title = "Barang Siap",
                IconType = StatIconType.Trophy,
                AccentColor = AppColors.Success,
                Location = new Point(345, 25),
                Size = new Size(300, 140),
            };
             pnlStatusCards.Controls.Add(cardReadyNew);
            
            // Empty State
            emptyStateNew = new AppEmptyState
            {
                 Name = "emptyStateNew",
                 Title = "Tidak Ada Data",
                 Description = "Belum ada permintaan part.",
                 Dock = DockStyle.Fill,
                 Visible = false
            };
            pnlContent.Controls.Add(emptyStateNew);
            emptyStateNew.BringToFront(); // Ensure it's on top of grid if visible

            // --- Configure Grid Manually ---
            gridRequests.AutoGenerateColumns = false;
            gridRequests.Columns.Clear();

            // 1. No (Sequence)
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "No", 
                HeaderText = "No", 
                Width = 80, // Sedikit lebar utk font besar
                ReadOnly = true 
            });

            // 2. Waktu Request
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "RequestedAt", 
                HeaderText = "Waktu Request", 
                DataPropertyName = "FormattedRequestTime",
                Width = 180
            });

            // 3. Nama Part
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "PartName", 
                HeaderText = "Nama Part", 
                DataPropertyName = "PartDisplayName",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill 
            });

            // NEW: Machine Column
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Machine",
                HeaderText = "Mesin",
                DataPropertyName = "MachineName",
                Width = 150
            });

            // 4. Jumlah
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Qty", 
                HeaderText = "Jumlah", 
                DataPropertyName = "Qty",
                Width = 100
            });

            // 5. Teknisi (Pindah ke kiri Status)
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Technician", 
                HeaderText = "Teknisi", 
                DataPropertyName = "TechnicianName", 
                Width = 200
            });

            // 6. Status (Paling Kanan)
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Status", 
                HeaderText = "Status", 
                DataPropertyName = "StatusId", 
                Width = 150
            });
            
            // --- Accessibility: Larger Fonts & Rows ---
            gridRequests.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            gridRequests.DefaultCellStyle.Font = new Font("Segoe UI", 14F, FontStyle.Regular);
            gridRequests.RowTemplate.Height = 80; // Lebih tinggi biar lega
            
            // Add Formatting Event for Status & No
            gridRequests.CellFormatting += GridRequests_CellFormatting;
        }

        private void GridRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // 1. Sequence Number
            if (gridRequests.Columns[e.ColumnIndex].Name == "No")
            {
                e.Value = (e.RowIndex + 1).ToString();
            }

            // 2. Localized Status
            if (gridRequests.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                if (int.TryParse(e.Value.ToString(), out int statusId))
                {
                    if (statusId == 1) e.Value = "Menunggu"; // Pending
                    else if (statusId == 2) e.Value = "Siap"; // Ready
                    else if (statusId == 3) e.Value = "Diambil"; // Taken
                    else e.Value = "-";
                }
            }
        }

        private async void InitializeDashboard()
        {
            await LoadDataAsync(isInitialLoad: true);
            timerRefresh.Start();
        }

        private async void timerRefresh_Tick(object sender, EventArgs e)
        {
            // Silent refresh
            await LoadDataAsync();
        }

        private async Task LoadDataAsync(bool isInitialLoad = false)
        {
            try
            {
                // Parallel execution for stats and list
                var statsTask = _repository.GetStatsAsync();
                var requestsTask = _repository.GetRequestsAsync(_currentFilter, _currentSort);

                await Task.WhenAll(statsTask, requestsTask);
                
                var newStats = statsTask.Result;
                
                // NOTIFICATION LOGIC
                // Check if pending count increased AND notification is not already showing
                if (!isInitialLoad && newStats.PendingCount > _previousPendingCount && !_isNotificationShowing)
                {
                    _isNotificationShowing = true;
                    _timerNotifSound.Start();
                    
                    // Get latest part name (Assuming list is sorted DESC by default or by DB query)
                    // If current sort is ASC, we might need Last(). But repository usually defaults DESC for recent.
                    var latestRequest = requestsTask.Result.FirstOrDefault();
                    string partName = latestRequest != null ? latestRequest.PartDisplayName : "Barang Tidak Dikenal";

                    // Show Custom Notification Form
                    using (var notifForm = new NotificationForm(partName))
                    {
                        notifForm.ShowDialog();
                    }
                    
                    // After user clicks OK:
                    _timerNotifSound.Stop();
                    _isNotificationShowing = false;
                }
                
                // Update tracker
                _previousPendingCount = newStats.PendingCount;

                UpdateStats(newStats);
                DisplayRequests(requestsTask.Result);
                
                lblLastUpdate.Text = $"🕐 Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                if (!timerRefresh.Enabled)
                    MessageBox.Show($"Error memuat data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStats(StockStatsDto stats)
        {
            if (cardPendingNew != null)
            {
                cardPendingNew.Value = stats.PendingCount.ToString();
            }

            if (cardReadyNew != null)
            {
                cardReadyNew.Value = stats.ReadyCount.ToString();
            }
        }

        private void DisplayRequests(IEnumerable<PartRequestDto> requests)
        {
            var data = requests.ToList();
            
            if (data.Any())
            {
                gridRequests.Visible = true;
                if (emptyStateNew != null) emptyStateNew.Visible = false;
                gridRequests.DataSource = data;
            }
            else
            {
                gridRequests.Visible = false;
                if (emptyStateNew != null) 
                {
                    emptyStateNew.Visible = true;
                    UpdateEmptyStateMessage();
                }
            }
        }
        
        private void UpdateEmptyStateMessage()
        {
            if (emptyStateNew == null) return;

            switch (_currentFilter)
            {
                case RequestStatus.Pending:
                    emptyStateNew.Title = "Tidak Ada Permintaan Pending";
                    emptyStateNew.Description = "Semua permintaan sudah diproses.";
                    break;
                case RequestStatus.Ready:
                    emptyStateNew.Title = "Tidak Ada Barang Siap";
                    emptyStateNew.Description = "Belum ada barang yang siap diambil.";
                    break;
                default:
                    emptyStateNew.Title = "Tidak Ada Data";
                    emptyStateNew.Description = "Tidak ada permintaan part yang tersedia.";
                    break;
            }
        }

        private async void btnFilterPending_Click(object sender, EventArgs e)
        {
            _currentFilter = RequestStatus.Pending;
            UpdateFilterButtons();
            await LoadDataAsync();
        }

        private async void btnFilterReady_Click(object sender, EventArgs e)
        {
            _currentFilter = RequestStatus.Ready;
            UpdateFilterButtons();
            await LoadDataAsync();
        }

        private async void btnFilterAll_Click(object sender, EventArgs e)
        {
            _currentFilter = RequestStatus.None; // All
            UpdateFilterButtons();
            await LoadDataAsync();
        }

        private void UpdateFilterButtons()
        {
             // Reset types
            btnFilterPending.Type = AppButton.ButtonType.Secondary;
            btnFilterReady.Type = AppButton.ButtonType.Secondary;
            btnFilterAll.Type = AppButton.ButtonType.Secondary;

            switch (_currentFilter)
            {
                case RequestStatus.Pending:
                    btnFilterPending.Type = AppButton.ButtonType.Primary;
                    break;
                case RequestStatus.Ready:
                    btnFilterReady.Type = AppButton.ButtonType.Primary;
                    break;
                default:
                    btnFilterAll.Type = AppButton.ButtonType.Primary;
                    break;
            }
        }

        private async void btnSortAsc_Click(object sender, EventArgs e)
        {
            _currentSort = StockSortOrder.Ascending;
            UpdateSortButtons();
            await LoadDataAsync();
        }

        private async void btnSortDesc_Click(object sender, EventArgs e)
        {
            _currentSort = StockSortOrder.Descending;
            UpdateSortButtons();
            await LoadDataAsync();
        }

        private void UpdateSortButtons()
        {
            btnSortAsc.Type = _currentSort == StockSortOrder.Ascending ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnSortDesc.Type = _currentSort == StockSortOrder.Descending ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // [OPTIMIZATION] Stop Timers
            timerRefresh?.Stop();
            timerRefresh?.Dispose();
            
            _timerNotifSound?.Stop();
            _timerNotifSound?.Dispose();
            
            base.OnFormClosing(e);
        }

        private async void btnReady_Click(object sender, EventArgs e)
        {
            if (gridRequests.CurrentRow?.DataBoundItem is PartRequestDto request)
            {
                if (request.StatusId == 2)
                {
                    MessageBox.Show("Permintaan ini sudah berstatus SIAP.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                if (request.StatusId == 3)
                {
                   MessageBox.Show("Barang sudah diambil.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                   return;
                }

                if (MessageBox.Show("Tandai barang sebagai SIAP?", "Konfirmasi", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    bool success = await _repository.MarkAsReadyAsync(request.RequestId);
                    if (success)
                    {
                        MessageBox.Show("Berhasil ditandai siap.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadDataAsync();
                    }
                }
            }
            else
            {
                MessageBox.Show("Pilih permintaan terlebih dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
