using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public class ChecksheetHistoryForm : AppBaseForm
    {
        private readonly IMachineHistoryRepository _repository;
        private readonly int _machineId;
        private readonly int _templateId;
        private readonly string _roleTarget;

        private DataGridView _dgvHistory;
        private Label _lblStatus;
        private Panel _pnlContent;

        public ChecksheetHistoryForm(int machineId, int templateId, string roleTarget)
        {
            _repository = new MachineHistoryRepository();
            _machineId = machineId;
            _templateId = templateId;
            _roleTarget = roleTarget;

            InitializeForm();
            InitializeUI();
            
            this.Load += async (s, e) => await LoadHistoryDataAsync();
        }

        private void InitializeForm()
        {
            this.Text = "Riwayat Patroli Checksheet";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void InitializeUI()
        {
            // Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = this.BackColor
            };

            var lblTitle = new Label
            {
                Text = "Riwayat Patroli Checksheet (30 Hari Terakhir)",
                Font = AppFonts.Header2,
                ForeColor = AppColors.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft, // Posisi teks di bawah agar ada margin
                Padding = new Padding(20, 0, 0, 20) // Padding kiri 20, bawah 20
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Container for Grid
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 0, 20, 20)
            };
            this.Controls.Add(_pnlContent);
            _pnlContent.BringToFront(); // Memastikan _pnlContent merender mengisi sisa ruang di bawah pnlHeader

            // DataGridView
            _dgvHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                BackgroundColor = AppColors.CardBackground,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single, // Beri border bawah agar jelas batasnya
                ColumnHeadersVisible = true, // Pastikan eksplisit visible
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize, // Kembali ke AutoSize responsif
                ScrollBars = ScrollBars.Both, 
                RowTemplate = { Height = 45 } 
            };

            // Grid Styling
            _dgvHistory.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppColors.Surface, // Ubah ke abu-abu muda (Surface) agar tidak menyatu dengan background putih
                ForeColor = AppColors.TextPrimary,
                Font = AppFonts.Body, // Gunakan Body font agar fit
                Padding = new Padding(5), // Padding normal
                SelectionBackColor = AppColors.Surface,
                SelectionForeColor = AppColors.TextPrimary,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                WrapMode = DataGridViewTriState.True // Memastikan tidak ter-clip jika overflow
            };

            _dgvHistory.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = AppColors.TextSecondary,
                Font = AppFonts.Body,
                Padding = new Padding(15, 10, 15, 10), // Padding dinaikkan
                SelectionBackColor = AppColors.PrimaryLight,
                SelectionForeColor = AppColors.PrimaryDark,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            // Event handler for coloring OK/NG
            _dgvHistory.CellFormatting += DgvHistory_CellFormatting;

            _pnlContent.Controls.Add(_dgvHistory);

            // Status Label
            _lblStatus = new Label
            {
                Text = "Memuat data riwayat...",
                Font = AppFonts.Body,
                ForeColor = AppColors.TextSecondary,
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(_lblStatus);
            // =========================================================
            // [BARU] PANEL BAWAH & TOMBOL TUTUP DI KIRI BAWAH
            // =========================================================
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = this.BackColor
            };

            var btnCloseBottom = new Button
            {
                Text = "TUTUP",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Crimson,
                Size = new Size(120, 40),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, 10) // Jarak 20px dari tepi kiri
            };
            btnCloseBottom.FlatAppearance.BorderSize = 0;
            btnCloseBottom.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(btnCloseBottom);
            
            // Tambahkan ke form
            this.Controls.Add(pnlBottom);
            
            // Pastikan panel ini tidak tertutup oleh grid konten
            pnlBottom.BringToFront(); 
            // =========================================================
        }

        private async System.Threading.Tasks.Task LoadHistoryDataAsync()
        {
            try
            {
                _dgvHistory.Visible = false;
                _lblStatus.Text = "Memuat data riwayat rincian...";
                _lblStatus.Visible = true;

                // Memanggil GetChecksheetHistoryPivotAsync dengan roleTarget (Teknisi / Operator)
                DataTable pivotData = await _repository.GetChecksheetHistoryPivotAsync(_machineId, _templateId, _roleTarget, 30);

                if (pivotData.Rows.Count == 0)
                {
                    _lblStatus.Text = "Tidak ada riwayat patroli dalam 30 hari terakhir.";
                }
                else
                {
                    _dgvHistory.DataSource = pivotData;
                    
                    // Kolom 0 sekarang berisi Nama Item Checksheet (dulu namanya Tanggal)
                    _dgvHistory.Columns[0].HeaderText = "Checksheet Item";
                    _dgvHistory.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                    _dgvHistory.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    _dgvHistory.Columns[0].Frozen = true; // Pin the item column so users can scroll dates

                    // Kolom ke 1 dan seterusnya adalah kolom Tanggal ("dd/MM/yyyy")
                    for (int i = 1; i < _dgvHistory.Columns.Count; i++)
                    {
                        _dgvHistory.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        _dgvHistory.Columns[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        _dgvHistory.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells; 
                    }

                    // Setelah beres render, allow user resize
                    _dgvHistory.AllowUserToResizeColumns = true;

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
            // Kolom 0 adalah Tanggal, jadi skip. Format OK / NG hanya di cell nilai.
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
                else if (status == "N/A")
                {
                    e.CellStyle.ForeColor = Color.DimGray;
                    e.CellStyle.Font = AppFonts.Subtitle;
                }
            }
        }
    }
}
