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
        private AppButton btnMarkResolved;
        
        private DataGridView gridPatrols;
        private AppEmptyState emptyState;
        private Panel pnlContent;
        private Panel pnlTopBanner;
        private Panel pnlFilters;
        private Panel pnlActions;

        private DateTime _startDate;
        private DateTime _endDate;

        public TechnicianPatrolControl(ITechnicianRepository repository)
        {
            _repository = repository;
            InitializeComponentLayout();
        }

        private void InitializeComponentLayout()
        {
            this.Size = new Size(1200, 700);
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;

            // ==========================================
            // TOP BANNER (Stats Cards)
            // ==========================================
            pnlTopBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                BackColor = Color.FromArgb(248, 249, 250), // Matches StockDashboard
                Padding = new Padding(20, 25, 20, 25)
            };
            this.Controls.Add(pnlTopBanner);

            cardPending = new StatCard
            {
                Title = "NG (Belum Diperbaiki)",
                IconType = StatIconType.None,
                AccentColor = AppColors.Danger,
                Location = new Point(25, 25),
                Size = new Size(300, 140)
            };
            
            cardResolved = new StatCard
            {
                Title = "NG (Selesai)",
                IconType = StatIconType.Checklist,
                AccentColor = AppColors.Success,
                Location = new Point(345, 25),
                Size = new Size(300, 140)
            };
            pnlTopBanner.Controls.Add(cardPending);
            pnlTopBanner.Controls.Add(cardResolved);

            // ==========================================
            // FILTER PANEL
            // ==========================================
            pnlFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = AppColors.CardBackground
            };
            
            pnlFilters.Paint += (s, e) =>
            {
                // Only draw top and bottom borders, leave left and right empty "plong sampe pojok"
                using (Pen p = new Pen(AppColors.Border))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlFilters.Width, 0);
                    e.Graphics.DrawLine(p, 0, pnlFilters.Height - 1, pnlFilters.Width, pnlFilters.Height - 1);
                }
            };

            FlowLayoutPanel flowLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(20, 22, 0, 0)
            };

            Label lblFilter = new Label
            {
                Text = "Filter:",
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(73, 80, 87),
                Margin = new Padding(0, 10, 10, 0)
            };

            btnFilterNg = CreateFilterButton("⏳ Belum Diperbaiki", 160, () => { _currentFilter = "NG"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });
            btnFilterResolved = CreateFilterButton("✓ Selesai", 130, () => { _currentFilter = "Selesai"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });
            btnFilterAll = CreateFilterButton("📋 Semua", 130, () => { _currentFilter = "Semua"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });

            flowLeft.Controls.Add(lblFilter);
            flowLeft.Controls.Add(btnFilterNg);
            flowLeft.Controls.Add(btnFilterResolved);
            flowLeft.Controls.Add(btnFilterAll);

            FlowLayoutPanel flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 22, 20, 0)
            };

            btnSortDesc = CreateFilterButton("↓ Terbaru", 120, () => { _currentSort = "DESC"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });
            btnSortAsc = CreateFilterButton("↑ Terlama", 120, () => { _currentSort = "ASC"; UpdateFilterButtons(); _ = LoadDataAsync(_startDate, _endDate); });
            
            Label lblSort = new Label
            {
                Text = "Urutkan:",
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(73, 80, 87),
                Margin = new Padding(0, 10, 10, 0)
            };

            flowRight.Controls.Add(btnSortDesc);
            flowRight.Controls.Add(btnSortAsc);
            flowRight.Controls.Add(lblSort);

            pnlFilters.Controls.Add(flowLeft);
            pnlFilters.Controls.Add(flowRight);

            // ==========================================
            // BOTTOM ACTIONS PANEL
            // ==========================================
            pnlActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                Width = 1200, // Force width for anchor calcs
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(25, 18, 25, 18)
            };

            btnRefresh = new AppButton
            {
                Text = "🔄 Refresh",
                Type = AppButton.ButtonType.Secondary,
                Location = new Point(25, 20),
                Size = new Size(130, 50),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 12F, FontStyle.Regular)
            };
            btnRefresh.Click += (s, e) => { _ = LoadDataAsync(_startDate, _endDate); };

            btnMarkResolved = new AppButton
            {
                Text = "✓ TANDAI TELAH DIPERBAIKI",
                Type = AppButton.ButtonType.Primary,
                Location = new Point(pnlActions.Width - 275, 20),
                Size = new Size(250, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };
            btnMarkResolved.Click += BtnMarkResolved_Click;

            pnlActions.Controls.Add(btnRefresh);
            pnlActions.Controls.Add(btnMarkResolved);

            // ==========================================
            // CONTENT PANEL (Grid + Empty State)
            // ==========================================
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(25, 20, 25, 20)
            };
            
            // Re-order controls for correct docking behavior
            this.Controls.Add(pnlContent); // Fill
            this.Controls.Add(pnlFilters); // Top
            this.Controls.Add(pnlActions); // Bottom
            this.Controls.Add(pnlTopBanner); // Top

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
                GridColor = Color.FromArgb(222, 226, 230),
                Visible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                EnableHeadersVisualStyles = false
            };

            // Grid Styling
            gridPatrols.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = AppFonts.Header3,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = AppColors.TextPrimary,
                SelectionBackColor = Color.FromArgb(248, 250, 252),
                SelectionForeColor = AppColors.TextPrimary,
                Padding = new Padding(5)
            };
            
            gridPatrols.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = AppFonts.Header3,
                ForeColor = AppColors.TextPrimary,
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = AppColors.TextPrimary
            };
            gridPatrols.RowTemplate.Height = 80;

            // Columns
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "No", Width = 80 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatrolDate", HeaderText = "Waktu Laporkan", DataPropertyName = "FormattedPatrolDate", Width = 180 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Machine", HeaderText = "Mesin", DataPropertyName = "MachineName", Width = 150 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reporter", HeaderText = "Pelapor", DataPropertyName = "RoleTarget", Width = 120 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "Item NG", DataPropertyName = "ItemName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Note", HeaderText = "Keterangan", DataPropertyName = "ActionNote", Width = 250 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 150 });

            gridPatrols.CellFormatting += GridPatrols_CellFormatting;
            gridPatrols.CellDoubleClick += GridPatrols_CellDoubleClick;
            pnlContent.Controls.Add(gridPatrols);

            UpdateFilterButtons();
        }

        private AppButton CreateFilterButton(string text, int width, Action onClick)
        {
            var btn = new AppButton
            {
                Text = text,
                Size = new Size(width, 45),
                Type = AppButton.ButtonType.Secondary,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Margin = new Padding(0, 0, 10, 0)
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

        private void GridPatrols_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            
            if (gridPatrols.Rows[e.RowIndex].DataBoundItem is PatrolNgDto dto)
            {
                gridPatrols.CurrentCell = gridPatrols.Rows[e.RowIndex].Cells[0];
                long ticketId = dto.TicketId ?? 0;
                
                // Show detail form for viewing
                using (var ratingForm = new mtc_app.features.rating.presentation.screens.RatingTechnicianForm(ticketId, dto)) 
                {
                    ratingForm.ShowDialog();
                }
            }
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
                if (status == "NOT_OK" || status == "NG")
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
                    // Tampilkan Form Penilaian Operator terlebih dahulu
                    // Dto bisa memiliki TicketId null jika operator mensubmit NG tanpa memicu Auto-Ticket
                    long ticketIdForRating = dto.TicketId ?? 0;
                    
                    using (var ratingForm = new mtc_app.features.rating.presentation.screens.RatingTechnicianForm(ticketIdForRating, dto))
                    {
                        if (ratingForm.ShowDialog() == DialogResult.OK)
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
                }
            }
            else
            {
                MessageBox.Show("Pilih baris NG terlebih dahulu dari tabel.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
