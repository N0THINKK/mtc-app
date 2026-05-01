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
using mtc_app.shared.data.session;
using mtc_app.shared.data.dtos;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.components;

namespace mtc_app.features.operator_worksheet.presentation.screens
{
    public partial class OperatorWorksheetForm : Form
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
        private TextBox _txtFrontChA;
        private TextBox _txtRearChA;
        private TextBox _txtFrontCwA;
        private TextBox _txtRearCwA;
        private Button _btnSisiA;
        private Button _btnSisiB;
        private bool _isSisiA = true;
        private mtc_app.features.operator_worksheet.presentation.components.WireVisualizerPanel _wireVisualizer;

        // === Aktivitas fields ===
        private TextBox _txtQtyProduksi;
        private ComboBox _cboKodeDefect;
        private TextBox _txtDefectMesin;
        private TextBox _txtDefectOperator;
        private TextBox _txtCutL;
        private TextBox _txtLotIdWire;

        // === Sequen list ===
        private DataGridView _dgvSequen;

        // === Auto-Save ===
        private System.Windows.Forms.Timer _autoSaveTimer;
        private bool _isPopulatingFields = false;

        // === Riwayat Produksi ===
        // private DataGridView _dgvRiwayat;
        
        // === Sequen Tersimpan (Kotak Kecil) ===
        private DataGridView _dgvTersimpan;

        // === Gambar Terminal ===
        private PictureBox _picTerminal;
        private Label _lblImageInfo;

