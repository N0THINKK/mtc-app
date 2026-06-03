using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.features.technician.presentation.controllers;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public partial class TechnicianPatrolControl : UserControl, ITechnicianPatrolView
    {
        private readonly TechnicianPatrolController _controller;
        
        private string _currentFilter = "NG"; // "NG", "Selesai", "Semua"
        private string _currentSort = "DESC"; // "DESC", "ASC"
        private string _currentRoleFilter = "Semua"; // "Semua", "Teknisi", "Operator"
        private string _currentItemFilter = "Semua"; // "Semua" or specific item name
        
        private StatCard cardPending;
        private StatCard cardResolved;
        private AppButton btnFilterNg;
        private AppButton btnFilterResolved;
        private AppButton btnFilterAll;
        private ComboBox cbRoleFilter;
        private ComboBox cbItemFilter;
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
            _controller = new TechnicianPatrolController(this, repository);
            InitializeComponentLayout();
        }

        // ==========================================
        // ITechnicianPatrolView Implementation
        // ==========================================
        public string CurrentFilter => _currentFilter;
        public string CurrentSort => _currentSort;
        public string CurrentRoleFilter => _currentRoleFilter;
        public string CurrentItemFilter => _currentItemFilter;

        public void UpdateStats(int pendingCount, int resolvedCount)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStats(pendingCount, resolvedCount)));
                return;
            }
            cardPending.Value = pendingCount.ToString();
            cardResolved.Value = resolvedCount.ToString();
        }

        public void UpdateGrid(List<PatrolNgDto> patrols)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateGrid(patrols)));
                return;
            }
            gridPatrols.DataSource = patrols;
            gridPatrols.Visible = patrols.Any();
        }

        public void UpdateItemFilterList(List<string> items, string previousSelection)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateItemFilterList(items, previousSelection)));
                return;
            }
            
            cbItemFilter.SelectedIndexChanged -= CbItemFilter_SelectedIndexChanged;
            cbItemFilter.Items.Clear();
            cbItemFilter.Items.Add("Semua");
            foreach (var itemName in items)
            {
                cbItemFilter.Items.Add(itemName);
            }
            int idx = cbItemFilter.Items.IndexOf(previousSelection);
            cbItemFilter.SelectedIndex = idx >= 0 ? idx : 0;
            cbItemFilter.SelectedIndexChanged += CbItemFilter_SelectedIndexChanged;
        }

        public void ShowEmptyState(string title, string description)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowEmptyState(title, description)));
                return;
            }
            emptyState.Title = title;
            emptyState.Description = description;
            emptyState.Visible = true;
        }

        public void HideEmptyState()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HideEmptyState()));
                return;
            }
            emptyState.Visible = false;
        }

        public void ShowError(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowError(message)));
                return;
            }
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

        public void ShowWarning(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowWarning(message)));
                return;
            }
            MessageBox.Show(message, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public bool ConfirmAction(string title, string message)
        {
            if (this.InvokeRequired)
            {
                return (bool)this.Invoke(new Func<bool>(() => ConfirmAction(title, message)));
            }
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        // ==========================================
        // UI Layout & Logic
        // ==========================================

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            _startDate = start;
            _endDate = end;
            await _controller.LoadDataAsync(start, end);
        }

        private void InitializeComponentLayout()
        {
            this.Size = new Size(1200, 700);
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;

            // TOP BANNER
            pnlTopBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                BackColor = Color.FromArgb(248, 249, 250),
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

            // FILTER PANEL
            pnlFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = AppColors.CardBackground
            };
            
            pnlFilters.Paint += (s, e) =>
            {
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

            flowRight.Controls.Add(new Panel { Width = 30, Height = 10, BackColor = Color.Transparent });

            cbRoleFilter = new ComboBox
            {
                Width = 150,
                Margin = new Padding(0, 10, 10, 0),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
            };
            cbRoleFilter.Items.AddRange(new object[] { "Semua", "Teknisi", "Operator" });
            cbRoleFilter.SelectedIndex = 0;
            cbRoleFilter.SelectedIndexChanged += (s, e) => 
            {
                _currentRoleFilter = cbRoleFilter.SelectedItem?.ToString() ?? "Semua";
                _ = LoadDataAsync(_startDate, _endDate);
            };

            Label lblRoleFilter = new Label
            {
                Text = "Pelapor:",
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(73, 80, 87),
                Margin = new Padding(0, 10, 10, 0)
            };

            flowRight.Controls.Add(cbRoleFilter);
            flowRight.Controls.Add(lblRoleFilter);

            FlowLayoutPanel flowSecondRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(20, 8, 20, 0)
            };

            Label lblItemFilter = new Label
            {
                Text = "Item NG:",
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(73, 80, 87),
                Margin = new Padding(0, 10, 10, 0)
            };

            cbItemFilter = new ComboBox
            {
                Width = 350,
                Margin = new Padding(0, 10, 10, 0),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
            };
            cbItemFilter.Items.Add("Semua");
            cbItemFilter.SelectedIndex = 0;
            cbItemFilter.SelectedIndexChanged += CbItemFilter_SelectedIndexChanged;

            flowSecondRow.Controls.Add(lblItemFilter);
            flowSecondRow.Controls.Add(cbItemFilter);

            pnlFilters.Controls.Add(flowLeft);
            pnlFilters.Controls.Add(flowRight);
            pnlFilters.Controls.Add(flowSecondRow);

            // ACTIONS PANEL
            pnlActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                Width = 1200,
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
            btnMarkResolved.Click += async (s, e) => 
            {
                if (gridPatrols.CurrentRow?.DataBoundItem is PatrolNgDto dto)
                {
                    await _controller.MarkResolvedAsync(dto, _startDate, _endDate);
                }
                else
                {
                    ShowWarning("Pilih baris NG terlebih dahulu dari tabel.");
                }
            };

            pnlActions.Controls.Add(btnRefresh);
            pnlActions.Controls.Add(btnMarkResolved);

            // CONTENT PANEL
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(25, 20, 25, 20)
            };
            
            this.Controls.Add(pnlContent); 
            this.Controls.Add(pnlFilters); 
            this.Controls.Add(pnlActions); 
            this.Controls.Add(pnlTopBanner); 

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

            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "No", Width = 80 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatrolDate", HeaderText = "Waktu Laporkan", DataPropertyName = "FormattedPatrolDate", Width = 180 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Machine", HeaderText = "Mesin", DataPropertyName = "MachineName", Width = 150 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reporter", HeaderText = "Pelapor", DataPropertyName = "RoleTarget", Width = 120 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "Item NG", DataPropertyName = "ItemName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
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

        private void CbItemFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            _currentItemFilter = cbItemFilter.SelectedItem?.ToString() ?? "Semua";
            _ = LoadDataAsync(_startDate, _endDate);
        }

        private void GridPatrols_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            
            if (gridPatrols.Rows[e.RowIndex].DataBoundItem is PatrolNgDto dto)
            {
                gridPatrols.CurrentCell = gridPatrols.Rows[e.RowIndex].Cells[0];
                long ticketId = dto.TicketId ?? 0;
                
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
                    e.Value = "OPEN";
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
    }
}
