using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.micrometer_patrol.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.micrometer_patrol.presentation.screens
{
    public class MicrometerPatrolHistoryForm : AppBaseForm
    {
        private readonly IMicrometerPatrolRepository _repository;
        private DataGridView _dgvHistory;
        private Label _lblStatus;

        public MicrometerPatrolHistoryForm(IMicrometerPatrolRepository repository)
        {
            _repository = repository;
            InitializeUI();
            this.Load += async (s, e) => await LoadHistoryDataAsync();
        }

        private void InitializeUI()
        {
            this.Text = "Riwayat Patroli Mikrometer (Hari Ini)";
            this.Size = new Size(1200, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = this.BackColor };
            var lblTitle = new Label
            {
                Text = "Riwayat Patroli Mikrometer (Hari Ini)",
                Font = AppFonts.Header2,
                ForeColor = AppColors.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(20, 0, 0, 20)
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            var pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 20) };
            this.Controls.Add(pnlContent);

            _dgvHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                BackgroundColor = AppColors.CardBackground,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersVisible = true,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ScrollBars = ScrollBars.Both,
                RowTemplate = { Height = 45 }
            };

            _dgvHistory.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppColors.Surface,
                ForeColor = AppColors.TextPrimary,
                Font = AppFonts.Body,
                Padding = new Padding(5),
                SelectionBackColor = AppColors.Surface,
                SelectionForeColor = AppColors.TextPrimary,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                WrapMode = DataGridViewTriState.True
            };

            _dgvHistory.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = AppColors.TextSecondary,
                Font = AppFonts.Body,
                Padding = new Padding(15, 10, 15, 10),
                SelectionBackColor = AppColors.PrimaryLight,
                SelectionForeColor = AppColors.PrimaryDark,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            pnlContent.Controls.Add(_dgvHistory);

            _lblStatus = new Label
            {
                Text = "Memuat data...",
                Font = AppFonts.Body,
                ForeColor = AppColors.TextSecondary,
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(_lblStatus);

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = this.BackColor };
            var btnClose = new Button
            {
                Text = "TUTUP",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Crimson,
                Size = new Size(120, 40),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, 10)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);

            this.Controls.Add(pnlBottom);
            pnlHeader.SendToBack();
            pnlBottom.SendToBack();
            _lblStatus.SendToBack();
            pnlContent.BringToFront();
        }

        private async Task LoadHistoryDataAsync()
        {
            try
            {
                _dgvHistory.Visible = false;
                _lblStatus.Visible = true;

                var data = await _repository.GetTodayPatrolsAsync(DateTime.Now);
                var listData = new System.Collections.Generic.List<mtc_app.features.micrometer_patrol.data.dtos.MicrometerPatrolDto>(data);

                if (listData.Count == 0)
                {
                    _lblStatus.Text = "Belum ada riwayat hari ini.";
                }
                else
                {
                    DataTable pivotTable = new DataTable();
                    pivotTable.Columns.Add("Checksheet Item", typeof(string));

                    foreach (var record in listData)
                    {
                        string colName = $"{record.PatrolDate.ToString("dd/MM/yyyy")} ({record.ShiftName})";
                        if (!pivotTable.Columns.Contains(colName))
                        {
                            pivotTable.Columns.Add(colName, typeof(string));
                        }
                    }

                    string[] points = new string[] 
                    {
                        "1. Ada Nomer Registrasi dan tidak Expired",
                        "2. Angka terbaca dengan jelas",
                        "3. Zero setting OK",
                        "4. Kondisi Thimble, Anvil dan Spindle OK",
                        "5. Baut Pengunci tidak longgar/Dol"
                    };

                    for (int i = 0; i < 5; i++)
                    {
                        var row = pivotTable.NewRow();
                        row[0] = points[i];

                        foreach (var record in listData)
                        {
                            string colName = $"{record.PatrolDate.ToString("dd/MM/yyyy")} ({record.ShiftName})";
                            string val = "";
                            if (i == 0) val = record.Point1;
                            if (i == 1) val = record.Point2;
                            if (i == 2) val = record.Point3;
                            if (i == 3) val = record.Point4;
                            if (i == 4) val = record.Point5;

                            row[colName] = val;
                        }
                        pivotTable.Rows.Add(row);
                    }

                    var noteRow = pivotTable.NewRow();
                    noteRow[0] = "Keterangan";
                    foreach (var record in listData)
                    {
                        string colName = $"{record.PatrolDate.ToString("dd/MM/yyyy")} ({record.ShiftName})";
                        noteRow[colName] = record.Notes;
                    }
                    pivotTable.Rows.Add(noteRow);

                    _dgvHistory.DataSource = pivotTable;
                    
                    _dgvHistory.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                    _dgvHistory.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    _dgvHistory.Columns[0].Frozen = true;

                    for (int i = 1; i < _dgvHistory.Columns.Count; i++)
                    {
                        _dgvHistory.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        _dgvHistory.Columns[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        _dgvHistory.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells; 
                    }

                    _dgvHistory.CellFormatting -= DgvHistory_CellFormatting;
                    _dgvHistory.CellFormatting += DgvHistory_CellFormatting;

                    _dgvHistory.Visible = true;
                    _lblStatus.Visible = false;
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Gagal memuat history: " + ex.Message;
                _lblStatus.ForeColor = AppColors.Danger;
                _dgvHistory.Visible = false;
            }
        }

        private void DgvHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex > 0 && e.Value != null)
            {
                string status = e.Value.ToString();
                if (status == "OK")
                {
                    e.CellStyle.ForeColor = AppColors.Success;
                    e.CellStyle.Font = AppFonts.Subtitle;
                }
                else if (status == "NG")
                {
                    e.CellStyle.ForeColor = AppColors.Danger;
                    e.CellStyle.Font = AppFonts.Subtitle;
                }
                else if (status == "NA" || status == "Tidak ada/ Tidak Pakai")
                {
                    e.CellStyle.ForeColor = Color.DimGray;
                    e.CellStyle.Font = AppFonts.Subtitle;
                }
            }
        }
    }
}