        // === File watcher for auto-reload ===
        private FileSystemWatcher _csvWatcher;
        private System.Windows.Forms.Timer _debounceTimer;

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
        private int _defaultShiftIndex = 0;
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
                new CachedShiftDto { ShiftId = 1, ShiftName = "A1" },
                new CachedShiftDto { ShiftId = 2, ShiftName = "A2" },
                new CachedShiftDto { ShiftId = 3, ShiftName = "B1" },
                new CachedShiftDto { ShiftId = 4, ShiftName = "B2" },
                new CachedShiftDto { ShiftId = 5, ShiftName = "NS" }
            };
            TimeSpan nowTime = DateTime.Now.TimeOfDay;
            // Auto-select: A1 untuk pagi (07:00-19:00), B1 untuk malam (19:00-07:00)
            _defaultShiftIndex = (nowTime >= new TimeSpan(7, 0, 0) && nowTime < new TimeSpan(19, 0, 0)) ? 0 : 2;

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

            SetupFileWatcher();

            // === LANGKAH 2: Mulai baca CSV/DAT LANGSUNG (tidak menunggu DB) ===
            LoadSequenData();
        }


        /// <summary>
        /// Reconstruct machine number from prefix + current No. Urut textbox value.
        /// </summary>
        private string GetEffectiveMachineNumber()
        {
            string urut = _txtNoUrut?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(urut) || string.IsNullOrEmpty(_machinePrefix))
                return _machineNumber;
            return $"{_machinePrefix}-{urut}";
        }

        /// <summary>
        /// Called when No. Urut value changes — reload sequen data for the new machine number.
        /// </summary>
        private void OnNoUrutChanged()
        {
            // Zero-pad: jika 1 digit, tambahkan 0 di depan (misal "1" → "01")
            string urut = _txtNoUrut.Text.Trim();
            if (urut.Length == 1 && char.IsDigit(urut[0]))
            {
                _txtNoUrut.Text = urut.PadLeft(2, '0');
            }

            string newMachineNumber = GetEffectiveMachineNumber();
            if (newMachineNumber == _machineNumber) return;

            _machineNumber = newMachineNumber;
            UpdateActiveMachineId();
            LoadSequenData();
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
        //  FILE WATCHER — auto-reload saat PrdLog/prdmst berubah
        // =====================================================================
        private void SetupFileWatcher()
        {
            string watchDir = @"C:\AC90HMI\prg";
            if (!Directory.Exists(watchDir)) return;

            // Debounce timer: tunggu 500ms setelah perubahan terakhir sebelum reload
            _debounceTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                LoadSequenData();
            };

            _csvWatcher = new FileSystemWatcher(watchDir)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            _csvWatcher.Filter = "*.csv";
            _csvWatcher.Changed += OnCsvFileChanged;
        }

        private void OnCsvFileChanged(object sender, FileSystemEventArgs e)
        {
            string name = e.Name?.ToLower() ?? "";
            if (name == "prdlog.csv" || name == "prdmst.csv")
            {
                // Reset debounce timer (invoke on UI thread)
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(() => { _debounceTimer.Stop(); _debounceTimer.Start(); }));
                else
                    { _debounceTimer.Stop(); _debounceTimer.Start(); }
            }
        }

        // =====================================================================
        //  BUILD UI
        // =====================================================================
        private void InitializeUI()
        {
            this.SuspendLayout();
            this.Controls.Clear();

            this.BackColor = Color.FromArgb(243, 244, 246); // light gray background
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;

            _autoSaveTimer = new System.Windows.Forms.Timer { Interval = 20000 };
            _autoSaveTimer.Tick += async (s, e) =>
            {
                _autoSaveTimer.Stop();
                if (_activeRowData == null || _activeSource != ActiveGrid.Sequen) return;
                
                bool success = await PerformSaveAsync(isAutoSave: true);
                if (success && _dgvSequen.CurrentRow != null)
                {
                    int nextIdx = _dgvSequen.CurrentRow.Index + 1;
                    if (nextIdx < _dgvSequen.RowCount)
                    {
                        _dgvSequen.CurrentCell = _dgvSequen.Rows[nextIdx].Cells[0];
                    }
                }
            };

            // ---- 1) TITLE BAR ----
            var pnlTitleBar = CreateTitleBar();
            this.Controls.Add(pnlTitleBar);

            // ---- 2) INFO HEADER ----
            var pnlInfoHeader = CreateInfoHeader();
            pnlInfoHeader.Top = pnlTitleBar.Bottom;
            this.Controls.Add(pnlInfoHeader);

            // ---- 2.5) WIRE VISUALIZER (di bawah header, tengah layar) ----
            int vizWidth = (int)(this.ClientSize.Width * 0.94);
            _wireVisualizer = new mtc_app.features.operator_worksheet.presentation.components.WireVisualizerPanel
            {
                Left = (this.ClientSize.Width - vizWidth) / 2,
                Top = pnlInfoHeader.Bottom + 4,
                Width = vizWidth,
                Height = 65,
                BackColor = Color.White
            };
            _wireVisualizer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(_wireVisualizer);

            // ---- 3) CONTENT AREA ----
            _pnlContent = new Panel
            {
                Top = _wireVisualizer.Bottom,
                Left = 0,
                Width = this.ClientSize.Width,
                Height = this.ClientSize.Height - _wireVisualizer.Bottom,
                BackColor = Color.FromArgb(243, 244, 246),
                AutoScroll = true,
                Padding = new Padding(12, 10, 12, 10)
            };
            _pnlContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Calculate column widths for 2-column layout
            int contentW = this.ClientSize.Width - 36; // minus padding
            int gap = 8;
            int colW = (contentW - gap) / 2;
            int topRowHeight = 440;
            int bottomRowHeight = 440;

            // === ROW 1 LEFT: Sequen ===
            var pnlSequen = CreateSequenPanel(colW, topRowHeight);
            pnlSequen.Location = new Point(12, 10);
            _pnlContent.Controls.Add(pnlSequen);

            // === ROW 1 RIGHT: Aktivitas ===
            var pnlAktivitas = CreateAktivitasPanel(colW, topRowHeight);
            pnlAktivitas.Location = new Point(pnlSequen.Right + gap, 10);
            _pnlContent.Controls.Add(pnlAktivitas);

            // === ROW 2 LEFT: Tersimpan ===
            var pnlTersimpan = CreateTersimpanPanel(colW, bottomRowHeight);
            pnlTersimpan.Location = new Point(12, pnlSequen.Bottom + gap);
            _pnlContent.Controls.Add(pnlTersimpan);

            // === ROW 2 RIGHT: Input Produksi ===
            var pnlInputProduksi = CreateInputProduksiPanel(colW, bottomRowHeight);
            pnlInputProduksi.Location = new Point(pnlTersimpan.Right + gap, pnlAktivitas.Bottom + gap);
            _pnlContent.Controls.Add(pnlInputProduksi);

            // === BOTTOM: Gambar Terminal ===
            int imageAreaTop = pnlTersimpan.Bottom > pnlInputProduksi.Bottom ? pnlTersimpan.Bottom + gap : pnlInputProduksi.Bottom + gap;
            int imageAreaWidth = contentW + gap;
            int imageAreaHeight = 450;

            var pnlGambar = new Panel
            {
                Location = new Point(16, imageAreaTop),
                Size = new Size(imageAreaWidth, imageAreaHeight),
                BackColor = Color.White,
                Padding = new Padding(8)
            };
            pnlGambar.Paint += (s, ev) =>
            {
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1)) // slate-200 border
                    ev.Graphics.DrawRectangle(pen, 0, 0, pnlGambar.Width - 1, pnlGambar.Height - 1);
            };

            var lblGambarTitle = new Label
            {
                Text = "\uD83D\uDCF7  Gambar Terminal",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            pnlGambar.Controls.Add(lblGambarTitle);

            _lblImageInfo = new Label
            {
                Text = "Pilih sequen untuk melihat gambar terminal",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(12, 30)
            };
            pnlGambar.Controls.Add(_lblImageInfo);

            _picTerminal = new PictureBox
            {
                Location = new Point(12, 50),
                Size = new Size(imageAreaWidth - 32, imageAreaHeight - 60),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            _picTerminal.Click += (s, ev) =>
            {
                if (_picTerminal.Image == null) return;
                ShowZoomableImage(_picTerminal.Image, _lblImageInfo.Text);
            };
            pnlGambar.Controls.Add(_picTerminal);
            _pnlContent.Controls.Add(pnlGambar);

            this.Controls.Add(_pnlContent);

            this.ResumeLayout(true);
            
            // LoadSequenData() dipindahkan ke Form_Load setelah LoadHeaderData selesai
        }

        // =====================================================================
        //  TITLE BAR  (Dark navy bar with title + Close button)
        // =====================================================================
        private Panel CreateTitleBar()
        {
            // Colors matching the reference screenshot
            Color titleBgColor = Color.FromArgb(30, 41, 59);    // slate-800
            Color titleAccent = Color.FromArgb(56, 189, 248);   // sky-400 accent for left bar

            var pnl = new Panel
            {
                Left = 0,
                Top = 0,
                Width = this.ClientSize.Width,
                Height = 52,
                BackColor = titleBgColor
            };
            pnl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Accent stripe on the left
            var pnlAccent = new Panel
            {
                Left = 0, Top = 0,
                Width = 5, Height = pnl.Height,
                BackColor = titleAccent
            };
            pnl.Controls.Add(pnlAccent);

            // Title text  
            var lblTitle = new Label
            {
                Text = "LEMBAR KERJA OPERATOR",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(24, 12)
            };
            pnl.Controls.Add(lblTitle);

            // "Keluar" button
            var btnKeluar = new Button
            {
                Text = "Keluar",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(85, 34),
                Cursor = Cursors.Hand
            };
            btnKeluar.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225); // slate-300
            btnKeluar.FlatAppearance.BorderSize = 1;
            btnKeluar.Location = new Point(pnl.Width - btnKeluar.Width - 16, (pnl.Height - btnKeluar.Height) / 2);
            btnKeluar.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Rounded corners
            btnKeluar.Paint += (s, e) =>
            {
                var btn = (Button)s;
                int radius = 12;
                var rect = new Rectangle(0, 0, btn.Width, btn.Height);
                var path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                btn.Region = new Region(path);
            };

            btnKeluar.Click += (s, e) => this.Close();

            pnl.Controls.Add(btnKeluar);

            // Allow dragging by title bar
            lblTitle.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    // Windows API for drag
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            pnl.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };

            return pnl;
        }

        // =====================================================================
        //  INFO HEADER  (Tanggal, No Mesin, Shift, NIK, QTY bar)
        // =====================================================================
        private Panel CreateInfoHeader()
        {
            var pnl = new Panel
            {
                Left = 0,
                Width = this.ClientSize.Width,
                Height = 75, // Taller for 2 rows
                BackColor = Color.White
            };
            pnl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Bottom separator line
            pnl.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1)) // slate-200
                {
                    e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
                }
            };

            int leftX = 20;
            int labelY = 12;
            Color labelColor = Color.FromArgb(100, 116, 139);  // slate-500
            Color valueColor = Color.FromArgb(15, 23, 42);     // slate-900
            Font labelFont = new Font("Segoe UI", 10F);
            Font valueFont = new Font("Segoe UI", 10F, FontStyle.Bold);

            // Row 1
            int row1Y = 10;
            int col1X = 20;
            int col2X = 200;
            int col3X = 400;

            // ---- Deteksi No Urut dari No Mesin ----
            string displayMesin = _machineNumber;
            string displayUrut = "-";
            int dashIdx = _machineNumber.LastIndexOf('-');
            if (dashIdx > 0)
            {
                _machinePrefix = _machineNumber.Substring(0, dashIdx);
                displayMesin = _machinePrefix;
                displayUrut = _machineNumber.Substring(dashIdx + 1);
            }
            else
            {
                _machinePrefix = _machineNumber;
            }

            // ---- Tanggal ----
            var lblTanggalLabel = new Label { Text = "Tanggal :", Font = labelFont, ForeColor = labelColor, AutoSize = true, Location = new Point(col1X, row1Y), BackColor = Color.Transparent };
            pnl.Controls.Add(lblTanggalLabel);

            _lblTanggal = new Label { Text = DateTime.Now.ToString("yyyy/MM/dd"), Font = valueFont, ForeColor = valueColor, AutoSize = true, Location = new Point(lblTanggalLabel.Right + 4, row1Y), BackColor = Color.Transparent };
            pnl.Controls.Add(_lblTanggal);

            // ---- No Mesin ----
            var lblMesinLabel = new Label { Text = "Mesin :", Font = labelFont, ForeColor = labelColor, AutoSize = true, Location = new Point(col2X, row1Y), BackColor = Color.Transparent };
            pnl.Controls.Add(lblMesinLabel);

            _lblNoMesin = new Label { Text = displayMesin, Font = valueFont, ForeColor = valueColor, AutoSize = true, Location = new Point(lblMesinLabel.Right + 4, row1Y), BackColor = Color.Transparent };
            pnl.Controls.Add(_lblNoMesin);

            // ---- No Urut ----
            var lblUrutLabel = new Label { Text = "Urut :", Font = labelFont, ForeColor = labelColor, AutoSize = true, Location = new Point(col3X, row1Y), BackColor = Color.Transparent };
            pnl.Controls.Add(lblUrutLabel);

            _txtNoUrut = new TextBox { Text = displayUrut, Font = valueFont, ForeColor = valueColor, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Size = new Size(60, 24), Location = new Point(lblUrutLabel.Right + 4, row1Y - 2) };
            _txtNoUrut.Leave += (s, ev) => OnNoUrutChanged();
            _txtNoUrut.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Enter) { OnNoUrutChanged(); ev.SuppressKeyPress = true; } };
            pnl.Controls.Add(_txtNoUrut);

            // Row 2
            int row2Y = 40;
            int rightEdge = pnl.Width - 20;

            // ---- Shift (ComboBox) ----
            var lblShiftLabel = new Label { Text = "Shift :", Font = labelFont, ForeColor = labelColor, AutoSize = true, Location = new Point(col1X, row2Y), BackColor = Color.Transparent };
            pnl.Controls.Add(lblShiftLabel);

            _cboShift = new ComboBox { Font = valueFont, ForeColor = valueColor, BackColor = Color.White, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Size = new Size(110, 28), Location = new Point(lblShiftLabel.Right + 4, row2Y - 4), Cursor = Cursors.Hand };
            _cboShift.DisplayMember = "ShiftName";
            _cboShift.ValueMember = "ShiftId";
            if (_shifts != null && _shifts.Count > 0)
            {
                _cboShift.DataSource = _shifts;
                try { if (_defaultShiftIndex >= 0 && _defaultShiftIndex < _cboShift.Items.Count) _cboShift.SelectedIndex = _defaultShiftIndex; } catch { }
            }
            pnl.Controls.Add(_cboShift);

            // ---- NIK ----
            var lblNikLabel = new Label { Text = "NIK :", Font = labelFont, ForeColor = labelColor, AutoSize = true, Location = new Point(col2X, row2Y), BackColor = Color.Transparent };
            pnl.Controls.Add(lblNikLabel);

            _lblNik = new Label { Text = _nikOperator, Font = valueFont, ForeColor = valueColor, AutoSize = true, Location = new Point(lblNikLabel.Right + 4, row2Y), BackColor = Color.Transparent };
            pnl.Controls.Add(_lblNik);

            // ---- QTY section (right-aligned, row 2) ----
            _pbNetQty = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Size = new Size(50, 16), BackColor = Color.FromArgb(226, 232, 240), Style = ProgressBarStyle.Continuous };
            _pbNetQty.Location = new Point(rightEdge - _pbNetQty.Width, row2Y);
            _pbNetQty.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnl.Controls.Add(_pbNetQty);

            _lblNetQty = new Label { Text = "OK: - / -", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), AutoSize = true, BackColor = Color.Transparent };
            _lblNetQty.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnl.Controls.Add(_lblNetQty);

            _pbGrossQty = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Size = new Size(50, 16), BackColor = Color.FromArgb(226, 232, 240), Style = ProgressBarStyle.Continuous };
            _pbGrossQty.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnl.Controls.Add(_pbGrossQty);

            _lblGrossQty = new Label { Text = "Gross: - / -", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = valueColor, AutoSize = true, BackColor = Color.Transparent };
            _lblGrossQty.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnl.Controls.Add(_lblGrossQty);
            
            // Initial positions
            _lblNetQty.Location = new Point(_pbNetQty.Left - 70, row2Y);
            _pbGrossQty.Location = new Point(_lblNetQty.Left - 60, row2Y);
            _lblGrossQty.Location = new Point(_pbGrossQty.Left - 80, row2Y);

            return pnl;
        }

        // =====================================================================
        //  SHARED HELPERS
        // =====================================================================
        private Color CardBg => Color.White;
        private Color SlBorder => Color.FromArgb(226, 232, 240);
        private Font SectionFont => new Font("Segoe UI", 12F, FontStyle.Bold);
        private Font FieldLabelFont => new Font("Segoe UI", 9.5F);
        private Font FieldValueFont => new Font("Segoe UI", 10F);

        private Panel CreateCard(int width, int height)
        {
            var card = new Panel
            {
                Size = new Size(width, height),
                BackColor = CardBg,
                Padding = new Padding(16),
            };
            card.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using (var pen = new Pen(SlBorder, 1))
                {
                    var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                    int r = 10;
                    var path = new GraphicsPath();
                    path.AddArc(rect.X, rect.Y, r, r, 180, 90);
                    path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                    path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                    path.CloseFigure();
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            };
            return card;
        }

        private TextBox CreateStyledTextBox(string placeholder, int width = 200)
        {
            return new TextBox
            {
                Font = FieldValueFont,
                Width = width,
                Height = 32,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = placeholder
            };
        }

        // =====================================================================
        //  LEFT PANEL: Input Produksi
        // =====================================================================
        private Panel CreateInputProduksiPanel(int width, int height)
        {
            var card = CreateCard(width, height);
            var lblH = new Label { Text = "Input Produksi", Font = SectionFont, ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true, Location = new Point(16, 14) };
            card.Controls.Add(lblH);

            int y = 44;
            int fw = width - 40;

            // SISI A / SISI B toggle
            _btnSisiA = new Button { Text = "SISI A", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Size = new Size(fw / 2, 32), Location = new Point(16, y), FlatStyle = FlatStyle.Flat, BackColor = AppColors.Primary, ForeColor = Color.White, Cursor = Cursors.Hand };
            _btnSisiA.FlatAppearance.BorderSize = 0;
            _btnSisiA.Click += (s, e) => ToggleSisi(true);
            card.Controls.Add(_btnSisiA);

            _btnSisiB = new Button { Text = "SISI B", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Size = new Size(fw / 2, 32), Location = new Point(_btnSisiA.Right + 2, y), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(71, 85, 105), Cursor = Cursors.Hand };
            _btnSisiB.FlatAppearance.BorderSize = 1;
            _btnSisiB.FlatAppearance.BorderColor = SlBorder;
            _btnSisiB.Click += (s, e) => ToggleSisi(false);
            card.Controls.Add(_btnSisiB);
            y += 44;

            _lblLotId = new Label { Text = "Lot ID: -", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true, Location = new Point(16, y) };
            card.Controls.Add(_lblLotId);
            y += 28;

            // Terminal & Seal side by side
            int halfFw = (fw - 8) / 2;
            card.Controls.Add(new Label { Text = "Terminal", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            card.Controls.Add(new Label { Text = "Seal", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16 + halfFw + 8, y) });
            y += 20;

            _txtTerminal = CreateStyledTextBox("Terminal", halfFw);
            _txtTerminal.Location = new Point(16, y);
            _txtTerminal.ReadOnly = true;
            card.Controls.Add(_txtTerminal);

            _txtSeal = CreateStyledTextBox("Seal", halfFw);
            _txtSeal.Location = new Point(_txtTerminal.Right + 8, y);
            _txtSeal.ReadOnly = true;
            card.Controls.Add(_txtSeal);
            y += 40;

            y += 40;

            int labelWidth = 80;
            int inputWidth = fw - labelWidth - 8;

            card.Controls.Add(new Label { Text = "Front C/H", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), Size = new Size(labelWidth, 32), TextAlign = ContentAlignment.MiddleLeft, Location = new Point(16, y) });
            _txtFrontChA = CreateStyledTextBox("Masukkan Front C/H", inputWidth);
            _txtFrontChA.Location = new Point(16 + labelWidth + 8, y);
            _txtFrontChA.ReadOnly = true;
            _txtFrontChA.Text = "0";
            card.Controls.Add(_txtFrontChA);
            y += 36;

            card.Controls.Add(new Label { Text = "Rear C/H", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), Size = new Size(labelWidth, 32), TextAlign = ContentAlignment.MiddleLeft, Location = new Point(16, y) });
            _txtRearChA = CreateStyledTextBox("Masukkan Rear C/H", inputWidth);
            _txtRearChA.Location = new Point(16 + labelWidth + 8, y);
            _txtRearChA.ReadOnly = true;
            _txtRearChA.Text = "0";
            _txtRearChA.BackColor = Color.FromArgb(241, 245, 249);
            card.Controls.Add(_txtRearChA);
            y += 36;

            card.Controls.Add(new Label { Text = "Front C/W", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), Size = new Size(labelWidth, 32), TextAlign = ContentAlignment.MiddleLeft, Location = new Point(16, y) });
            _txtFrontCwA = CreateStyledTextBox("Masukkan Front C/W", inputWidth);
            _txtFrontCwA.Location = new Point(16 + labelWidth + 8, y);
            _txtFrontCwA.ReadOnly = true;
            _txtFrontCwA.Text = "0";
            card.Controls.Add(_txtFrontCwA);
            y += 36;

            card.Controls.Add(new Label { Text = "Rear C/W", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), Size = new Size(labelWidth, 32), TextAlign = ContentAlignment.MiddleLeft, Location = new Point(16, y) });
            _txtRearCwA = CreateStyledTextBox("Masukkan Rear C/W", inputWidth);
            _txtRearCwA.Location = new Point(16 + labelWidth + 8, y);
            _txtRearCwA.ReadOnly = true;
            _txtRearCwA.Text = "0";
            _txtRearCwA.BackColor = Color.FromArgb(241, 245, 249);
            card.Controls.Add(_txtRearCwA);
            y += 44;

            return card;
        }

        private void ToggleSisi(bool isSisiA)
        {
            _isSisiA = isSisiA;
            _btnSisiA.BackColor = isSisiA ? AppColors.Primary : Color.FromArgb(241, 245, 249);
            _btnSisiA.ForeColor = isSisiA ? Color.White : Color.FromArgb(71, 85, 105);
            _btnSisiB.BackColor = !isSisiA ? AppColors.Primary : Color.FromArgb(241, 245, 249);
            _btnSisiB.ForeColor = !isSisiA ? Color.White : Color.FromArgb(71, 85, 105);

            // Update Terminal & Seal fields based on active side
            if (_activeRowData?.Master != null)
            {
                var master = _activeRowData.Master;
                _txtTerminal.Text = isSisiA ? (master.TerminalA ?? "") : (master.TerminalB ?? "");
                _txtSeal.Text = isSisiA ? (master.SealA ?? "") : (master.SealB ?? "");
                LoadTerminalImage(master);
            }

            // Update Front/Rear from Jissk based on active side
            UpdateFrontRearFields(_activeRowData?.Jissk);
        }

        private void UpdateFrontRearFields(mtc_app.features.operator_worksheet.data.dtos.JisskDto jissk)
        {
            if (jissk == null)
            {
                _txtFrontChA.Text = "0";
                _txtFrontCwA.Text = "0";
                _txtRearChA.Text = "0";
                _txtRearCwA.Text = "0";
                return;
            }

            if (_isSisiA)
            {
                _txtFrontChA.Text = jissk.FrontChA;
                _txtFrontCwA.Text = jissk.FrontCwA;
                _txtRearChA.Text = jissk.RearChA;
                _txtRearCwA.Text = jissk.RearCwA;
            }
            else
            {
                _txtFrontChA.Text = jissk.FrontChB;
                _txtFrontCwA.Text = jissk.FrontCwB;
                _txtRearChA.Text = jissk.RearChB;
                _txtRearCwA.Text = jissk.RearCwB;
            }
        }

        // =====================================================================
        //  MIDDLE PANEL: Aktivitas
        // =====================================================================
        private Panel CreateAktivitasPanel(int width, int height)
        {
            var card = CreateCard(width, height);
            card.Controls.Add(new Label { Text = "Aktivitas", Font = SectionFont, ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true, Location = new Point(16, 14) });

            int y = 44;
            int fw = width - 40;
            int halfW = (fw - 8) / 2;
            int thirdW = (fw - 16) / 3;

            // Lot Id Wire & CutL side by side
            card.Controls.Add(new Label { Text = "Lot Id Wire", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            card.Controls.Add(new Label { Text = "CutL", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16 + halfW + 8, y) });
            y += 20;

            _txtLotIdWire = CreateStyledTextBox("Lot Id Wire", halfW);
            _txtLotIdWire.Location = new Point(16, y);
            _txtLotIdWire.TextChanged += (s, e) => ResetAutoSaveTimer();
            card.Controls.Add(_txtLotIdWire);

            _txtCutL = CreateStyledTextBox("0", halfW);
            _txtCutL.Location = new Point(_txtLotIdWire.Right + 8, y);
            _txtCutL.TextChanged += (s, e) => ResetAutoSaveTimer();
            card.Controls.Add(_txtCutL);
            y += 42;

            // QTY Produksi
            card.Controls.Add(new Label { Text = "QTY Produksi", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            y += 20;
            _txtQtyProduksi = CreateStyledTextBox("Masukkan Qty Produksi", fw);
            _txtQtyProduksi.Location = new Point(16, y);
            _txtQtyProduksi.TextChanged += (s, e) => ResetAutoSaveTimer();
            card.Controls.Add(_txtQtyProduksi);
            y += 42;

            // Kode Defect (dropdown diisi user)
            card.Controls.Add(new Label { Text = "Kode Defect", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            y += 20;
            _cboKodeDefect = new ComboBox { Font = FieldValueFont, Size = new Size(fw, 30), Location = new Point(16, y), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(248, 250, 252) };
            _cboKodeDefect.SelectedIndexChanged += (s, e) => ResetAutoSaveTimer();
            _cboKodeDefect.Items.AddRange(new object[] {
                "- Pilih Kode Defect -",
                "A.1 Core Terurai",
                "A.2 Core Terpotong",
                "A.3 Core Tidak teratur",
                "A.4 Core Maju",
                "A.5 Core Mundur",
                "A.6 Tidak Tercrimping",
                "A.7 Scracth",
                "B.1 Terminal Tergores",
                "B.2 Terminal Bengkok ke atas",
                "B.3 Terminal Bengkok ke bawah",
                "B.4 Terminal Melintir",
                "B.5 Terminal Ujung Terpotong",
                "B.6 Terminal Ujung Terbuka",
                "B.7 Terminal Ujung Rusak",
                "B.8 Terminal Bridge terlalu panjang",
                "B.9 Terminal Centilever Rusak",
                "B.10 Terminal Lepas dari Circuit",
                "C.1 Front C/H terlalu tinggi",
                "C.2 Front C/H terlalu rendah",
                "C.3 Front C/W terlalu tinggi",
                "C.4 Front C/W terlalu rendah",
                "C.5 Front Flash",
                "D.1 Rear C/H terlalu tinggi",
                "D.2 Rear C/H terlalu rendah",
                "D.3 Rear C/W terlalu tinggi",
                "D.4 Rear C/W terlalu rendah",
                "D.5 Rear ada di dalam Insulasi",
                "D.6 Tidak Standart",
                "E.1 Insulation Tercrimping",
                "E.2 Insulation Terlalu mundur",
                "E.3 Insulation Rusak",
                "E.4 Insulation Tidak rata",
                "F.1 Seal Terpotong",
                "F.2 Seal Terbalik",
                "F.3 Seal Terlalu mundur",
                "F.4 Seal Terlalu maju",
                "F.5 Seal Tercrimping",
                "F.6 Seal Tidak ada",
                "F.7 Seal Sobek",
                "G.1 Crimping Ada Benda Asing",
                "G.2 Crimping Ada 2 Terminal Tercrimping",
                "G.3 Crimping Tanpa Core",
                "G.4 Crimping Tanpa Stripping",
                "H.1 Lance Rusak",
                "H.2 Stabilizer Rusak",
                "H.3 Bellmouth Tidak Standart",
                "H.4 Kondisi core bagian A",
                "H.5 Resin masuk bagian A",
                "H.6 Resin barel bagian B Terbuka",
                "H.7 Core terlihat atas sisi C",
                "H.8 Core terlihat samping sisi C",
                "H.9 Sisi punggung",
                "H.10 Abnormal resin",
                "H.11 Panjang welding N-OK",
                "H.12 Circuit tidak terbonder",
                "H.13 Bonder Retak",
                "H.14 Stripping kepanjangan"
            });
            _cboKodeDefect.SelectedIndex = 0;
            card.Controls.Add(_cboKodeDefect);
            y += 42;

            // Defect Mesin (ReadOnly) + Defect Operator (Editable) side by side
            card.Controls.Add(new Label { Text = "Defect Mesin (otomatis)", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            card.Controls.Add(new Label { Text = "Defect Operator", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16 + halfW + 8, y) });
            y += 20;

            _txtDefectMesin = CreateStyledTextBox("0", halfW);
            _txtDefectMesin.Location = new Point(16, y);
            _txtDefectMesin.Text = "0";
            _txtDefectMesin.ReadOnly = true;
            _txtDefectMesin.BackColor = Color.FromArgb(241, 245, 249);
            card.Controls.Add(_txtDefectMesin);

            _txtDefectOperator = CreateStyledTextBox("Masukkan jumlah defect", halfW);
            _txtDefectOperator.Location = new Point(_txtDefectMesin.Right + 8, y);
            _txtDefectOperator.Text = "0";
            _txtDefectOperator.TextChanged += (s, e) => ResetAutoSaveTimer();
            card.Controls.Add(_txtDefectOperator);
            y += 48;

            // Button Simpan saja
            var btnSave = new Button { Text = "\u2713 Simpan", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Size = new Size(fw, 40), Location = new Point(16, y), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSimpanAktivitas_Click;
            card.Controls.Add(btnSave);

            return card;
        }

        private async void BtnSimpanAktivitas_Click(object sender, EventArgs e)
        {
            await PerformSaveAsync(isAutoSave: false);
        }

        private void ResetAutoSaveTimer()
        {
            if (_isPopulatingFields) return;
            if (_activeRowData != null && _activeSource == ActiveGrid.Sequen)
            {
                _autoSaveTimer.Stop();
                _autoSaveTimer.Start();
            }
        }

        private async Task<bool> PerformSaveAsync(bool isAutoSave)
        {
            if (_activeRowData == null)
            {
                if (!isAutoSave) MessageBox.Show("Pilih sequen terlebih dahulu.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var rowData = _activeRowData;

            // Cegah simpan ulang dari grid SEQUEN jika sudah tersimpan
            if (_activeSource == ActiveGrid.Sequen && rowData.DbRecord != null)
            {
                if (!isAutoSave) MessageBox.Show("Sequen ini sudah tersimpan. Gunakan tabel 'Sudah Tersimpan' untuk mengedit.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Ambil kode defect dari dropdown
            string kodeDefect = _cboKodeDefect.SelectedIndex > 0 ? _cboKodeDefect.SelectedItem.ToString() : string.Empty;

            // Parse defect operator
            int.TryParse(_txtDefectOperator.Text.Trim(), out int defectOperator);
            int.TryParse(_txtQtyProduksi.Text.Trim(), out int qtyProduct);

            // Validasi: jika defect operator > 0, wajib pilih kode defect
            if (defectOperator > 0 && string.IsNullOrEmpty(kodeDefect))
            {
                if (!isAutoSave) 
                {
                    MessageBox.Show("Jika ada Defect Operator, wajib memilih Kode Defect.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _cboKodeDefect.Focus();
                }
                return false;
            }

            // Ambil shift dari combo
            string shiftName = _cboShift?.SelectedItem is CachedShiftDto shift ? shift.ShiftName : "-";

            int.TryParse(rowData.Log?.QtyDefect ?? "0", out int defectMesin);

            var record = new mtc_app.features.operator_worksheet.data.dtos.LkoRecordDto
            {
                WaktuSimpan = DateTime.Now,
                NoMesin = GetEffectiveMachineNumber(),
                IdMesin = _activeMachineId,
                ShiftName = shiftName,
                Nik = _nikOperator,
                Sequen = rowData.DisplaySequen,
                UrutanKanban = rowData.DisplayUrutanPengerjaan,
                QtyProduct = qtyProduct,
                QtyDefectMesin = defectMesin,
                QtyDefectOperator = defectOperator,
                KodeDefect = kodeDefect,
                LotIdWire = _txtLotIdWire.Text.Trim(),
                CutLength = _txtCutL.Text.Trim(),

                // Master data
                KombinasiWire = rowData.Master?.KombinasiWire ?? "",
                TerminalA = rowData.Master?.TerminalA ?? "",
                TerminalB = rowData.Master?.TerminalB ?? "",
                SealA = rowData.Master?.SealA ?? "",
                SealB = rowData.Master?.SealB ?? "",
                QtyMaster = rowData.Master?.Qty ?? "",

                // Jissk data
                FrontChA = rowData.Jissk?.FrontChA ?? "0",
                FrontCwA = rowData.Jissk?.FrontCwA ?? "0",
                RearChA = rowData.Jissk?.RearChA ?? "0",
                RearCwA = rowData.Jissk?.RearCwA ?? "0",
                FrontChB = rowData.Jissk?.FrontChB ?? "0",
                FrontCwB = rowData.Jissk?.FrontCwB ?? "0",
                RearChB = rowData.Jissk?.RearChB ?? "0",
                RearCwB = rowData.Jissk?.RearCwB ?? "0",

                // Waktu mesin
                WaktuMulai = rowData.Log?.WaktuMulaiPengerjaan ?? "",
                WaktuSelesai = rowData.Log?.WaktuSelesaiPengerjaan ?? ""
            };

            try
            {
                bool savedOnline = await _lkoService.SaveToDatabase(record);

                // Update DB record reference di data lokal
                rowData.DbRecord = record;
                rowData.IsOffline = !savedOnline;

                // Refresh grid agar langsung muncul
                var savedData = _worksheetData.Where(x => x.DbRecord != null).ToList();
                _dgvTersimpan.DataSource = null;
                _dgvTersimpan.DataSource = savedData;
                _dgvSequen.Refresh();

                if (!isAutoSave)
                {
                    string aksi = _activeSource == ActiveGrid.Tersimpan ? "diperbarui" : "disimpan";
                    if (savedOnline)
                    {
                        MessageBox.Show($"Data berhasil {aksi}.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Server tidak tersedia. Data {aksi} secara offline dan akan di-sync otomatis saat koneksi kembali.", "Tersimpan Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                
                UpdateHeaderQty();
                return true;
            }
            catch (Exception ex)
            {
                if (!isAutoSave) MessageBox.Show("Gagal menyimpan: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // =====================================================================
        //  RIGHT PANEL: Sequen
        // =====================================================================
        private Panel CreateSequenPanel(int width, int height)
        {
            var card = CreateCard(width, height);
            card.Controls.Add(new Label { Text = "SEQUEN", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true, Location = new Point(16, 14) });

            int y = 40;
            int fw = width - 36;
            var txtSearch = CreateStyledTextBox("\uD83D\uDD0D Cari...", fw);
            txtSearch.Location = new Point(16, y);
            card.Controls.Add(txtSearch);
            y += 38;

            // Tombol Simpan Terpilih
            var btnSimpanTerpilih = new Button
            {
                Text = "\u2713 Simpan Terpilih",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(fw / 2 - 4, 34),
                Location = new Point(16, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSimpanTerpilih.FlatAppearance.BorderSize = 0;
            btnSimpanTerpilih.Click += BtnSimpanTerpilih_Click;
            card.Controls.Add(btnSimpanTerpilih);

            // Tombol Pilih Semua / Batal Pilih
            var btnPilihSemua = new Button
            {
                Text = "Pilih Semua",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(fw / 2 - 4, 34),
                Location = new Point(16 + fw / 2 + 4, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnPilihSemua.FlatAppearance.BorderSize = 0;
            btnPilihSemua.Click += (s, ev) =>
            {
                if (_dgvSequen.Rows.Count == 0) return;
                // Toggle: jika sudah ada yang dicentang semua, batal semua
                bool allChecked = true;
                foreach (DataGridViewRow r in _dgvSequen.Rows)
                {
                    if (r.Cells["Pilih"].Value == null || !(bool)r.Cells["Pilih"].Value) { allChecked = false; break; }
                }
                foreach (DataGridViewRow r in _dgvSequen.Rows)
                {
                    r.Cells["Pilih"].Value = !allChecked;
                }
                btnPilihSemua.Text = allChecked ? "Pilih Semua" : "Batal Pilih";
                _dgvSequen.RefreshEdit();
            };
            card.Controls.Add(btnPilihSemua);
            y += 40;

            _dgvSequen = new DataGridView
            {
                Location = new Point(16, y),
                Size = new Size(fw, height - y - 16),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(241, 245, 249),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                Font = new Font("Segoe UI", 9.5F),
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 30 },
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 252), ForeColor = Color.FromArgb(71, 85, 105), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(4) },
                DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 23, 42), Padding = new Padding(4) },
                EditMode = DataGridViewEditMode.EditProgrammatically
            };
            // Kolom checkbox — satu-satunya yang bisa diedit
            var chkCol = new DataGridViewCheckBoxColumn
            {
                Name = "Pilih",
                HeaderText = "✓",
                Width = 35,
                FalseValue = false,
                TrueValue = true,
                ReadOnly = false
            };
            _dgvSequen.Columns.Add(chkCol);
            _dgvSequen.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sequen", HeaderText = "SEQUEN", DataPropertyName = "DisplaySequen", Width = 70, ReadOnly = true });
            _dgvSequen.Columns.Add(new DataGridViewTextBoxColumn { Name = "Urutan", HeaderText = "Urutan", DataPropertyName = "DisplayUrutanPengerjaan", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
            _dgvSequen.SelectionChanged += DgvSequen_SelectionChanged;
            _dgvSequen.CellFormatting += DgvSequen_CellFormatting;

            // Range selection: klik checkbox pertama, lalu klik checkbox kedua → semua di antaranya ikut tercentang
            int _lastCheckedIndex = -1;
            _dgvSequen.CellClick += (s, ev) =>
            {
                if (ev.RowIndex < 0) return;
                var cell = _dgvSequen.Rows[ev.RowIndex].Cells["Pilih"];
                bool current = cell.Value != null && (bool)cell.Value;
                bool newVal = !current;
                cell.Value = newVal;

                if (newVal && _lastCheckedIndex >= 0 && _lastCheckedIndex != ev.RowIndex)
                {
                    // Auto-centang semua baris di antara _lastCheckedIndex dan ev.RowIndex
                    int from = Math.Min(_lastCheckedIndex, ev.RowIndex);
                    int to = Math.Max(_lastCheckedIndex, ev.RowIndex);
                    for (int i = from; i <= to; i++)
                    {
                        _dgvSequen.Rows[i].Cells["Pilih"].Value = true;
                    }
                }

                _lastCheckedIndex = newVal ? ev.RowIndex : -1;
                _dgvSequen.RefreshEdit();
            };

            txtSearch.TextChanged += (s, e) =>
            {
                string f = txtSearch.Text.Trim();
                _dgvSequen.DataSource = string.IsNullOrEmpty(f) ? _worksheetData :
                    _worksheetData.Where(d => d.DisplaySequen?.Trim() == f).ToList();
            };

            card.Controls.Add(_dgvSequen);
            return card;
        }

        private async void BtnSimpanTerpilih_Click(object sender, EventArgs e)
        {
            // Kumpulkan baris yang dicentang
            var checkedRows = new List<LkoService.LkoAggregatedData>();
            foreach (DataGridViewRow row in _dgvSequen.Rows)
            {
                bool isChecked = row.Cells["Pilih"].Value != null && (bool)row.Cells["Pilih"].Value;
                if (!isChecked) continue;
                var rowData = row.DataBoundItem as LkoService.LkoAggregatedData;
                if (rowData != null) checkedRows.Add(rowData);
            }

            if (checkedRows.Count == 0)
            {
                MessageBox.Show("Centang (✓) satu atau lebih sequen terlebih dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string shiftName = _cboShift?.SelectedItem is CachedShiftDto shift ? shift.ShiftName : "-";
            int savedCount = 0;
            int skippedCount = 0;

            foreach (var rowData in checkedRows)
            {
                // Skip yang sudah tersimpan
                if (rowData.DbRecord != null) { skippedCount++; continue; }

                int.TryParse(rowData.Log?.QtyDefect ?? "0", out int defectMesin);
                int.TryParse(rowData.Log?.QtyProduk ?? "0", out int qtyProduct);

                var record = new mtc_app.features.operator_worksheet.data.dtos.LkoRecordDto
                {
                    WaktuSimpan = DateTime.Now,
                    NoMesin = GetEffectiveMachineNumber(),
                    IdMesin = _activeMachineId,
                    ShiftName = shiftName,
                    Nik = _nikOperator,
                    Sequen = rowData.DisplaySequen,
                    UrutanKanban = rowData.DisplayUrutanPengerjaan,
                    QtyProduct = qtyProduct,
                    QtyDefectMesin = defectMesin,
                    QtyDefectOperator = 0,
                    KodeDefect = "",
                    LotIdWire = "",
                    CutLength = rowData.Master?.CutLength ?? "",
                    KombinasiWire = rowData.Master?.KombinasiWire ?? "",
                    TerminalA = rowData.Master?.TerminalA ?? "",
                    TerminalB = rowData.Master?.TerminalB ?? "",
                    SealA = rowData.Master?.SealA ?? "",
                    SealB = rowData.Master?.SealB ?? "",
                    QtyMaster = rowData.Master?.Qty ?? "",
                    FrontChA = rowData.Jissk?.FrontChA ?? "0",
                    FrontCwA = rowData.Jissk?.FrontCwA ?? "0",
                    RearChA = rowData.Jissk?.RearChA ?? "0",
                    RearCwA = rowData.Jissk?.RearCwA ?? "0",
                    FrontChB = rowData.Jissk?.FrontChB ?? "0",
                    FrontCwB = rowData.Jissk?.FrontCwB ?? "0",
                    RearChB = rowData.Jissk?.RearChB ?? "0",
                    RearCwB = rowData.Jissk?.RearCwB ?? "0",
                    WaktuMulai = rowData.Log?.WaktuMulaiPengerjaan ?? "",
                    WaktuSelesai = rowData.Log?.WaktuSelesaiPengerjaan ?? ""
                };

                try
                {
                    bool savedOnline = await _lkoService.SaveToDatabase(record);
                    rowData.DbRecord = record;
                    rowData.IsOffline = !savedOnline;
                    savedCount++;
                }
                catch { }
            }

            // Reset semua checkbox
            foreach (DataGridViewRow row in _dgvSequen.Rows)
                row.Cells["Pilih"].Value = false;

            // Refresh UI
            _dgvSequen.Refresh();
            var savedData = _worksheetData.Where(x => x.DbRecord != null).ToList();
            _dgvTersimpan.DataSource = null;
            _dgvTersimpan.DataSource = savedData;
            UpdateHeaderQty();

            string msg = $"{savedCount} sequen berhasil disimpan.";
            if (skippedCount > 0) msg += $"\n{skippedCount} sequen dilewati (sudah tersimpan).";
            MessageBox.Show(msg, "Simpan Terpilih", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =====================================================================
        //  BOTTOM PANEL: Riwayat Produksi
        // =====================================================================
        private Panel CreateTersimpanPanel(int width, int height)
        {
            var card = CreateCard(width, height);
            card.Controls.Add(new Label { Text = "SUDAH TERSIMPAN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(34, 197, 94), AutoSize = true, Location = new Point(16, 12) });

            _dgvTersimpan = new DataGridView
            {
                Location = new Point(16, 38),
                Size = new Size(width - 32, height - 54),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(241, 245, 249),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                Font = new Font("Segoe UI", 9.5F),
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 28 },
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 252), ForeColor = Color.FromArgb(71, 85, 105), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(4) },
                DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(220, 252, 231), SelectionForeColor = Color.FromArgb(15, 23, 42), Padding = new Padding(4) }
            };
            _dgvTersimpan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sequen", DataPropertyName = "DisplaySequen", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvTersimpan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Urutan", DataPropertyName = "DisplayUrutanPengerjaan", Width = 80 });

            _dgvTersimpan.SelectionChanged += DgvTersimpan_SelectionChanged;

            card.Controls.Add(_dgvTersimpan);
            return card;
        }

        // =====================================================================
        //  DATA LOADING
        // =====================================================================
        private async void LoadSequenData()
        {
            try
            {
                // Offload pembacaan dan parsing file ke background thread agar UI tidak freeze
                var data = await Task.Run(() => 
                {
                    return _lkoService.GetAllWorksheetData(_machineNumber);
                });

                // Reverse urutan dari file: baris terbawah di file muncul paling atas di UI
                data = await Task.Run(() => 
                {
                    data.Reverse();
                    return data;
                });

                _worksheetData = data;

                // Update UI SECARA INSTAN dengan data lokal yang sudah diparse
                _dgvSequen.DataSource = _worksheetData;
                
                // Mulai background process untuk sinkronisasi DB (bisa makan waktu kalau koneksi jelek)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Merge dengan data DB (defect operator, kode defect, status)
                        await _lkoService.MergeDbRecordsAsync(_worksheetData, _machineNumber);

                        // Perbarui UI lagi dari background setelah selesai sync DB
                        if (!this.IsDisposed)
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                _dgvSequen.Refresh();
                                
                                var savedData = _worksheetData.Where(x => x.DbRecord != null).ToList();
                                _dgvTersimpan.DataSource = null;
                                _dgvTersimpan.DataSource = savedData;
                                
                                UpdateHeaderQty();
                            });
                        }
                    }
                    catch (Exception syncEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"DB Sync error: {syncEx.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSequenData error: {ex.Message}");
            }
        }

        private void UpdateHeaderQty()
        {
            if (_worksheetData == null) return;
            
            int targetSum = 0;
            int grossSum = 0;
            int defectSum = 0;

            foreach(var item in _worksheetData) {
                // hanya yang sudah disimpan via tombol Simpan
                if (item.DbRecord != null) {
                    grossSum += item.DbRecord.QtyProduct;
                    defectSum += item.DbRecord.QtyDefectOperator;
                }
            }
            
            int netSum = grossSum - defectSum;
            
            // Progress bar = rasio OK / Gross
            _pbGrossQty.Maximum = Math.Max(grossSum, 1);
            _pbGrossQty.Value = Math.Min(grossSum, _pbGrossQty.Maximum);
            
            _pbNetQty.Maximum = Math.Max(grossSum, 1);
            _pbNetQty.Value = Math.Min(Math.Max(netSum, 0), _pbNetQty.Maximum);
            
            _lblGrossQty.Text = $"Gross: {grossSum}";
            _lblNetQty.Text = $"OK: {netSum}";

            // Realign
            _lblNetQty.Left = _pbNetQty.Left - TextRenderer.MeasureText(_lblNetQty.Text, _lblNetQty.Font).Width - 8;
            _pbGrossQty.Left = _lblNetQty.Left - _pbGrossQty.Width - 16;
            _lblGrossQty.Left = _pbGrossQty.Left - TextRenderer.MeasureText(_lblGrossQty.Text, _lblGrossQty.Font).Width - 8;
        }

        private void DgvSequen_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvSequen?.CurrentRow == null) return;
            var rowData = _dgvSequen.CurrentRow.DataBoundItem as LkoService.LkoAggregatedData;
            if (rowData == null) return;

            _activeSource = ActiveGrid.Sequen;
            _activeRowData = rowData;

            _isPopulatingFields = true;
            PopulateInputFields(rowData);
            _isPopulatingFields = false;
            
            _autoSaveTimer?.Stop(); // Jangan auto-save sampai ada interaksi user
            
            // Auto scroll ke atas agar form input terlihat
            if (_pnlContent != null) _pnlContent.AutoScrollPosition = new Point(0, 0);
        }

        private void DgvTersimpan_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvTersimpan?.CurrentRow == null) return;
            var rowData = _dgvTersimpan.CurrentRow.DataBoundItem as LkoService.LkoAggregatedData;
            if (rowData == null) return;

            _activeSource = ActiveGrid.Tersimpan;
            _activeRowData = rowData;

            _isPopulatingFields = true;
            PopulateInputFields(rowData);
            _isPopulatingFields = false;
            
            _autoSaveTimer?.Stop(); // Grid tersimpan tidak punya auto-save
            
            // Auto scroll ke atas agar form input terlihat
            if (_pnlContent != null) _pnlContent.AutoScrollPosition = new Point(0, 0);
        }

        private void PopulateInputFields(LkoService.LkoAggregatedData rowData)
        {
            _lblLotId.Text = $"Lot ID: {rowData.DisplaySequen}";
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
            _txtFrontChA.Text = "0";
            _txtRearChA.Text = "0";
            _txtFrontCwA.Text = "0";
            _txtRearCwA.Text = "0";
            // Populate Front/Rear from Jissk
            UpdateFrontRearFields(rowData.Jissk);
            _txtQtyProduksi.Text = rowData.DbRecord != null ? rowData.DbRecord.QtyProduct.ToString() : (rowData.Log?.QtyProduk ?? "");
            _txtCutL.Text = rowData.DbRecord != null && !string.IsNullOrEmpty(rowData.DbRecord.CutLength) ? rowData.DbRecord.CutLength : (!string.IsNullOrWhiteSpace(rowData.Master?.CutLength) ? rowData.Master.CutLength : "0");
            _txtLotIdWire.Text = rowData.DbRecord?.LotIdWire ?? "";
            _txtDefectMesin.Text = rowData.Log?.QtyDefect ?? "0";

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

                // Jika indikator HasTerminal bukan "2" → strip only → muat Strip.jpg
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
                        _lblImageInfo.Text = $"📷 Sisi {sisiLabel}: {stripFile} (Strip Only)";
                    }
                    else
                    {
                        _lblImageInfo.Text = $"⚠ Sisi {sisiLabel}: Strip Only — {stripFile} tidak ditemukan";
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
                    _lblImageInfo.Text = $"📷 Sisi {sisiLabel}: {usedFileName}";
                }
                else
                {
                    string tried = fileNameWithSeal != null
                        ? $"{fileNameWithSeal} / {fileNameNoSeal}"
                        : fileNameNoSeal;
                    _lblImageInfo.Text = $"⚠ Gambar tidak ditemukan: {tried}";
                }
            }
            catch (Exception ex)
            {
                _lblImageInfo.Text = $"Error memuat gambar: {ex.Message}";
            }
        }

        // =====================================================================
        //  ZOOMABLE IMAGE POPUP
        // =====================================================================
        private void ShowZoomableImage(System.Drawing.Image sourceImage, string title)
        {
            var popup = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(Math.Min(Screen.PrimaryScreen.WorkingArea.Width - 100, 1200),
                               Math.Min(Screen.PrimaryScreen.WorkingArea.Height - 100, 800)),
                BackColor = Color.FromArgb(30, 30, 30),
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                KeyPreview = true
            };

            // Buat copy dari image agar tidak terganggu dispose
            var imgCopy = new Bitmap(sourceImage);
            float zoomLevel = 1.0f;
            PointF offset = PointF.Empty;
            bool isDragging = false;
            Point dragStart = Point.Empty;
            PointF offsetStart = PointF.Empty;

            // Panel gambar utama (custom paint)
            var canvas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Cursor = Cursors.Hand
            };
            canvas.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(canvas, true);

            // Label zoom di pojok
            var lblZoom = new Label
            {
                Text = "100%",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(120, 0, 0, 0),
                AutoSize = true,
                Location = new Point(12, 12),
                Padding = new Padding(6, 3, 6, 3)
            };
            canvas.Controls.Add(lblZoom);

            // Tombol close
            var btnClose = new Button
            {
                Text = "✕ Tutup",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(180, 220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(90, 36),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Location = new Point(popup.Width - btnClose.Width - 16, 12);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Click += (s, ev) => popup.Close();
            canvas.Controls.Add(btnClose);

            // Label instruksi
            var lblHelp = new Label
            {
                Text = "Scroll = Zoom  |  Drag = Geser  |  Klik Tutup / tekan Esc",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Location = new Point(12, popup.Height - 35),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            canvas.Controls.Add(lblHelp);

            // Paint
            canvas.Paint += (s, ev) =>
            {
                ev.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                ev.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float drawW = imgCopy.Width * zoomLevel;
                float drawH = imgCopy.Height * zoomLevel;

                // Center image + offset
                float cx = (canvas.Width - drawW) / 2 + offset.X;
                float cy = (canvas.Height - drawH) / 2 + offset.Y;

                ev.Graphics.DrawImage(imgCopy, cx, cy, drawW, drawH);
            };

            // Scroll = Zoom
            canvas.MouseWheel += (s, ev) =>
            {
                float oldZoom = zoomLevel;
                if (ev.Delta > 0)
                    zoomLevel = Math.Min(zoomLevel * 1.2f, 10f);
                else
                    zoomLevel = Math.Max(zoomLevel / 1.2f, 0.1f);

                lblZoom.Text = $"{(int)(zoomLevel * 100)}%";
                canvas.Invalidate();
            };

            // Drag = Pan
            canvas.MouseDown += (s, ev) =>
            {
                if (ev.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    dragStart = ev.Location;
                    offsetStart = offset;
                    canvas.Cursor = Cursors.SizeAll;
                }
            };
            canvas.MouseMove += (s, ev) =>
            {
                if (isDragging)
                {
                    offset = new PointF(
                        offsetStart.X + ev.X - dragStart.X,
                        offsetStart.Y + ev.Y - dragStart.Y);
                    canvas.Invalidate();
                }
            };
            canvas.MouseUp += (s, ev) =>
            {
                isDragging = false;
                canvas.Cursor = Cursors.Hand;
            };

            // Double click = reset zoom
            canvas.MouseDoubleClick += (s, ev) =>
            {
                zoomLevel = 1.0f;
                offset = PointF.Empty;
                lblZoom.Text = "100%";
                canvas.Invalidate();
            };

            popup.Controls.Add(canvas);
            popup.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Escape) popup.Close(); };
            popup.FormClosed += (s, ev) => imgCopy.Dispose();

            // Focus canvas agar scroll langsung bisa
            popup.Shown += (s, ev) => canvas.Focus();

            popup.ShowDialog(this);
        }

        // =====================================================================
        //  NATIVE METHODS (for window dragging)
        // =====================================================================
        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ReleaseCapture();
        }
    }
}
