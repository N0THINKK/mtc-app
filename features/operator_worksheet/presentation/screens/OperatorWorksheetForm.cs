using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.operator_worksheet.services;
using mtc_app.features.operator_worksheet.data.dtos;
using mtc_app.features.operator_worksheet.data.repositories;
using mtc_app.shared.data.session;
using mtc_app.shared.data.dtos;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.components;

namespace mtc_app.features.operator_worksheet.presentation.screens
{
    public partial class OperatorWorksheetForm : Form, mtc_app.features.operator_worksheet.presentation.controllers.IOperatorWorksheetView
    {
        // === Services ===
        private LkoService _lkoService;

        // === Header Info Labels ===
        private Label _lblTanggal;
        private Label _lblNoMesin;
        private ComboBox _cboShift;    // Dropdown shift dari database
        private Label _lblNik;
        private TextBox _txtNoUrut;    // Editable No. Urut
        private string _machinePrefix = ""; // type.area prefix (tanpa urut)
        
        // Qty UI
        private Label _lblGrossQty;
        private ProgressBar _pbGrossQty;
        private Label _lblNetQty;
        private ProgressBar _pbNetQty;

        // === Content panels ===
        private Panel _pnlContent;

        // === Input Produksi fields ===
        private Label _lblLotId;
        private TextBox _txtTerminal;
        private TextBox _txtSeal;
        private TextBox _txtCutL;
        private TextBox _txtFrontChA;
        private TextBox _txtRearChA;
        private TextBox _txtFrontCwA;
        private TextBox _txtRearCwA;
        private Button _btnSisiA;
        private Button _btnSisiB;
        private bool _isSisiA = true;
        private mtc_app.features.operator_worksheet.presentation.components.WireVisualizerPanel _wireVisualizer;

        // === Aktivitas fields ===
        private Label _lblAktivitasTitle;
        private TextBox _txtQtyProduksi;
        private TextBox _txtNo4m;
        private ComboBox _cboKodeDefect;
        private TextBox _txtDefectMesin;
        private TextBox _txtDefectOperator;
        private TextBox _txtLotIdWire;
        private TextBox _txtLotIdTerminalA;
        private TextBox _txtLotIdTerminalB;
        private TextBox _txtIssueKanban;

        // === Sequen list ===
        private DataGridView _dgvSequen;

        // === Auto-Save ===
        private System.Windows.Forms.Timer _autoSaveTimer;
        private bool _isPopulatingFields = false;

        // === Riwayat Produksi ===
        // private DataGridView _dgvRiwayat;
        
        // === Sequen Tersimpan (Kotak Kecil) ===
        private DataGridView _dgvTersimpan;
        private DataGridView _dgvProduct;

        // === Gambar Terminal ===
        private PictureBox _picTerminal;
        private Label _lblImageInfo;

        // === File watcher for auto-reload ===
        private mtc_app.features.operator_worksheet.services.MachineFileWatcherService _fileWatcherService;
        private mtc_app.features.operator_worksheet.presentation.controllers.OperatorWorksheetController _controller;

        // === Loaded data ===
        private List<LkoService.LkoAggregatedData> _worksheetData = new List<LkoService.LkoAggregatedData>();

        // === Active source tracking ===
        private enum ActiveGrid { Sequen, Tersimpan }
        private ActiveGrid _activeSource = ActiveGrid.Sequen;
        private LkoService.LkoAggregatedData _activeRowData = null;

        // === Header data ===
        private string _machineNumber = "-";
        private int? _activeMachineId = null;
        private List<CachedShiftDto> _shifts = new List<CachedShiftDto>();
        private static int _defaultShiftIndex = 0;
        private string _nikOperator = "-";
        private int _qtyDone = 0;
        private int _qtyTarget = 0;

        public OperatorWorksheetForm()
        {
            InitializeComponent();
            _lkoService = new LkoService();
        }

