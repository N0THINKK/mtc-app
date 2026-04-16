using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
        private Label _lblQtyTotal;    // "QTY: 123 / 777"
        private ProgressBar _pbQty;    // visual progress

        // === Content panels ===
        private Panel _pnlContent;

        // === Input Produksi fields ===
        private Label _lblLotId;
        private TextBox _txtLotTermA;
        private TextBox _txtFrontChA;
        private TextBox _txtRearChA;
        private TextBox _txtFrontCwA;
        private TextBox _txtRearCwA;
        private Button _btnSisiA;
        private Button _btnSisiB;
        private bool _isSisiA = true;

        // === Aktivitas fields ===
        private TextBox _txtQtyProduksi;
        private ComboBox _cboDefect1;
        private ComboBox _cboDefect2;
        private ComboBox _cboKodeDefect;
        private TextBox _txtDefectMesin;

        // === Sequen list ===
        private DataGridView _dgvSequen;

        // === Riwayat Produksi ===
        private DataGridView _dgvRiwayat;

        // === Loaded data ===
        private List<LkoService.LkoAggregatedData> _worksheetData = new List<LkoService.LkoAggregatedData>();

        // === Header data ===
        private string _machineNumber = "-";
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

        private void OperatorWorksheetForm_Load(object sender, EventArgs e)
        {
            LoadHeaderData();
            InitializeUI();
        }

        // =====================================================================
        //  HEADER DATA
        // =====================================================================
        private void LoadHeaderData()
        {
            // === NIK dari Session ===
            _nikOperator = UserSession.CurrentUser?.Username ?? "-";

            // === No Mesin dari database config ===
            try
            {
                string machineIdStr = DatabaseHelper.GetMachineId();
                if (int.TryParse(machineIdStr, out int machineId))
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        var info = Dapper.SqlMapper.QueryFirstOrDefault(conn,
                            @"SELECT m.machine_number, mt.type_name, ma.area_name 
                              FROM machines m 
                              LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                              LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                              WHERE m.machine_id = @Id",
                            new { Id = machineId });

                        if (info != null)
                        {
                            string typeName = info.type_name?.ToString() ?? "";
                            string areaName = info.area_name?.ToString() ?? "";
                            string machNum = info.machine_number?.ToString() ?? "";
                            // Format: type.area-no_urut  (contoh: AC90.NPR-02)
                            _machineNumber = $"{typeName}.{areaName}-{machNum}";
                        }
                    }
                }
            }
            catch { _machineNumber = "-"; }

            // === Shift dari database ===
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    _shifts = conn.Query<CachedShiftDto>(
                        "SELECT shift_id AS ShiftId, shift_name AS ShiftName FROM shifts ORDER BY shift_id"
                    ).ToList();
                }

                // Auto-select berdasarkan jam saat ini
                TimeSpan now = DateTime.Now.TimeOfDay;
                bool isPagi = now >= new TimeSpan(7, 0, 0) && now < new TimeSpan(19, 0, 0);
                // Cari index shift yang cocok (nama mengandung "Pagi"/"A" untuk pagi, "Malam"/"B" untuk malam)
                for (int i = 0; i < _shifts.Count; i++)
                {
                    string name = _shifts[i].ShiftName?.ToUpper() ?? "";
                    if (isPagi && (name.Contains("PAGI") || name.Contains("SIANG") || name == "A" || name == "1"))
                    {
                        _defaultShiftIndex = i;
                        break;
                    }
                    if (!isPagi && (name.Contains("MALAM") || name == "B" || name == "2"))
                    {
                        _defaultShiftIndex = i;
                        break;
                    }
                }
            }
            catch
            {
                // Fallback jika DB tidak tersedia
                _shifts = new List<CachedShiftDto>
                {
                    new CachedShiftDto { ShiftId = 1, ShiftName = "A" },
                    new CachedShiftDto { ShiftId = 2, ShiftName = "B" }
                };
                TimeSpan now = DateTime.Now.TimeOfDay;
                _defaultShiftIndex = (now >= new TimeSpan(7, 0, 0) && now < new TimeSpan(19, 0, 0)) ? 0 : 1;
            }

            // === QTY dari PrdLog.csv ===
            try
            {
                var data = _lkoService.GetAllWorksheetData();
                _qtyTarget = data.Count;
                _qtyDone = data.Count(d => !string.IsNullOrWhiteSpace(d.Log?.QtyProduk) && d.Log.QtyProduk != "0");
            }
            catch { }
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

            // ---- 1) TITLE BAR ----
            var pnlTitleBar = CreateTitleBar();
            this.Controls.Add(pnlTitleBar);

            // ---- 2) INFO HEADER ----
            var pnlInfoHeader = CreateInfoHeader();
            pnlInfoHeader.Top = pnlTitleBar.Bottom;
            this.Controls.Add(pnlInfoHeader);

            // ---- 3) CONTENT AREA ----
            _pnlContent = new Panel
            {
                Top = pnlInfoHeader.Bottom,
                Left = 0,
                Width = this.ClientSize.Width,
                Height = this.ClientSize.Height - pnlInfoHeader.Bottom,
                BackColor = Color.FromArgb(243, 244, 246),
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 12)
            };
            _pnlContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Calculate column widths (3 columns: 28% / 38% / 34%)
            int contentW = _pnlContent.Width - 32; // minus padding
            int gap = 12;
            int col1W = (int)(contentW * 0.26);
            int col2W = (int)(contentW * 0.38);
            int col3W = contentW - col1W - col2W - gap * 2;
            int topRowHeight = 420;

            // === LEFT: Input Produksi ===
            var pnlInputProduksi = CreateInputProduksiPanel(col1W, topRowHeight);
            pnlInputProduksi.Location = new Point(16, 12);
            _pnlContent.Controls.Add(pnlInputProduksi);

            // === MIDDLE: Aktivitas ===
            var pnlAktivitas = CreateAktivitasPanel(col2W, topRowHeight);
            pnlAktivitas.Location = new Point(pnlInputProduksi.Right + gap, 12);
            _pnlContent.Controls.Add(pnlAktivitas);

            // === RIGHT: Sequen ===
            var pnlSequen = CreateSequenPanel(col3W, topRowHeight);
            pnlSequen.Location = new Point(pnlAktivitas.Right + gap, 12);
            _pnlContent.Controls.Add(pnlSequen);

            // === BOTTOM: Riwayat Produksi ===
            var pnlRiwayat = CreateRiwayatPanel(contentW, 180);
            pnlRiwayat.Location = new Point(16, pnlInputProduksi.Bottom + gap);
            _pnlContent.Controls.Add(pnlRiwayat);

            this.Controls.Add(_pnlContent);

            this.ResumeLayout(true);

            // Load data ke Sequen list
            LoadSequenData();
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
                Height = 44,
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

            // ---- Tanggal ----
            var lblTanggalLabel = new Label
            {
                Text = "Tanggal :",
                Font = labelFont,
                ForeColor = labelColor,
                AutoSize = true,
                Location = new Point(leftX, labelY),
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(lblTanggalLabel);

            _lblTanggal = new Label
            {
                Text = DateTime.Now.ToString("yyyy/MM/dd"),
                Font = valueFont,
                ForeColor = valueColor,
                AutoSize = true,
                Location = new Point(lblTanggalLabel.Right + 4, labelY),
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(_lblTanggal);

            // ---- No Mesin ----
            int noMesinX = _lblTanggal.Right + 28;
            var lblMesinLabel = new Label
            {
                Text = "No Mesin :",
                Font = labelFont,
                ForeColor = labelColor,
                AutoSize = true,
                Location = new Point(noMesinX, labelY),
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(lblMesinLabel);

            _lblNoMesin = new Label
            {
                Text = _machineNumber,
                Font = valueFont,
                ForeColor = valueColor,
                AutoSize = true,
                Location = new Point(lblMesinLabel.Right + 4, labelY),
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(_lblNoMesin);

            // ---- Shift (ComboBox) ----
            int shiftX = _lblNoMesin.Right + 28;
            var lblShiftLabel = new Label
            {
                Text = "Shift :",
                Font = labelFont,
                ForeColor = labelColor,
                AutoSize = true,
                Location = new Point(shiftX, labelY),
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(lblShiftLabel);

            _cboShift = new ComboBox
            {
                Font = valueFont,
                ForeColor = valueColor,
                BackColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 28),
                Location = new Point(lblShiftLabel.Right + 4, labelY - 4),
                Cursor = Cursors.Hand
            };

            // Isi data shift dari database
            _cboShift.DisplayMember = "ShiftName";
            _cboShift.ValueMember = "ShiftId";
            if (_shifts != null && _shifts.Count > 0)
            {
                _cboShift.DataSource = _shifts;
                // Pilih default berdasarkan jam
                try
                {
                    if (_defaultShiftIndex >= 0 && _defaultShiftIndex < _cboShift.Items.Count)
                    {
                        _cboShift.SelectedIndex = _defaultShiftIndex;
                    }
                }
                catch { /* abaikan jika gagal set index */ }
            }

            pnl.Controls.Add(_cboShift);

            // ---- NIK ----
            int nikX = _cboShift.Right + 28;
            var lblNikLabel = new Label
            {
                Text = "NIK :",
                Font = labelFont,
                ForeColor = labelColor,
                AutoSize = true,
                Location = new Point(nikX, labelY),
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(lblNikLabel);

            _lblNik = new Label
            {
                Text = _nikOperator,
                Font = valueFont,
                ForeColor = valueColor,
                AutoSize = true,
                Location = new Point(lblNikLabel.Right + 4, labelY),
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(_lblNik);

            // ---- QTY section (right-aligned) ----
            // Build QTY from right to left
            int qtyAreaRight = pnl.Width - 20;

            // Progress mini-bar
            _pbQty = new ProgressBar
            {
                Minimum = 0,
                Maximum = Math.Max(_qtyTarget, 1),
                Value = Math.Min(_qtyDone, Math.Max(_qtyTarget, 1)),
                Size = new Size(30, 16),
                BackColor = Color.FromArgb(226, 232, 240),
                Style = ProgressBarStyle.Continuous
            };
            _pbQty.Location = new Point(qtyAreaRight - _pbQty.Width, labelY);
            _pbQty.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnl.Controls.Add(_pbQty);

            // QTY value "123 / 777"
            string doneStr = _qtyDone.ToString();
            string targetStr = _qtyTarget.ToString();

            _lblQtyTotal = new Label
            {
                Text = $"QTY:  ",
                Font = labelFont,
                ForeColor = labelColor,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var lblQtyValues = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(0, labelY)
            };

            // We'll use the Paint event for dual-color rendering
            lblQtyValues.Font = valueFont;
            lblQtyValues.Text = $"QTY:  {doneStr} / {targetStr}";
            lblQtyValues.ForeColor = valueColor;

            // Measure total width
            int qtyTextWidth = TextRenderer.MeasureText(lblQtyValues.Text, valueFont).Width + 8;
            lblQtyValues.Location = new Point(_pbQty.Left - qtyTextWidth - 12, labelY);
            lblQtyValues.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Custom paint for coloring the "done" number green
            lblQtyValues.Paint += (s, pe) =>
            {
                var lbl = (Label)s;
                pe.Graphics.Clear(lbl.BackColor);
                pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                string prefix = "QTY:  ";
                string done = doneStr;
                string separator = " / ";
                string target = targetStr;

                int x = 0;
                // "QTY:  " in gray
                TextRenderer.DrawText(pe.Graphics, prefix, labelFont, new Point(x, 0), labelColor);
                x += TextRenderer.MeasureText(prefix, labelFont).Width - 4;

                // done number in green
                Color greenColor = Color.FromArgb(34, 197, 94); // green-500
                TextRenderer.DrawText(pe.Graphics, done, valueFont, new Point(x, 0), greenColor);
                x += TextRenderer.MeasureText(done, valueFont).Width - 4;

                // " / " in default
                TextRenderer.DrawText(pe.Graphics, separator, valueFont, new Point(x, 0), valueColor);
                x += TextRenderer.MeasureText(separator, valueFont).Width - 4;

                // target in bold dark
                TextRenderer.DrawText(pe.Graphics, target, valueFont, new Point(x, 0), valueColor);
            };

            pnl.Controls.Add(lblQtyValues);

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

            _txtLotTermA = CreateStyledTextBox("Lot Term A", fw);
            _txtLotTermA.Location = new Point(16, y);
            _txtLotTermA.ReadOnly = true;
            card.Controls.Add(_txtLotTermA);
            y += 40;

            card.Controls.Add(new Label { Text = "Front C/H", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            y += 20;
            _txtFrontChA = CreateStyledTextBox("Masukkan Front C/H", fw);
            _txtFrontChA.Location = new Point(16, y);
            _txtFrontChA.ReadOnly = true;
            card.Controls.Add(_txtFrontChA);
            y += 36;

            _txtRearChA = CreateStyledTextBox("Masukkan Rear C/H", fw);
            _txtRearChA.Location = new Point(16, y);
            _txtRearChA.ReadOnly = true;
            _txtRearChA.BackColor = Color.FromArgb(241, 245, 249);
            card.Controls.Add(_txtRearChA);
            y += 40;

            card.Controls.Add(new Label { Text = "Front C/W", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            y += 20;
            _txtFrontCwA = CreateStyledTextBox("Masukkan Front C/W", fw);
            _txtFrontCwA.Location = new Point(16, y);
            _txtFrontCwA.ReadOnly = true;
            card.Controls.Add(_txtFrontCwA);
            y += 36;

            _txtRearCwA = CreateStyledTextBox("Masukkan Rear C/W", fw);
            _txtRearCwA.Location = new Point(16, y);
            _txtRearCwA.ReadOnly = true;
            _txtRearCwA.BackColor = Color.FromArgb(241, 245, 249);
            card.Controls.Add(_txtRearCwA);
            y += 44;

            var btnSave = new Button { Text = "\u2713 Simpan", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Size = new Size(fw, 36), Location = new Point(16, y), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            card.Controls.Add(btnSave);

            return card;
        }

        private void ToggleSisi(bool isSisiA)
        {
            _isSisiA = isSisiA;
            _btnSisiA.BackColor = isSisiA ? AppColors.Primary : Color.FromArgb(241, 245, 249);
            _btnSisiA.ForeColor = isSisiA ? Color.White : Color.FromArgb(71, 85, 105);
            _btnSisiB.BackColor = !isSisiA ? AppColors.Primary : Color.FromArgb(241, 245, 249);
            _btnSisiB.ForeColor = !isSisiA ? Color.White : Color.FromArgb(71, 85, 105);
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

            // QTY Produksi
            card.Controls.Add(new Label { Text = "QTY Produksi", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            y += 20;
            _txtQtyProduksi = CreateStyledTextBox("Masukkan Qty Produksi", fw);
            _txtQtyProduksi.Location = new Point(16, y);
            card.Controls.Add(_txtQtyProduksi);
            y += 42;

            // No. 4M
            card.Controls.Add(new Label { Text = "No. 4M", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            y += 20;
            _cboDefect1 = new ComboBox { Font = FieldValueFont, Size = new Size(halfW, 30), Location = new Point(16, y), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(248, 250, 252) };
            _cboDefect1.Items.Add("- Pilih Defect -");
            _cboDefect1.SelectedIndex = 0;
            card.Controls.Add(_cboDefect1);

            _cboDefect2 = new ComboBox { Font = FieldValueFont, Size = new Size(halfW, 30), Location = new Point(_cboDefect1.Right + 8, y), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(248, 250, 252) };
            _cboDefect2.Items.Add("- Pilih Defect -");
            _cboDefect2.SelectedIndex = 0;
            card.Controls.Add(_cboDefect2);
            y += 42;

            // Kode Defect + Defect Mesin
            card.Controls.Add(new Label { Text = "Kode Defect  \u24D8", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });
            card.Controls.Add(new Label { Text = "Defect Mesin  \u24D8", Font = FieldLabelFont, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16 + halfW + 8, y) });
            y += 20;

            _cboKodeDefect = new ComboBox { Font = FieldValueFont, Size = new Size(halfW, 30), Location = new Point(16, y), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(248, 250, 252) };
            _cboKodeDefect.Items.Add("- Pilih Defect -");
            _cboKodeDefect.SelectedIndex = 0;
            card.Controls.Add(_cboKodeDefect);

            _txtDefectMesin = CreateStyledTextBox("0", halfW);
            _txtDefectMesin.Location = new Point(_cboKodeDefect.Right + 8, y);
            _txtDefectMesin.Text = "0";
            card.Controls.Add(_txtDefectMesin);
            y += 48;

            // Buttons
            var btnTrack = new Button { Text = "\u2726 Track Defect", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Size = new Size(halfW, 36), Location = new Point(16, y), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, Cursor = Cursors.Hand };
            btnTrack.FlatAppearance.BorderSize = 0;
            card.Controls.Add(btnTrack);

            var btnSave = new Button { Text = "\u2713 Simpan", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Size = new Size(halfW, 36), Location = new Point(btnTrack.Right + 8, y), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSimpanAktivitas_Click;
            card.Controls.Add(btnSave);
            y += 50;

            // Riwayat Produksi mini-header
            card.Controls.Add(new Label { Text = "Riwayat Produksi", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true, Location = new Point(16, y) });
            y += 24;
            card.Controls.Add(new Label { Text = "Sequen     1 - 1 dari 1", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(16, y) });

            return card;
        }

        private void BtnSimpanAktivitas_Click(object sender, EventArgs e)
        {
            if (_dgvSequen?.CurrentRow == null) return;
            var rowData = _dgvSequen.CurrentRow.DataBoundItem as LkoService.LkoAggregatedData;
            if (rowData != null)
            {
                rowData.Log.QtyProduk = _txtQtyProduksi.Text.Trim();
                rowData.Log.QtyDefect = _txtDefectMesin.Text.Trim();
                rowData.Log.WaktuSelesaiPengerjaan = DateTime.Now.ToString("HH:mm:ss");
                try
                {
                    _lkoService.SaveWorksheet(rowData.Log);
                    MessageBox.Show("Data berhasil disimpan.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal menyimpan: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // =====================================================================
        //  RIGHT PANEL: Sequen
        // =====================================================================
        private Panel CreateSequenPanel(int width, int height)
        {
            var card = CreateCard(width, height);
            card.Controls.Add(new Label { Text = "SEQUEN", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true, Location = new Point(16, 14) });
            card.Controls.Add(new Label { Text = $"\u25C0\u25CF {_qtyDone}/{_qtyTarget}", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = AppColors.Primary, AutoSize = true, Location = new Point(width - 100, 16) });

            int y = 40;
            var txtSearch = CreateStyledTextBox("\uD83D\uDD0D Cari...", width - 36);
            txtSearch.Location = new Point(16, y);
            card.Controls.Add(txtSearch);
            y += 38;

            _dgvSequen = new DataGridView
            {
                Location = new Point(16, y),
                Size = new Size(width - 36, height - y - 16),
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
                RowTemplate = { Height = 30 },
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 252), ForeColor = Color.FromArgb(71, 85, 105), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(4) },
                DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 23, 42), Padding = new Padding(4) }
            };
            _dgvSequen.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sequen", HeaderText = "SEQUEN", DataPropertyName = "DisplaySequen", Width = 70 });
            _dgvSequen.Columns.Add(new DataGridViewTextBoxColumn { Name = "Urutan", HeaderText = "Urutan", DataPropertyName = "DisplayUrutanPengerjaan", Width = 55 });
            _dgvSequen.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kombinasi", HeaderText = "Kombinasi", DataPropertyName = "DisplayKombinasi", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvSequen.SelectionChanged += DgvSequen_SelectionChanged;

            txtSearch.TextChanged += (s, e) =>
            {
                string f = txtSearch.Text.Trim().ToLower();
                _dgvSequen.DataSource = string.IsNullOrEmpty(f) ? _worksheetData :
                    _worksheetData.Where(d => (d.DisplaySequen?.ToLower().Contains(f) == true) || (d.DisplayKombinasi?.ToLower().Contains(f) == true)).ToList();
            };

            card.Controls.Add(_dgvSequen);
            return card;
        }

        // =====================================================================
        //  BOTTOM PANEL: Riwayat Produksi
        // =====================================================================
        private Panel CreateRiwayatPanel(int width, int height)
        {
            var card = CreateCard(width, height);
            card.Controls.Add(new Label { Text = "Riwayat Produksi", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true, Location = new Point(16, 12) });

            _dgvRiwayat = new DataGridView
            {
                Location = new Point(16, 38),
                Size = new Size(width - 36, height - 54),
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
                DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 23, 42), Padding = new Padding(4) }
            };
            _dgvRiwayat.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sequen", DataPropertyName = "DisplaySequen", Width = 70 });
            _dgvRiwayat.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kombinasi", DataPropertyName = "DisplayKombinasi", Width = 130 });
            _dgvRiwayat.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Term A", DataPropertyName = "DisplayTermA", Width = 100 });
            _dgvRiwayat.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Term B", DataPropertyName = "DisplayTermB", Width = 100 });
            _dgvRiwayat.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Qty", DataPropertyName = "DisplayQty", Width = 70 });
            _dgvRiwayat.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Qty Produk", DataPropertyName = "DisplayQtyProduk", Width = 90 });
            _dgvRiwayat.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Qty Defect", DataPropertyName = "DisplayQtyDefect", Width = 90 });
            _dgvRiwayat.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mulai", DataPropertyName = "DisplayWaktuMulai", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvRiwayat.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Selesai", DataPropertyName = "DisplayWaktuSelesai", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            card.Controls.Add(_dgvRiwayat);
            return card;
        }

        // =====================================================================
        //  DATA LOADING
        // =====================================================================
        private void LoadSequenData()
        {
            try
            {
                _worksheetData = _lkoService.GetAllWorksheetData();
                _dgvSequen.DataSource = _worksheetData;
                _dgvRiwayat.DataSource = _worksheetData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSequenData error: {ex.Message}");
            }
        }

        private void DgvSequen_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvSequen?.CurrentRow == null) return;
            var rowData = _dgvSequen.CurrentRow.DataBoundItem as LkoService.LkoAggregatedData;
            if (rowData == null) return;

            _lblLotId.Text = $"Lot ID: {rowData.DisplaySequen}";
            _txtLotTermA.Text = rowData.Master?.TerminalA ?? "";
            _txtFrontChA.Text = rowData.Master?.PanjangStripSisiA ?? "";
            _txtRearChA.Text = rowData.Master?.PanjangStripSisiB ?? "";
            _txtFrontCwA.Text = rowData.Master?.CutLength ?? "";
            _txtRearCwA.Text = "";
            _txtQtyProduksi.Text = rowData.Log?.QtyProduk ?? "";
            _txtDefectMesin.Text = rowData.Log?.QtyDefect ?? "0";
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
