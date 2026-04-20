using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.applicator_patrol.data.dtos;
using mtc_app.features.applicator_patrol.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public partial class TechnicianApplicatorPatrolControl : UserControl
    {
        private readonly IApplicatorPatrolRepository _repository;
        private string _currentSort = "DESC"; // "DESC", "ASC"
        
        private StatCard cardNg;
        private AppButton btnSortDesc;
        private AppButton btnSortAsc;
        private AppButton btnRefresh;
        
        private DataGridView gridPatrols;
        private AppEmptyState emptyState;
        private Panel pnlContent;
        private Panel pnlTopBanner;
        private Panel pnlFilters;
        private Panel pnlActions;

        private DateTime _startDate;
        private DateTime _endDate;

        public TechnicianApplicatorPatrolControl(IApplicatorPatrolRepository repository)
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
                BackColor = Color.FromArgb(248, 249, 250), 
                Padding = new Padding(20, 25, 20, 25)
            };
            this.Controls.Add(pnlTopBanner);

            cardNg = new StatCard
            {
                Title = "Item Aplikator NG",
                IconType = StatIconType.None,
                AccentColor = AppColors.Danger,
                Location = new Point(25, 25),
                Size = new Size(300, 140)
            };
            pnlTopBanner.Controls.Add(cardNg);

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

            pnlActions.Controls.Add(btnRefresh);

            // ==========================================
            // CONTENT PANEL (Grid + Empty State)
            // ==========================================
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
                Title = "Tidak Ada Data NG Aplikator",
                Description = "Semua aplikator dalam kondisi baik berdasar patrol operator.",
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
                SelectionForeColor = AppColors.TextPrimary,
                WrapMode = DataGridViewTriState.True
            };
            gridPatrols.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            gridPatrols.RowTemplate.MinimumHeight = 80;

            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "No", Width = 80 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatrolDate", HeaderText = "Waktu Laporan", DataPropertyName = "FormattedPatrolDate", Width = 180 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Machine", HeaderText = "Mesin", DataPropertyName = "MachineName", Width = 150 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Operator", HeaderText = "Pelapor", DataPropertyName = "OperatorNik", Width = 120 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Applicator", HeaderText = "Kode App", DataPropertyName = "ApplicatorCode", Width = 150 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Side", HeaderText = "Sisi", DataPropertyName = "Side", Width = 80 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Judgment", HeaderText = "Status", DataPropertyName = "Judgment", Width = 100 });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Items", HeaderText = "Item NG", DataPropertyName = "NgItems", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            gridPatrols.CellFormatting += GridPatrols_CellFormatting;
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
            btnSortDesc.Type = _currentSort == "DESC" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnSortAsc.Type = _currentSort == "ASC" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            
            btnSortDesc.Invalidate();
            btnSortAsc.Invalidate();
        }

        private void GridPatrols_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;

            if (gridPatrols.Columns[e.ColumnIndex].Name == "No")
            {
                e.Value = (e.RowIndex + 1).ToString();
            }
            else if (gridPatrols.Columns[e.ColumnIndex].Name == "Judgment")
            {
                string status = e.Value.ToString();
                if (status == "NG")
                {
                    e.CellStyle.ForeColor = AppColors.Danger;
                    e.CellStyle.Font = new Font(gridPatrols.DefaultCellStyle.Font, FontStyle.Bold);
                }
            }
            else if (gridPatrols.Columns[e.ColumnIndex].Name == "Items")
            {
                string val = e.Value.ToString();
                if (string.IsNullOrWhiteSpace(val)) return;

                string[] numbers = val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var labels = new List<string>();
                foreach (var numStr in numbers)
                {
                    if (int.TryParse(numStr, out int idx) && 
                        idx >= 1 && 
                        idx <= mtc_app.features.applicator_patrol.presentation.components.NgItemSelectorForm.ITEM_LABELS.Length)
                    {
                        labels.Add(mtc_app.features.applicator_patrol.presentation.components.NgItemSelectorForm.ITEM_LABELS[idx - 1]);
                    }
                    else
                    {
                        labels.Add($"Item {numStr}");
                    }
                }
                e.Value = string.Join(Environment.NewLine, labels);
            }
        }

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            _startDate = start;
            _endDate = end;

            try
            {
                var statsTask = _repository.GetApplicatorNgStatsAsync(start, end);
                var listTask = _repository.GetApplicatorNgListAsync(start, end, _currentSort);

                await Task.WhenAll(statsTask, listTask);

                var stats = statsTask.Result;
                var list = listTask.Result.ToList();

                cardNg.Value = (stats?.TotalNgCount ?? 0).ToString();

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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data NG Aplikator: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