        private async void OperatorWorksheetForm_Load(object sender, EventArgs e)
        {
            // Set default cepat agar UI bisa dirender langsung
            _nikOperator = UserSession.CurrentUser?.Username ?? "-";

            // === LANGKAH 1: Resolve machine number dari LOKAL (instan, tanpa jaringan) ===
            _machineNumber = "-";
            try
            {
                string machineIdStr = DatabaseHelper.GetMachineId(); // dari appsettings.json (lokal)
                if (int.TryParse(machineIdStr, out int machineId))
                {
                    _activeMachineId = machineId;
                    // Baca dari offline cache (SQLite lokal, instan)
                    var offlineRepo = new mtc_app.shared.data.local.OfflineRepository();
                    var cachedMachines = offlineRepo.GetMachinesFromCache();
                    var matched = cachedMachines.FirstOrDefault(m => m.MachineId == machineId);
                    if (matched != null)
                    {
                        string machNum = matched.MachineNumber ?? "";
                        string typeName = matched.MachineType ?? "";
                        string areaName = matched.MachineArea ?? "";
                        _machineNumber = $"{typeName}.{areaName}-{machNum}";
                        _machinePrefix = $"{typeName}.{areaName}";
                    }
                }
            }
            catch { _machineNumber = "-"; }

            // === LANGKAH 1.5: Shift HARDCODE (instan, tanpa query DB) ===
            _shifts = new List<CachedShiftDto>
            {
                new CachedShiftDto { ShiftId = 0, ShiftName = "" },
                new CachedShiftDto { ShiftId = 1, ShiftName = "A1" },
                new CachedShiftDto { ShiftId = 2, ShiftName = "A2" },
                new CachedShiftDto { ShiftId = 3, ShiftName = "B1" },
                new CachedShiftDto { ShiftId = 4, ShiftName = "B2" },
                new CachedShiftDto { ShiftId = 5, ShiftName = "NS" }
            };
            TimeSpan nowTime = DateTime.Now.TimeOfDay;
            // Gunakan shift yang sudah tersimpan sebelumnya (berkat field static)

            InitializeUI(); // Form akan langsung muncul

            // Update label mesin langsung (data sudah ada dari lokal)
            if (_lblNoMesin != null)
            {
                int dashIdx = _machineNumber.LastIndexOf('-');
                if (dashIdx > 0)
                {
                    _machinePrefix = _machineNumber.Substring(0, dashIdx);
                    _lblNoMesin.Text = _machinePrefix;
                    if (_txtNoUrut != null) _txtNoUrut.Text = _machineNumber.Substring(dashIdx + 1);
                }
                else
                {
                    _lblNoMesin.Text = _machineNumber;
                }
            }

            // Pasang shift combo langsung
            if (_cboShift != null)
            {
                _cboShift.DataSource = _shifts;
                _cboShift.DisplayMember = "ShiftName";
                _cboShift.ValueMember = "ShiftId";
                if (_defaultShiftIndex >= 0 && _defaultShiftIndex < _shifts.Count)
                    _cboShift.SelectedIndex = _defaultShiftIndex;
            }

            _controller = new mtc_app.features.operator_worksheet.presentation.controllers.OperatorWorksheetController(this);
            _fileWatcherService = new mtc_app.features.operator_worksheet.services.MachineFileWatcherService(this);
            _fileWatcherService.OnSequenDataChanged += (s, e) => _ = _controller?.LoadAllDataAsync();
            _fileWatcherService.OnProductDataChanged += (s, e) => _ = _controller?.LoadProductDataOnlyAsync();
            _fileWatcherService.StartWatching();

            // === LANGKAH 2: Mulai baca CSV/DAT LANGSUNG (tidak menunggu DB) ===
            _ = _controller?.LoadAllDataAsync();
        }


        /// <summary>
        /// Reconstruct machine number from prefix + current No. Urut textbox value.
        /// </summary>
        public string GetEffectiveMachineNumberInternal()
        {
            string urut = _txtNoUrut?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(urut) || string.IsNullOrEmpty(_machinePrefix))
                return _machineNumber;
            return $"{_machinePrefix}-{urut}";
        }

        /// <summary>
        /// Called when No. Urut value changes - reload sequen data for the new machine number.
        /// </summary>
        private void OnNoUrutChanged()
        {
            // Zero-pad: jika 1 digit, tambahkan 0 di depan (misal "1" -> "01")
            string urut = _txtNoUrut.Text.Trim();
            if (urut.Length == 1 && char.IsDigit(urut[0]))
            {
                _txtNoUrut.Text = urut.PadLeft(2, '0');
            }

            string newMachineNumber = GetEffectiveMachineNumber();
            if (newMachineNumber == _machineNumber) return;

            _machineNumber = newMachineNumber;
            UpdateActiveMachineId();
            _ = _controller?.LoadAllDataAsync();
        }

