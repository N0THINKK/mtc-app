using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.stock.data.dtos;
using mtc_app.features.stock.data.repositories;
using mtc_app.features.technician.presentation.controllers;
using mtc_app.features.rating.presentation.screens;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class StockDataControl : UserControl, IStockDataView
    {
        private readonly StockDataController _controller;

        // UI Components
        private StatCard _cardPending;
        private StatCard _cardReady;
        private DataGridView _grid;
        private AppEmptyState _emptyState;

        public StockDataControl(IStockRepository repository)
        {
            _controller = new StockDataController(this, repository);
            InitializeComponent();
        }

        // ========================================================
        // IStockDataView Implementation
        // ========================================================
        public void UpdateStats(StockStatsDto stats)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStats(stats)));
                return;
            }
            _cardPending.Value = stats.PendingCount.ToString();
            _cardReady.Value = stats.ReadyCount.ToString();
        }

        public void DisplayRequests(List<PartRequestDto> requests)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => DisplayRequests(requests)));
                return;
            }
            
            if (requests.Any())
            {
                _grid.Visible = true;
                _emptyState.Visible = false;
                _grid.DataSource = requests;
            }
            else
            {
                _grid.Visible = false;
                _emptyState.Visible = true;
            }
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

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            await _controller.LoadDataAsync(start, end);
        }

        // ========================================================
        // UI Construction
        // ========================================================

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = AppColors.CardBackground;
            this.Dock = DockStyle.Fill;

            var pnlContent = BuildContentPanel();
            var pnlCards = BuildCardsPanel();

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlCards);

            var lblTitle = new Label
            {
                Text = "Data Permintaan Part",
                Font = AppFonts.PageTitle,
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 40
            };
            this.Controls.Add(lblTitle);

            this.ResumeLayout(false);
        }

        private Panel BuildCardsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 200,
                BackColor = Color.FromArgb(248, 249, 250), 
                Padding = new Padding(20, 25, 20, 25)
            };

            _cardPending = new StatCard
            {
                Title = "Permintaan Pending",
                IconType = StatIconType.Checklist,
                AccentColor = AppColors.Warning,
                Location = new Point(25, 25),
                Size = new Size(300, 140) 
            };

            _cardReady = new StatCard
            {
                Title = "Barang Siap",
                IconType = StatIconType.Trophy,
                AccentColor = AppColors.Success,
                Location = new Point(345, 25), 
                Size = new Size(300, 140)
            };

            panel.Controls.Add(_cardPending);
            panel.Controls.Add(_cardReady);
            return panel;
        }

        private Panel BuildContentPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(25, 20, 25, 20) 
            };

            _grid = BuildDataGrid();

            _emptyState = new AppEmptyState
            {
                Title = "Tidak Ada Data",
                Description = "Tidak ada permintaan part di rentang tanggal ini.",
                Dock = DockStyle.Fill,
                Visible = false
            };

            panel.Controls.Add(_emptyState);
            panel.Controls.Add(_grid);
            _emptyState.BringToFront();

            return panel;
        }

        private DataGridView BuildDataGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = AppColors.CardBackground,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                GridColor = Color.FromArgb(222, 226, 230),
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = AppFonts.Header3,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = AppColors.TextPrimary,
                SelectionBackColor = Color.FromArgb(248, 250, 252),
                SelectionForeColor = AppColors.TextPrimary,
                Padding = new Padding(5)
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = AppFonts.Header3,
                ForeColor = AppColors.TextPrimary,
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = AppColors.TextPrimary
            };

            grid.RowTemplate.Height = 80;

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "No",
                HeaderText = "No",
                Width = 80,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RequestedAt",
                HeaderText = "Waktu Request",
                DataPropertyName = "FormattedRequestTime",
                Width = 180
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PartName",
                HeaderText = "Nama Part",
                DataPropertyName = "PartDisplayName",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Machine",
                HeaderText = "Mesin",
                DataPropertyName = "MachineName",
                Width = 150
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Qty",
                HeaderText = "Jumlah",
                DataPropertyName = "Qty",
                Width = 100
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Technician",
                HeaderText = "Teknisi",
                DataPropertyName = "TechnicianName",
                Width = 200
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                DataPropertyName = "StatusId",
                Width = 150
            });

            grid.CellFormatting += Grid_CellFormatting;
            grid.CellDoubleClick += Grid_CellDoubleClick;

            return grid;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (_grid.Columns[e.ColumnIndex].Name == "No")
            {
                e.Value = (e.RowIndex + 1).ToString();
            }

            if (_grid.Columns[e.ColumnIndex].Name == "Technician" && e.Value is string techName && !string.IsNullOrEmpty(techName))
            {
                var names = techName.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                if (names.Length > 1)
                {
                    e.Value = $"{names[0]} + {names.Length - 1} lainnya";
                }
            }

            if (_grid.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                if (int.TryParse(e.Value.ToString(), out int statusId))
                {
                    switch (statusId)
                    {
                        case 1: e.Value = "Menunggu"; break;
                        case 2: e.Value = "Siap"; break;
                        case 3: e.Value = "Diambil"; break;
                        default: e.Value = "-"; break;
                    }
                }
            }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (_grid.Rows[e.RowIndex].DataBoundItem is PartRequestDto request)
            {
                if (request.TicketId <= 0) return;

                using (var form = new RatingTechnicianForm(request.TicketId))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}
