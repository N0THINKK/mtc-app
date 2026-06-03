using System;
using System.Collections.Generic;
using System.Drawing;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.stock.data.dtos;
using mtc_app.features.stock.data.enums;
using mtc_app.features.stock.data.repositories;
using mtc_app.features.stock.presentation.components;
using mtc_app.features.stock.presentation.controllers;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

using StockSortOrder = mtc_app.features.stock.data.enums.SortOrder;

namespace mtc_app.features.stock.presentation.screens
{
    public partial class StockDashboardForm : AppBaseForm, IStockDashboardView
    {
        private readonly StockDashboardController _controller;
        private RequestStatus _currentFilter = RequestStatus.Pending;
        private StockSortOrder _currentSort = StockSortOrder.Descending;
        
        private Timer _timerNotifSound;
        private bool _isLoading = false;
        
        private StatCard cardPendingNew;
        private StatCard cardReadyNew;
        private AppEmptyState emptyStateNew;

        public StockDashboardForm() : this(ServiceLocator.CreateStockRepository())
        {
        }

        public StockDashboardForm(IStockRepository repository)
        {
            _controller = new StockDashboardController(this, repository);
            InitializeComponent();
            InitializeCustomComponents();
            InitializeNotificationTimer();

            this.Shown += StockDashboardForm_Shown;
        }

        // ========================================================
        // IStockDashboardView Implementation
        // ========================================================
        
        public RequestStatus CurrentFilter => _currentFilter;
        public StockSortOrder CurrentSort => _currentSort;

