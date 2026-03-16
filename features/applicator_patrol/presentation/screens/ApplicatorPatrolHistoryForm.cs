using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.applicator_patrol.data.dtos;
using mtc_app.features.applicator_patrol.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.applicator_patrol.presentation.screens
{
    /// <summary>
    /// Form riwayat Patroli Harian Aplikator.
    /// Menampilkan daftar patroli yang sudah disimpan, bisa filter tanggal.
    /// Klik baris untuk melihat detail aplikator (OK/NG/NA).
    /// </summary>
    public class ApplicatorPatrolHistoryForm : Form
    {
        private readonly IApplicatorPatrolRepository _repository;
        private readonly int _machineId;
        private readonly string _machineCode;

        // Controls
        private DateTimePicker dtpFilter;
        private CheckBox chkFilterDate;
        private AppButton btnRefresh, btnTutup;
        private DataGridView dgvHistory;
        private Panel pnlDetail;
        private Label lblDetailTitle;
        private DataGridView dgvDetail;

        private List<ApplicatorPatrolHistoryDto> _history = new List<ApplicatorPatrolHistoryDto>();

        public ApplicatorPatrolHistoryForm(IApplicatorPatrolRepository repository, int machineId, string machineCode)
        {
            _repository = repository;
            _machineId = machineId;
            _machineCode = machineCode;
            InitializeUI();
            LoadHistoryAsync();
        }

        private void InitializeUI()
        {
            this.Text = $"Riwayat Patroli Aplikator — {_machineCode}";
            this.ClientSize = new Size(1000, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // ── Title bar ────────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text = "RIWAYAT PATROLI HARIAN APLIKATOR",
                Font = new Font(AppFonts.FontFamily, 14, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Bounds = new Rectangle(12, 12, 680, 36)
            };
            this.Controls.Add(lblTitle);

            btnTutup = new AppButton
            {
                Text = "Tutup", Type = AppButton.ButtonType.Secondary,
                Bounds = new Rectangle(900, 12, 85, 32)
            };
            btnTutup.Click += (s, e) => this.Close();
            this.Controls.Add(btnTutup);

            this.Controls.Add(new Panel { Bounds = new Rectangle(0, 52, 1000, 1), BackColor = AppColors.Border });

            // ── Filter bar ────────────────────────────────────────────────
            chkFilterDate = new CheckBox
            {
                Text = "Filter Tanggal:",
                Font = new Font(AppFonts.FontFamily, 10, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true, Location = new Point(12, 66),
                Checked = false
            };
            chkFilterDate.CheckedChanged += (s, e) => dtpFilter.Enabled = chkFilterDate.Checked;
            this.Controls.Add(chkFilterDate);

            dtpFilter = new DateTimePicker
            {
                Value = DateTime.Today,
                Format = DateTimePickerFormat.Short,
                Bounds = new Rectangle(130, 63, 130, 26),
                Enabled = false,
                Font = new Font(AppFonts.FontFamily, 10, FontStyle.Regular)
            };
            this.Controls.Add(dtpFilter);

            btnRefresh = new AppButton
            {
                Text = "🔄 Refresh", Type = AppButton.ButtonType.Primary,
                Bounds = new Rectangle(270, 61, 90, 30)
            };
            btnRefresh.Click += (s, e) => LoadHistoryAsync();
            this.Controls.Add(btnRefresh);

            var lblInfo = new Label
            {
                Text = "Klik baris untuk melihat detail aplikator",
                Font = new Font(AppFonts.FontFamily, 9, FontStyle.Italic),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true, Location = new Point(375, 68)
            };
            this.Controls.Add(lblInfo);

            this.Controls.Add(new Panel { Bounds = new Rectangle(0, 98, 1000, 1), BackColor = AppColors.Border });

            // ── Tabel history (atas) ──────────────────────────────────────
            dgvHistory = new DataGridView
            {
                Bounds = new Rectangle(12, 106, 976, 240),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(AppFonts.FontFamily, 10, FontStyle.Regular),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(200, 40, 40),
                    ForeColor = Color.White,
                    Font = new Font(AppFonts.FontFamily, 10, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };
            dgvHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvHistory.ColumnHeadersHeight = 32;
            dgvHistory.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvHistory.DefaultCellStyle.SelectionForeColor = Color.White;

            // Kolom
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",   HeaderText = "Tanggal",     FillWeight = 14 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShift",  HeaderText = "Shift",       FillWeight = 8 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSide",   HeaderText = "Sisi",        FillWeight = 6 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOp",     HeaderText = "Operator",    FillWeight = 16 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",  HeaderText = "Total Appl.", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNg",     HeaderText = "Jml NG",     FillWeight = 10,  DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status",      FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            dgvHistory.CellFormatting += DgvHistory_CellFormatting;
            dgvHistory.SelectionChanged += DgvHistory_SelectionChanged;
            this.Controls.Add(dgvHistory);

            // ── Panel detail (bawah) ──────────────────────────────────────
            pnlDetail = new Panel { Bounds = new Rectangle(12, 356, 976, 252), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            this.Controls.Add(pnlDetail);

            lblDetailTitle = new Label
            {
                Text = "Detail Aplikator — pilih baris di atas",
                Font = new Font(AppFonts.FontFamily, 10, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = false, Dock = DockStyle.Top, Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                BackColor = Color.FromArgb(245, 245, 245)
            };
            pnlDetail.Controls.Add(lblDetailTitle);

            dgvDetail = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                Font = new Font(AppFonts.FontFamily, 10, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(255, 210, 30),
                    ForeColor = AppColors.TextPrimary,
                    Font = new Font(AppFonts.FontFamily, 10, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };
            dgvDetail.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvDetail.ColumnHeadersHeight = 28;

            dgvDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "dNo",   HeaderText = "No.",           FillWeight = 5,  DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "dCode", HeaderText = "No. Aplikator", FillWeight = 20 });
            dgvDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "dJudge",HeaderText = "Kondisi",       FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "dItems",HeaderText = "Item yang NG",  FillWeight = 65 });

            dgvDetail.CellFormatting += DgvDetail_CellFormatting;
            pnlDetail.Controls.Add(dgvDetail);
        }

        private async void LoadHistoryAsync()
        {
            btnRefresh.Enabled = false;
            btnRefresh.Text = "Loading...";
            try
            {
                DateTime? filterDate = chkFilterDate.Checked ? dtpFilter.Value.Date : (DateTime?)null;
                _history = await _repository.GetHistoryAsync(_machineId, filterDate);

                dgvHistory.Rows.Clear();
                foreach (var h in _history)
                {
                    string status = h.TotalNg > 0 ? "NG" : "OK";
                    dgvHistory.Rows.Add(
                        h.PatrolDate.ToString("dd/MM/yyyy"),
                        h.ShiftName,
                        h.Side,
                        h.OperatorNik ?? "-",
                        h.TotalAplikator,
                        h.TotalNg,
                        status
                    );
                }

                if (dgvHistory.Rows.Count == 0)
                {
                    lblDetailTitle.Text = "Tidak ada data patroli yang ditemukan.";
                    dgvDetail.Rows.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat riwayat: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
                btnRefresh.Text = "🔄 Refresh";
            }
        }

        private async void DgvHistory_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvHistory.SelectedRows.Count == 0) return;
            int rowIdx = dgvHistory.SelectedRows[0].Index;
            if (rowIdx < 0 || rowIdx >= _history.Count) return;

            var h = _history[rowIdx];
            lblDetailTitle.Text = $"Detail — {h.PatrolDate:dd/MM/yyyy}  Shift {h.ShiftName}  Sisi {h.Side}  |  Total: {h.TotalAplikator}  NG: {h.TotalNg}";

            try
            {
                var details = await _repository.GetDetailsAsync(h.LogId);
                dgvDetail.Rows.Clear();
                int no = 1;
                foreach (var d in details)
                {
                    string itemDesc = FormatNgItems(d.NgItems);
                    dgvDetail.Rows.Add(no++, d.ApplicatorCode, d.Judgment, itemDesc);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat detail: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _history.Count) return;
            // Warna baris NG = merah muda
            if (_history[e.RowIndex].TotalNg > 0)
                dgvHistory.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);

            // Kolom Status: teks warna
            if (dgvHistory.Columns[e.ColumnIndex].Name == "colStatus" && e.Value != null)
            {
                e.CellStyle.ForeColor = e.Value.ToString() == "NG" ? Color.FromArgb(200, 30, 30) : Color.FromArgb(30, 150, 30);
                e.CellStyle.Font = new Font(AppFonts.FontFamily, 10, FontStyle.Bold);
            }
        }

        private static readonly string[] ITEM_NAMES = {
            "1: Crimper Anvil / Anvil holder / Supporting stopper",
            "2: Posisi Ram",
            "3: Kondisi pot oil PA5",
            "4: Wire stopper / Safety cover / Strip terminal EASY",
            "5: Crimper front/rear / Anvil punggung / Shear blade",
            "6: Crimper Anvil (Micrometer)",
            "7: Validasi Appl"
        };

        private static string FormatNgItems(string ngItems)
        {
            if (string.IsNullOrEmpty(ngItems)) return "";
            var parts = ngItems.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var names = new System.Collections.Generic.List<string>();
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int idx) && idx >= 1 && idx <= ITEM_NAMES.Length)
                    names.Add(ITEM_NAMES[idx - 1]);
            }
            return names.Count > 0 ? string.Join("  |  ", names) : ngItems;
        }

        private void DgvDetail_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvDetail.Columns[e.ColumnIndex].Name == "dJudge" && e.Value != null)
            {
                string v = e.Value.ToString();
                e.CellStyle.ForeColor = v == "NG" ? Color.FromArgb(200, 30, 30) : v == "OK" ? Color.FromArgb(30, 150, 30) : Color.Gray;
                e.CellStyle.Font = new Font(AppFonts.FontFamily, 10, FontStyle.Bold);
                if (v == "NG") dgvDetail.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
            }
        }
    }
}
