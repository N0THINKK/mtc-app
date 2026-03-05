using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public partial class TechnicianPatrolControl : UserControl
    {
        private readonly ITechnicianRepository _repository;
        private string _currentFilter = "NG"; // "NG", "Selesai", "Semua"
        private string _currentSort = "DESC"; // "DESC", "ASC"
        
        private StatCard cardPending;
        private StatCard cardResolved;
        private AppButton btnFilterNg;
        private AppButton btnFilterResolved;
        private AppButton btnFilterAll;
        private AppButton btnSortDesc;
        private AppButton btnSortAsc;
        private AppButton btnRefresh;
        
        private DataGridView gridPatrols;
        private AppEmptyState emptyState;
        private Panel pnlContent;
        private Panel pnlTopBanner;

        private DateTime _startDate;
        private DateTime _endDate;

        public TechnicianPatrolControl(ITechnicianRepository repository)
        {
            _repository = repository;
            InitializeComponentLayout();
        }

        private void InitializeComponentLayout()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;
            this.Padding = new Padding(20);

            // ==========================================
            // TOP BANNER (Stats & Filters)
            // ==========================================
            pnlTopBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlTopBanner);

            // Stat Cards
            cardPending = new StatCard
            {
                Title = "NG (Belum Diperbaiki)",
                IconType = StatIconType.None,
                AccentColor = AppColors.Danger,
                Location = new Point(0, 0),
                Size = new Size(300, 140)
            };
            
            cardResolved = new StatCard
            {
                Title = "NG (Selesai)",
                IconType = StatIconType.Checklist,
                AccentColor = AppColors.Success,
                Location = new Point(320, 0),
                Size = new Size(300, 140)
            };

            pnlTopBanner.Controls.Add(cardPending);
            pnlTopBanner.Controls.Add(cardResolved);

            // Filters & Actions (Right aligned)
            FlowLayoutPanel flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };

            btnRefresh = CreateActionButton("↻ Segarkan", () => LoadDataAsync(_startDate, _endDate));
            btnSortAsc = CreateActionButton("↑ Terlama", () => { _currentSort = "ASC"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });
            btnSortDesc = CreateActionButton("↓ Terbaru", () => { _currentSort = "DESC"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });
            btnFilterAll = CreateActionButton("Tampilkan Semua", () => { _currentFilter = "Semua"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });
            btnFilterResolved = CreateActionButton("Selesai", () => { _currentFilter = "Selesai"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });
            btnFilterNg = CreateActionButton("Belum Diperbaiki", () => { _currentFilter = "NG"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });

            flowFilters.Controls.Add(btnRefresh);
            flowFilters.Controls.Add(btnSortAsc);
            flowFilters.Controls.Add(btnSortDesc);
            flowFilters.Controls.Add(btnFilterAll);
            flowFilters.Controls.Add(btnFilterResolved);
            flowFilters.Controls.Add(btnFilterNg);
            
            pnlTopBanner.Controls.Add(flowFilters);

            // ==========================================
            // CONTENT PANEL (Grid + Empty State)
            // ==========================================
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 20, 0, 0),
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlContent);
            pnlContent.BringToFront();

            emptyState = new AppEmptyState
            {
                Title = "Tidak Ada Data NG",
                Description = "Semua mesin dalam kondisi prima.",
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlContent.Controls.Add(emptyState);

            gridPatrols = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.CardBackground,
                BorderStyle = BorderStyle.None,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                GridColor = AppColors.Border,
                Visible = false
            };

            // Grid Styling
            gridPatrols.EnableHeadersVisualStyles = false;
            gridPatrols.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            gridPatrols.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Surface;
            gridPatrols.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.TextSecondary;
            gridPatrols.ColumnHeadersDefaultCellStyle.Font = AppFonts.Header3;
            gridPatrols.ColumnHeadersHeight = 50;
            
            gridPatrols.DefaultCellStyle.BackColor = AppColors.CardBackground;
            gridPatrols.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            gridPatrols.DefaultCellStyle.SelectionBackColor = AppColors.PrimaryLight;
            gridPatrols.DefaultCellStyle.SelectionForeColor = AppColors.TextPrimary;
            gridPatrols.DefaultCellStyle.Font = AppFonts.Body;
            gridPatrols.RowTemplate.Height = 70;

            // Columns
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "No", Width = 60 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatrolDate", HeaderText = "Waktu Laporkan", DataPropertyName = "FormattedPatrolDate", Width = 150 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Machine", HeaderText = "Mesin", DataPropertyName = "MachineName", Width = 150 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reporter", HeaderText = "Pelapor", DataPropertyName = "RoleTarget", Width = 100 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "Item NG", DataPropertyName = "ItemName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Note", HeaderText = "Keterangan", DataPropertyName = "ActionNote", Width = 200 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 150 });

            // Status Formatting
            gridPatrols.CellFormatting += GridPatrols_CellFormatting;
            pnlContent.Controls.Add(gridPatrols);

            // ==========================================
            // BOTTOM PANEL (Action Button)
            // ==========================================
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                Padding = new Padding(0, 20, 0, 0),
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlBottom);

            AppButton btnMarkResolved = new AppButton
            {
                Text = "Tandai Telah Diperbaiki",
                Type = AppButton.ButtonType.Primary,
                Width = 250,
                Height = 45,
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnMarkResolved.Click += BtnMarkResolved_Click;
            pnlBottom.Controls.Add(btnMarkResolved);

            UpdateFilterButtons();
        }

        private AppButton CreateActionButton(string text, Action onClick)
        {
            var btn = new AppButton
            {
                Text = text,
                Type = AppButton.ButtonType.Secondary,
                AutoSize = true,
                Margin = new Padding(0, 0, AppDimens.MarginSmall, 0),
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void UpdateFilterButtons()
        {
            btnFilterNg.Type = _currentFilter == "NG" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnFilterResolved.Type = _currentFilter == "Selesai" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnFilterAll.Type = _currentFilter == "Semua" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;

            btnSortDesc.Type = _currentSort == "DESC" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnSortAsc.Type = _currentSort == "ASC" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            
            btnFilterNg.Invalidate();
            btnFilterResolved.Invalidate();
            btnFilterAll.Invalidate();
            btnSortDesc.Invalidate();
            btnSortAsc.Invalidate();
        }

        private void GridPatrols_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (gridPatrols.Columns[e.ColumnIndex].Name == "No")
            {
                e.Value = (e.RowIndex + 1).ToString();
            }
            else if (gridPatrols.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString();
                if (status == "NOT_OK")
                {
                    e.Value = "NG - Pending";
                    e.CellStyle.ForeColor = AppColors.Danger;
                    e.CellStyle.Font = new Font(gridPatrols.DefaultCellStyle.Font, FontStyle.Bold);
                }
                else if (status == "PERBAIKAN_OK")
                {
                    e.Value = "Selesai";
                    e.CellStyle.ForeColor = AppColors.Success;
                    e.CellStyle.Font = new Font(gridPatrols.DefaultCellStyle.Font, FontStyle.Bold);
                }
            }
        }

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            _startDate = start;
            _endDate = end;

            try
            {
                // Parallel fetch
                var statsTask = _repository.GetPatrolNgStatsAsync(start, end);
                var listTask = _repository.GetPatrolNgListAsync(_currentFilter, _currentSort, start, end);

                await Task.WhenAll(statsTask, listTask);

                var stats = statsTask.Result;
                var list = listTask.Result.ToList();

                // Update Stats
                cardPending.Value = (stats?.PendingCount ?? 0).ToString();
                cardResolved.Value = (stats?.ResolvedCount ?? 0).ToString();

                // Update Grid
                if (list.Any())
                {
                    gridPatrols.Visible = true;
                    emptyState.Visible = false;
                    gridPatrols.DataSource = list;
                }
                else
                {
                    gridPatrols.Visible = false;
                    emptyState.Visible = true;

                    if (_currentFilter == "NG")
                    {
                        emptyState.Title = "Tidak Ada NG Pending";
                        emptyState.Description = "Semua masalah checksheet telah diselesaikan.";
                    }
                    else if (_currentFilter == "Selesai")
                    {
                        emptyState.Title = "Belum Ada NG Selesai";
                        emptyState.Description = "Belum ada riwayat perbaikan NG pada rentang tanggal ini.";
                    }
                    else
                    {
                        emptyState.Title = "Tidak Ada Data";
                        emptyState.Description = "Tidak ada riwayat NG yang dilaporkan.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data Patroli: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnMarkResolved_Click(object sender, EventArgs e)
        {
            if (gridPatrols.CurrentRow?.DataBoundItem is PatrolNgDto dto)
            {
                if (dto.Status == "PERBAIKAN_OK")
                {
                    MessageBox.Show("Item ini sudah berstatus Selesai.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Apakah Anda yakin telah memperbaiki masalah ini?\n\nMesin: {dto.MachineName}\nItem: {dto.ItemName}", 
                    "Konfirmasi Perbaikan", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    bool success = await _repository.MarkPatrolNgAsResolvedAsync(dto.DetailId);
                    if (success)
                    {
                        MessageBox.Show("Berhasil ditandai selesai.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadDataAsync(_startDate, _endDate);
                    }
                    else
                    {
                        MessageBox.Show("Gagal memperbarui status. Silakan coba lagi.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Pilih baris NG terlebih dahulu dari tabel.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