        public void UpdateStats(StockStatsDto stats)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStats(stats)));
                return;
            }
            if (cardPendingNew != null) cardPendingNew.Value = stats.PendingCount.ToString();
            if (cardReadyNew != null) cardReadyNew.Value = stats.ReadyCount.ToString();
        }

        public void DisplayRequests(List<PartRequestDto> requests)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => DisplayRequests(requests)));
                return;
            }
            
            if (requests.Count > 0)
            {
                gridRequests.Visible = true;
                if (emptyStateNew != null) emptyStateNew.Visible = false;
                gridRequests.DataSource = requests;
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

        public void ShowNotification(string partName)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowNotification(partName)));
                return;
            }

            _timerNotifSound.Start();
            using (var notifForm = new NotificationForm(partName))
            {
                notifForm.ShowDialog();
            }
            _timerNotifSound.Stop();
        }

        public void UpdateEmptyStateMessage()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateEmptyStateMessage));
                return;
            }
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

        public void ShowError(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowError(message)));
                return;
            }
            if (!timerRefresh.Enabled)
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowSuccess(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowSuccess(message)));
                return;
            }
            MessageBox.Show(message, "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public bool ShowConfirmation(string title, string message)
        {
            if (this.InvokeRequired)
            {
                return (bool)this.Invoke(new Func<bool>(() => ShowConfirmation(title, message)));
            }
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            return result == DialogResult.Yes;
        }

        public void UpdateLastUpdateTime(string timeString)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateLastUpdateTime(timeString)));
                return;
            }
            lblLastUpdate.Text = $"🕐 Terakhir diperbarui: {timeString}";
        }

        // ========================================================
        // UI Construction
        // ========================================================

        private void InitializeCustomComponents()
        {
            cardPendingNew = new StatCard
            {
                Title = "Permintaan Pending",
                IconType = StatIconType.Checklist,
                AccentColor = AppColors.Warning,
                Location = new Point(25, 25),
                Size = new Size(300, 140), 
            };
            pnlStatusCards.Controls.Add(cardPendingNew);

            cardReadyNew = new StatCard
            {
                Title = "Barang Siap",
                IconType = StatIconType.Trophy,
                AccentColor = AppColors.Success,
                Location = new Point(345, 25),
                Size = new Size(300, 140),
            };
             pnlStatusCards.Controls.Add(cardReadyNew);
            
            emptyStateNew = new AppEmptyState
            {
                 Name = "emptyStateNew",
                 Title = "Tidak Ada Data",
                 Description = "Belum ada permintaan part.",
                 Dock = DockStyle.Fill,
                 Visible = false
            };
            pnlContent.Controls.Add(emptyStateNew);
            emptyStateNew.BringToFront();

            gridRequests.AutoGenerateColumns = false;
            gridRequests.Columns.Clear();

            gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "No", Width = 80, ReadOnly = true });
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "RequestedAt", HeaderText = "Waktu Request", DataPropertyName = "FormattedRequestTime", Width = 140 });
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReadyAt", HeaderText = "Waktu Siap", DataPropertyName = "FormattedReadyTime", Width = 140 });
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartName", HeaderText = "Nama Part", DataPropertyName = "PartDisplayName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "Machine", HeaderText = "Mesin", DataPropertyName = "MachineName", Width = 150 });
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Jumlah", DataPropertyName = "Qty", Width = 100 });
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "Technician", HeaderText = "Teknisi", DataPropertyName = "TechnicianName", Width = 200 });
            gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "StatusId", Width = 150 });
            
            gridRequests.ColumnHeadersDefaultCellStyle.Font = AppFonts.Header3;
            gridRequests.DefaultCellStyle.Font = AppFonts.Header3;
            gridRequests.RowTemplate.Height = 80;
            
            gridRequests.CellFormatting += GridRequests_CellFormatting;
        }

        private void InitializeNotificationTimer()
        {
            _timerNotifSound = new Timer();
            _timerNotifSound.Interval = 1500;
            _timerNotifSound.Tick += (s, e) => SystemSounds.Asterisk.Play();
        }

        // ========================================================
        // UI Events
        // ========================================================

        private async void StockDashboardForm_Shown(object sender, EventArgs e)
        {
            await Task.Delay(50);
            await LoadDataAsync();
            timerRefresh.Start();
            gridRequests.Focus();
        }

        private async void timerRefresh_Tick(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                await _controller.LoadDataAsync();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void GridRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (gridRequests.Columns[e.ColumnIndex].Name == "No")
            {
                e.Value = (e.RowIndex + 1).ToString();
            }

            if (gridRequests.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                if (int.TryParse(e.Value.ToString(), out int statusId))
                {
                    if (statusId == 1) e.Value = "Menunggu"; 
                    else if (statusId == 2) e.Value = "Siap"; 
                    else if (statusId == 3) e.Value = "Diambil"; 
                    else if (statusId == 4) e.Value = "Ditolak"; 
                    else e.Value = "-";
                }
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
            _currentFilter = RequestStatus.None;
            UpdateFilterButtons();
            await LoadDataAsync();
        }

        private async void btnFilterRejected_Click(object sender, EventArgs e)
        {
            _currentFilter = RequestStatus.Rejected;
            UpdateFilterButtons();
            await LoadDataAsync();
        }

        private void UpdateFilterButtons()
        {
            btnFilterPending.Type = _currentFilter == RequestStatus.Pending ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnFilterReady.Type = _currentFilter == RequestStatus.Ready ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnFilterAll.Type = _currentFilter == RequestStatus.None ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnFilterRejected.Type = _currentFilter == RequestStatus.Rejected ? AppButton.ButtonType.Danger : AppButton.ButtonType.Secondary;
                
            btnFilterPending.Invalidate();
            btnFilterReady.Invalidate();
            btnFilterAll.Invalidate();
            btnFilterRejected.Invalidate();
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

                await _controller.MarkAsReadyAsync(request.RequestId);
            }
            else
            {
                MessageBox.Show("Pilih permintaan terlebih dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnReject_Click(object sender, EventArgs e)
        {
            if (gridRequests.CurrentRow?.DataBoundItem is PartRequestDto request)
            {
                if (request.StatusId == 3)
                {
                   MessageBox.Show("Barang sudah diambil, tidak bisa ditolak.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                   return;
                }

                if (request.StatusId == 4)
                {
                    MessageBox.Show("Permintaan ini sudah berstatus DITOLAK.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                await _controller.RejectRequestAsync(request.RequestId);
            }
            else
            {
                MessageBox.Show("Pilih permintaan terlebih dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