        private void UpdateActiveMachineId()
        {
            _activeMachineId = null;
            string effMesin = GetEffectiveMachineNumber();
            
            // 1. Coba ambil dari Cache Lokal (Offline)
            try
            {
                var offlineRepo = new mtc_app.shared.data.local.OfflineRepository();
                var machines = offlineRepo.GetMachinesFromCache();
                var matched = machines.FirstOrDefault(m => 
                    string.Equals($"{m.MachineType}.{m.MachineArea}-{m.MachineNumber}", effMesin, StringComparison.OrdinalIgnoreCase)
                );
                if (matched != null) _activeMachineId = matched.MachineId;
            }
            catch { }

            // 2. Fallback: Ambil langsung dari MySQL (Online)
            if (_activeMachineId == null)
            {
                try
                {
                    using (var conn = mtc_app.DatabaseHelper.GetConnection())
                    {
                        string sql = @"
                            SELECT m.machine_id 
                            FROM machines m
                            LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                            LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                            WHERE CONCAT(COALESCE(mt.type_name, ''), '.', COALESCE(ma.area_name, ''), '-', m.machine_number) = @EffMesin
                            LIMIT 1";
                        _activeMachineId = Dapper.SqlMapper.QueryFirstOrDefault<int?>(conn, sql, new { EffMesin = effMesin });
                    }
                }
                catch { }
            }

            // 3. Fallback terakhir: Config ID
            if (_activeMachineId == null)
            {
                if (int.TryParse(mtc_app.DatabaseHelper.GetMachineId(), out int configId))
                {
                    _activeMachineId = configId;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LKO] Updated _activeMachineId for '{effMesin}' -> {_activeMachineId?.ToString() ?? "NULL"}");
        }



        // =====================================================================

        // =====================================================================
        private void DgvSequen_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvSequen?.CurrentRow == null) return;
            var rowData = _dgvSequen.CurrentRow.DataBoundItem as LkoService.LkoAggregatedData;
            if (rowData == null) return;

            bool isSameRow = _activeRowData?.DisplaySequen == rowData.DisplaySequen;
            _activeSource = ActiveGrid.Sequen;
            _activeRowData = rowData;

            _isPopulatingFields = true;
            PopulateInputFields(rowData, isSameRow);
            _isPopulatingFields = false;
            
            _autoSaveTimer?.Stop(); // Jangan auto-save sampai ada interaksi user
        }

        private void DgvProduct_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvProduct?.CurrentRow == null) return;
            var product = _dgvProduct.CurrentRow.DataBoundItem as ProductDto;
            if (product == null) return;

            // Buat wrapper LkoAggregatedData minimal dari ProductDto agar bisa digunakan oleh PopulateInputFields
            var wrapper = new LkoService.LkoAggregatedData
            {
                Log = new PrdLogDto { Sequen = product.Sequen },
                Master = new PrdmstDto
                {
                    Sequen = product.Sequen,
                    CutLength = product.CutLength,
                    TerminalA = product.TerminalA,
                    TerminalB = product.TerminalB,
                    SealA = product.SealA,
                    SealB = product.SealB,
                    KombinasiWire = product.KombinasiWire,
                    HasTerminalA = string.IsNullOrWhiteSpace(product.TerminalA) || product.TerminalA == "-" ? "Y" : "2",
                    HasTerminalB = string.IsNullOrWhiteSpace(product.TerminalB) || product.TerminalB == "-" ? "Y" : "2",
                }
            };
            
            // Jadikan wrapper ini sebagai data aktif (agar jika user klik Simpan, datanya terikat, meski sebenarnya Product blm siap simpan)
            bool isSameRow = _activeRowData?.DisplaySequen == wrapper.DisplaySequen;
            _activeSource = ActiveGrid.Sequen;
            _activeRowData = wrapper;

            _isPopulatingFields = true;
            PopulateInputFields(wrapper, isSameRow); // Ini juga akan otomatis mengupdate _wireVisualizer
            _isPopulatingFields = false;
        }

        private void DgvTersimpan_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvTersimpan?.CurrentRow == null) return;
            var dbRecord = _dgvTersimpan.CurrentRow.DataBoundItem as LkoRecordDto;
            if (dbRecord == null) return;

            // Cari LkoAggregatedData yang cocok di _worksheetData, atau buat wrapper minimal
            var matchingRow = _worksheetData?.FirstOrDefault(x =>
                x.DisplaySequen == dbRecord.Sequen &&
                (x.DisplayUrutanPengerjaan ?? "") == (dbRecord.UrutanKanban ?? ""));

            if (matchingRow != null)
            {
                matchingRow.DbRecord = dbRecord;
                bool isSameRow = _activeRowData?.DisplaySequen == matchingRow.DisplaySequen;
                _activeSource = ActiveGrid.Tersimpan;
                _activeRowData = matchingRow;

                _isPopulatingFields = true;
                PopulateInputFields(matchingRow, isSameRow);
                _isPopulatingFields = false;
            }
            else
            {
                // Jika tidak ada di CSV (misal data lama), buat wrapper minimal dari DB record
                var wrapper = new LkoService.LkoAggregatedData
                {
                    Log = new PrdLogDto { Sequen = dbRecord.Sequen, UrutanPengerjaan = dbRecord.UrutanKanban, WaktuMulaiPengerjaan = dbRecord.WaktuMulai, WaktuSelesaiPengerjaan = dbRecord.WaktuSelesai, QtyProduk = dbRecord.QtyProduct.ToString(), QtyDefect = dbRecord.QtyDefectMesin.ToString() },
                    Master = new PrdmstDto { Sequen = dbRecord.Sequen, KombinasiWire = dbRecord.KombinasiWire, TerminalA = dbRecord.TerminalA, TerminalB = dbRecord.TerminalB, SealA = dbRecord.SealA, SealB = dbRecord.SealB, CutLength = dbRecord.CutLength, Qty = dbRecord.QtyMaster },
                    DbRecord = dbRecord
                };
                bool isSameRow = _activeRowData?.DisplaySequen == wrapper.DisplaySequen;
                _activeSource = ActiveGrid.Tersimpan;
                _activeRowData = wrapper;

                _isPopulatingFields = true;
                PopulateInputFields(wrapper, isSameRow);
                _isPopulatingFields = false;
            }
            
            _autoSaveTimer?.Stop(); // Grid tersimpan tidak punya auto-save
        }

        private void PopulateInputFields(LkoService.LkoAggregatedData rowData, bool skipUserEditableFields = false)
        {
            _lblLotId.Text = $"Sequen: {rowData.DisplaySequen}";
            if (_lblAktivitasTitle != null)
            {
                _lblAktivitasTitle.Text = $"Aktivitas - Sequen {rowData.DisplaySequen} ({rowData.DisplayUrutanPengerjaan})";
            }
            // Show Terminal & Seal based on active side
            if (_isSisiA)
            {
                _txtTerminal.Text = rowData.Master?.TerminalA ?? "";
                _txtSeal.Text = rowData.Master?.SealA ?? "";
            }
            else
            {
                _txtTerminal.Text = rowData.Master?.TerminalB ?? "";
                _txtSeal.Text = rowData.Master?.SealB ?? "";
            }
            if (!skipUserEditableFields)
            {
                _txtFrontChA.Text = "0";
                _txtRearChA.Text = "0";
                _txtFrontCwA.Text = "0";
                _txtRearCwA.Text = "0";
                // Populate Front/Rear from Jissk
                UpdateFrontRearFields(rowData.Jissk);
                _txtNo4m.Text = rowData.DbRecord?.No4m ?? "";
                _txtCutL.Text = rowData.DbRecord != null && !string.IsNullOrEmpty(rowData.DbRecord.CutLength) ? rowData.DbRecord.CutLength : (!string.IsNullOrWhiteSpace(rowData.Master?.CutLength) ? rowData.Master.CutLength : "0");
                _txtLotIdWire.Text = rowData.DbRecord?.LotIdWire ?? "";
                _txtLotIdTerminalA.Text = rowData.DbRecord?.LotIdTerminalA ?? "";
                _txtLotIdTerminalB.Text = rowData.DbRecord?.LotIdTerminalB ?? "";
                _txtIssueKanban.Text = rowData.DbRecord?.IssueKanban ?? "";

                // Load dari DB record jika ada
                _txtDefectOperator.Text = rowData.DbRecord?.QtyDefectOperator.ToString() ?? "0";
                if (rowData.DbRecord != null && !string.IsNullOrEmpty(rowData.DbRecord.KodeDefect))
                {
                    int idx = _cboKodeDefect.Items.IndexOf(rowData.DbRecord.KodeDefect);
                    _cboKodeDefect.SelectedIndex = idx >= 0 ? idx : 0;
                }
                else
                {
                    _cboKodeDefect.SelectedIndex = 0;
                }
            }

            _txtQtyProduksi.Text = rowData.DbRecord != null ? rowData.DbRecord.QtyProduct.ToString() : (rowData.Log?.QtyProduk ?? "");
            _txtDefectMesin.Text = rowData.Log?.QtyDefect ?? "0";

            if (rowData.Master != null)
            {
                _wireVisualizer?.UpdateData(rowData.Master);
                LoadTerminalImage(rowData.Master);
            }
        }

        private void DgvSequen_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var rowData = _dgvSequen.Rows[e.RowIndex].DataBoundItem as LkoService.LkoAggregatedData;
            if (rowData?.DbRecord != null)
            {
                e.CellStyle.BackColor = Color.FromArgb(220, 252, 231);       // hijau muda
                e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);         // hijau tua
                e.CellStyle.SelectionBackColor = Color.FromArgb(187, 247, 208);
                e.CellStyle.SelectionForeColor = Color.FromArgb(22, 101, 52);
            }
        }
        // =====================================================================
        //  TERMINAL IMAGE LOADER
        // =====================================================================
        // Direktori pencarian gambar (fallback berurutan)
        private static readonly string[] ImageDirs = new string[]
        {
            @"C:\AC90HMI\prg\Gambar",
            @"C:\AC90 Master Paper\Gambar",
            @"C:\MTC_System\Gambar"
        };

        /// <summary>
        /// Cari file di beberapa direktori, kembalikan path lengkap yang pertama ditemukan.
        /// </summary>
        private string FindImageFile(string fileName)
        {
            foreach (var dir in ImageDirs)
            {
                string fullPath = Path.Combine(dir, fileName);
                if (File.Exists(fullPath)) return fullPath;
            }
            return null;
        }

        private void LoadTerminalImage(PrdmstDto master)
        {
            if (_picTerminal == null) return;

            try
            {
                // Dispose previous image to free memory
                if (_picTerminal.Image != null)
                {
                    _picTerminal.Image.Dispose();
                    _picTerminal.Image = null;
                }

                // Pick Terminal & Seal based on active side
                string terminal = _isSisiA ? (master.TerminalA?.Trim() ?? "") : (master.TerminalB?.Trim() ?? "");
                string seal = _isSisiA ? (master.SealA?.Trim() ?? "") : (master.SealB?.Trim() ?? "");
                string hasTerminal = _isSisiA ? (master.HasTerminalA?.Trim() ?? "") : (master.HasTerminalB?.Trim() ?? "");
                string kombinasi = master.KombinasiWire?.Trim() ?? "";
                string sisiLabel = _isSisiA ? "A" : "B";

                // Jika indikator HasTerminal bukan "2" -> strip only -> muat Strip.jpg
                if (hasTerminal != "2")
                {
                    string stripFile = "Strip.jpg";
                    string stripPath = FindImageFile(stripFile);
                    if (stripPath != null)
                    {
                        using (var fs = new FileStream(stripPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            _picTerminal.Image = Image.FromStream(fs);
                        }
                        _lblImageInfo.Text = $"Sisi {sisiLabel}: {stripFile} (Strip Only)";
                    }
                    else
                    {
                        _lblImageInfo.Text = $"Sisi {sisiLabel}: Strip Only - {stripFile} tidak ditemukan";
                    }
                    return;
                }

                if (string.IsNullOrEmpty(terminal))
                {
                    _lblImageInfo.Text = $"Tidak ada data Terminal {sisiLabel}";
                    return;
                }

                string kombDash = kombinasi.Replace(" ", "-");

                // Priority 1: Terminal + Seal + Kombinasi
                string fileNameWithSeal = null;
                if (!string.IsNullOrEmpty(seal))
                {
                    fileNameWithSeal = (terminal + seal + kombDash) + ".jpg";
                }

                // Priority 2: Terminal + Kombinasi
                string fileNameNoSeal = (terminal + kombDash) + ".jpg";

                // Try loading with seal first
                string foundPath = null;
                string usedFileName = null;

                if (fileNameWithSeal != null)
                {
                    foundPath = FindImageFile(fileNameWithSeal);
                    usedFileName = fileNameWithSeal;
                }

                if (foundPath == null)
                {
                    foundPath = FindImageFile(fileNameNoSeal);
                    usedFileName = fileNameNoSeal;
                }

                if (foundPath != null)
                {
                    using (var fs = new FileStream(foundPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        _picTerminal.Image = Image.FromStream(fs);
                    }
                    _lblImageInfo.Text = $"Sisi {sisiLabel}: {usedFileName}";
                }
                else
                {
                    string tried = fileNameWithSeal != null
                        ? $"{fileNameWithSeal} / {fileNameNoSeal}"
                        : fileNameNoSeal;
                    _lblImageInfo.Text = $"Gambar tidak ditemukan: {tried}";
                }
            }
            catch (Exception ex)
            {
                _lblImageInfo.Text = $"Error memuat gambar: {ex.Message}";
            }
        }


        // =====================================================================
        //  POPUP: LIHAT SEMUA RECORD TERSIMPAN
        // =====================================================================
        private async void ShowSavedRecordsPopup()
        {
            var popup = new Form
            {
                Text = "Record LKO Tersimpan",
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(
                    Math.Min(Screen.PrimaryScreen.WorkingArea.Width - 60, 1400),
                    Math.Min(Screen.PrimaryScreen.WorkingArea.Height - 60, 800)),
                BackColor = Color.FromArgb(243, 244, 246),
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimizeBox = false,
                MaximizeBox = true,
                KeyPreview = true
            };
            popup.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Escape) popup.Close(); };

            // Title label
            var lblTitle = new Label
            {
                Text = "Record LKO Tersimpan - " + GetEffectiveMachineNumber(),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(16, 12)
            };
            popup.Controls.Add(lblTitle);

            // Close button
            var btnClose = new Button
            {
                Text = "X Tutup",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 36),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Location = new Point(popup.ClientSize.Width - btnClose.Width - 16, 10);
            btnClose.Click += (s, ev) => popup.Close();
            popup.Controls.Add(btnClose);

            // Date Picker for Filtering
            var dtpFilter = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10F),
                Size = new Size(120, 30),
                Location = new Point(btnClose.Left - 136, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            popup.Controls.Add(dtpFilter);
            
            var lblFilter = new Label
            {
                Text = "Tanggal:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(dtpFilter.Left - 66, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            popup.Controls.Add(lblFilter);

            // DataGridView
            var dgv = new DataGridView
            {
                Location = new Point(16, 52),
                Size = new Size(popup.ClientSize.Width - 32, popup.ClientSize.Height - 68),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = new Padding(4)
                },
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 28 },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Padding = new Padding(4),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 23, 42)
                }
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 252) };
            popup.Controls.Add(dgv);

            // Loading indicator
            var lblLoading = new Label
            {
                Text = "Memuat data...",
                Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(popup.ClientSize.Width / 2 - 60, popup.ClientSize.Height / 2)
            };
            popup.Controls.Add(lblLoading);
            lblLoading.BringToFront();

            // Load Data Function
            Action<DateTime> loadDataAction = async (targetDate) =>
            {
                lblLoading.Visible = true;
                lblLoading.Text = "Memuat data...";
                lblLoading.ForeColor = Color.Black;
                dgv.DataSource = null;

                try
                {
                    var records = await Task.Run(async () =>
                    {
                        var repo = new mtc_app.features.operator_worksheet.data.repositories.LkoRepository();
                        return await repo.GetRecordsByDateAsync(GetEffectiveMachineNumber(), targetDate);
                    });

                    lblLoading.Visible = false;

                    if (records == null || records.Count == 0)
                    {
                        lblLoading.Text = "Belum ada record tersimpan pada tanggal tersebut.";
                        lblLoading.Visible = true;
                        lblTitle.Text = $"Record LKO Tersimpan - {GetEffectiveMachineNumber()} (0 record)";
                        return;
                    }

                    // Build DataTable for clean column names
                    var dt = new System.Data.DataTable();
                    dt.Columns.Add("No", typeof(int));
                    dt.Columns.Add("Waktu Mulai", typeof(string));
                    dt.Columns.Add("Waktu Selesai", typeof(string));
                    dt.Columns.Add("Sequen", typeof(string));
                    dt.Columns.Add("Urutan", typeof(string));
                    dt.Columns.Add("Shift", typeof(string));
                    dt.Columns.Add("NIK", typeof(string));
                    dt.Columns.Add("Kombinasi Wire", typeof(string));
                    dt.Columns.Add("Terminal A", typeof(string));
                    dt.Columns.Add("Terminal B", typeof(string));
                    dt.Columns.Add("Seal A", typeof(string));
                    dt.Columns.Add("Seal B", typeof(string));
                    dt.Columns.Add("Qty Master", typeof(string));
                    dt.Columns.Add("CutL", typeof(string));
                    dt.Columns.Add("Qty Produk", typeof(int));
                    dt.Columns.Add("No. 4m", typeof(string));
                    dt.Columns.Add("Defect Mesin", typeof(int));
                    dt.Columns.Add("Defect Operator", typeof(int));
                    dt.Columns.Add("Kode Defect", typeof(string));
                    dt.Columns.Add("Lot Id Wire", typeof(string));
                    dt.Columns.Add("Lot Id Term A", typeof(string));
                    dt.Columns.Add("Lot Id Term B", typeof(string));
                    dt.Columns.Add("Issue Kanban", typeof(string));
                    dt.Columns.Add("Front CH A", typeof(string));
                    dt.Columns.Add("Front CW A", typeof(string));
                    dt.Columns.Add("Rear CH A", typeof(string));
                    dt.Columns.Add("Rear CW A", typeof(string));
                    dt.Columns.Add("Front CH B", typeof(string));
                    dt.Columns.Add("Front CW B", typeof(string));
                    dt.Columns.Add("Rear CH B", typeof(string));
                    dt.Columns.Add("Rear CW B", typeof(string));
                    dt.Columns.Add("Waktu Simpan", typeof(string));

                    int no = 1;
                    // Urutkan berdasarkan Waktu Pengerjaan (WaktuMulai) dari yang terbaru
                    var sortedRecords = records.OrderByDescending(r => r.WaktuMulai).ToList();
                    
                    foreach (var r in sortedRecords)
                    {
                        dt.Rows.Add(
                            no++,
                            r.WaktuMulai, r.WaktuSelesai,
                            r.Sequen, r.UrutanKanban,
                            r.ShiftName, r.Nik,
                            r.KombinasiWire,
                            r.TerminalA, r.TerminalB,
                            r.SealA, r.SealB,
                            r.QtyMaster, r.CutLength,
                            r.QtyProduct, r.No4m, r.QtyDefectMesin, r.QtyDefectOperator,
                            r.KodeDefect,
                            r.LotIdWire, r.LotIdTerminalA, r.LotIdTerminalB,
                            r.IssueKanban,
                            r.FrontChA, r.FrontCwA, r.RearChA, r.RearCwA,
                            r.FrontChB, r.FrontCwB, r.RearChB, r.RearCwB,
                            r.WaktuSimpan.ToString("yyyy-MM-dd HH:mm:ss")
                        );
                    }

                    dgv.DataSource = dt;

                    // Kolom No kecil saja
                    if (dgv.Columns.Contains("No"))
                        dgv.Columns["No"].Width = 40;

                    lblTitle.Text = $"Record LKO Tersimpan - {GetEffectiveMachineNumber()} ({records.Count} record)";
                }
                catch (Exception ex)
                {
                    lblLoading.Text = $"Gagal memuat data: {ex.Message}";
                    lblLoading.ForeColor = Color.FromArgb(220, 38, 38);
                }
            };

            // Event handler for DatePicker
            dtpFilter.ValueChanged += (s, ev) => loadDataAction(dtpFilter.Value);

            popup.Show(this);

            // Load initial data (Today)
            loadDataAction(DateTime.Today);
        }


        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ReleaseCapture();
        }
    }
}
