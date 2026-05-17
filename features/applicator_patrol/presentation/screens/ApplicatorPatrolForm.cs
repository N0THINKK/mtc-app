using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.applicator_patrol.data.dtos;
using mtc_app.features.applicator_patrol.data.repositories;
using mtc_app.features.applicator_patrol.data.services;
using mtc_app.features.applicator_patrol.presentation.components;
using mtc_app.shared.data.dtos;
using mtc_app.shared.data.repositories;
using mtc_app.shared.data.session;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using static mtc_app.DatabaseHelper;

namespace mtc_app.features.applicator_patrol.presentation.screens
{
    public class ApplicatorPatrolForm : Form
    {
        // ── Layout constants ─────────────────────────────────────────────
        //  Semua zona berbagi Y yang sama agar sejajar (satu layer)
        //
        //  ROW_HEADER  (y=65)  : Tanggal | Shift | NIK | No.Mesin
        //  ROW_SUB     (y=130) : [kosong kiri] | Label "Sisi A" + toggle
        //  ROW_COLHDR  (y=168) : Header merah (kiri) = Header kuning (kanan)
        //  ROW_BODY    (y=198) : Tabel items (kiri) = List aplikator (kanan)
        //  ROW_FOOTER  (y=618) : Keterangan
        //
        private const int FORM_W = 1000;
        private const int FORM_H = 660;

        // Kolom
        private const int COL1_X = 12;       // tabel kiri
        private const int COL1_W = 280;
        private const int GAP = 8;
        private const int COL2_X = 300;      // list aplikator
        private const int COL2_W = 560;
        private const int COL3_X = 870;      // action buttons
        private const int COL3_W = 110;

        // Baris  (shared Y agar sejajar, jarak cukup agar tidak menumpuk)
        private const int ROW_HEADER = 50;
        private const int ROW_SUB = 130;
        private const int ROW_COLHDR = 168;
        private const int ROW_BODY = 198;
        private const int BODY_H = 390;
        private const int ROW_FOOTER = 600;

        // Pagination
        private const int PAGE_SIZE = 10;
        private int _currentPage = 0;

        // ── Dependencies ─────────────────────────────────────────────────
        private readonly IApplicatorPatrolRepository _repository;
        private readonly IMasterDataRepository _masterDataRepository;

        // ── State ────────────────────────────────────────────────────────
        private List<CachedShiftDto> _shifts = new List<CachedShiftDto>();
        private List<CachedMachineDto> _machines = new List<CachedMachineDto>();
        private string _currentSide = "A";
        private List<string> _sideAApplicators = new List<string>();
        private List<string> _sideBApplicators = new List<string>();
        private Dictionary<string, string> _judgmentsA = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _judgmentsB = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Menyimpan ng_items per aplikator (nomor item yg NG, e.g. "1,3")
        private Dictionary<string, string> _ngItemsA = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _ngItemsB = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ── Controls ─────────────────────────────────────────────────────
        private Label lblClock, lblSide, lblPageInfo;
        private AppInput txtTanggal, cmbShift, cmbNik, cmbMesin;
        private Panel pnlApplicatorList;
        private AppButton btnSideA, btnSideB, btnPrev, btnNext, btnRecord, btnSimpan, btnKeluar;
        private System.Windows.Forms.Timer _clockTimer;

        public ApplicatorPatrolForm(IApplicatorPatrolRepository repository, IMasterDataRepository masterDataRepository)
        {
            _repository = repository;
            _masterDataRepository = masterDataRepository;
            InitializeUI();
            SetupFormAsync();
        }

        // ═════════════════════════════════════════════════════════════════
        //  UI BUILD
        // ═════════════════════════════════════════════════════════════════

        private void InitializeUI()
        {
            this.Text = "Patroli Harian Aplikator";
            this.ClientSize = new Size(FORM_W, FORM_H);
            this.MinimumSize = this.Size;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            BuildTopBar();
            BuildHeaderInputs();
            BuildSubHeaderRow();
            BuildColumnHeaders();
            BuildCheckItemsBody();
            BuildApplicatorList();
            BuildActionButtons();
            BuildFooter();

            this.FormClosing += (s, e) => { _clockTimer?.Stop(); };
        }

        // ─── Top bar: clock + title + keluar ─────────────────────────────
        private void BuildTopBar()
        {
            lblClock = new Label
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                Font = new Font(AppFonts.FontFamily, 16, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true, Location = new Point(12, 6)
            };
            this.Controls.Add(lblClock);

            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            var lblTitle = new Label
            {
                Text = "PATROLI HARIAN APLIKATOR",
                Font = new Font(AppFonts.FontFamily, 17, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(200, 4, 640, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(lblTitle);

            btnKeluar = new AppButton
            {
                Text = "Keluar", Type = AppButton.ButtonType.Secondary,
                Bounds = new Rectangle(FORM_W - 105, 6, 90, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnKeluar.Click += (s, e) => this.Close();
            this.Controls.Add(btnKeluar);

            // Divider
            this.Controls.Add(new Panel { Bounds = new Rectangle(0, 42, FORM_W, 1), BackColor = AppColors.Border, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right });
        }

        // ─── Header inputs: Tanggal | Shift | NIK | No.Mesin ────────────
        private void BuildHeaderInputs()
        {
            txtTanggal = new AppInput { LabelText = "Tanggal",   InputType = AppInput.InputTypeEnum.Text, Enabled = false, Bounds = new Rectangle(COL1_X, ROW_HEADER, 150, 75) };
            cmbShift   = new AppInput { LabelText = "Shift",     InputType = AppInput.InputTypeEnum.Dropdown, AllowCustomText = false, Bounds = new Rectangle(170, ROW_HEADER, 115, 75) };
            cmbNik     = new AppInput { LabelText = "NIK",       InputType = AppInput.InputTypeEnum.Dropdown, AllowCustomText = true,  Bounds = new Rectangle(295, ROW_HEADER, 145, 75) };
            cmbMesin   = new AppInput { LabelText = "No. Mesin", InputType = AppInput.InputTypeEnum.Text,     Enabled = false,          Bounds = new Rectangle(450, ROW_HEADER, 210, 75), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            this.Controls.AddRange(new Control[] { txtTanggal, cmbShift, cmbNik, cmbMesin });
        }

        // ─── Sub-header row: Label "Sisi A" + toggle buttons ─────────────
        private void BuildSubHeaderRow()
        {
            lblSide = new Label
            {
                Text = "Sisi  A",
                Font = new Font(AppFonts.FontFamily, 16, FontStyle.Bold),
                ForeColor = AppColors.Primary,
                AutoSize = true,
                Location = new Point(COL2_X, ROW_SUB + 4)
            };
            this.Controls.Add(lblSide);

            btnSideA = new AppButton { Text = "Sisi A", Type = AppButton.ButtonType.Primary,   Bounds = new Rectangle(COL2_X + COL2_W - 230, ROW_SUB, 110, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnSideB = new AppButton { Text = "Sisi B", Type = AppButton.ButtonType.Secondary, Bounds = new Rectangle(COL2_X + COL2_W - 115, ROW_SUB, 110, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnSideA.Click += (s, e) => SwitchSide("A");
            btnSideB.Click += (s, e) => SwitchSide("B");
            this.Controls.Add(btnSideA);
            this.Controls.Add(btnSideB);
        }

        // ─── Column headers: merah (kiri) + kuning (kanan) di Y YANG SAMA ─
        private void BuildColumnHeaders()
        {
            // Header merah tabel kiri
            var hdrLeft = new Panel { Bounds = new Rectangle(COL1_X, ROW_COLHDR, COL1_W, 28), BackColor = Color.FromArgb(200, 40, 40) };
            void AddHL(string t, int x, int w) =>
                hdrLeft.Controls.Add(new Label { Text = t, Bounds = new Rectangle(x, 0, w, 28),
                    Font = new Font(AppFonts.FontFamily, 8f, FontStyle.Bold), ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle });
            AddHL("Nº",       0,  30);
            AddHL("ITEM",    30, 105);
            AddHL("CRITERIA",135, 85);
            AddHL("Metode",  220, 58);
            this.Controls.Add(hdrLeft);

            // Header kuning list kanan
            var hdrRight = new Panel { Bounds = new Rectangle(COL2_X, ROW_COLHDR, COL2_W, 28), BackColor = Color.FromArgb(255, 210, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            hdrRight.Controls.Add(new Label { Text = "No. Aplikator",    Bounds = new Rectangle(32, 0, 220, 28),  Font = new Font(AppFonts.FontFamily, 10f, FontStyle.Bold), ForeColor = AppColors.TextPrimary, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right });
            hdrRight.Controls.Add(new Label { Text = "Kondisi Aplikator", Bounds = new Rectangle(260, 0, 200, 28), Font = new Font(AppFonts.FontFamily, 10f, FontStyle.Bold), ForeColor = AppColors.TextPrimary, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Top | AnchorStyles.Right });
            this.Controls.Add(hdrRight);
        }

        // ─── Tabel check-items (kiri), mulai di ROW_BODY ─────────────────
        private void BuildCheckItemsBody()
        {
            var pnl = new Panel
            {
                Bounds = new Rectangle(COL1_X, ROW_BODY, COL1_W, BODY_H),
                BackColor = AppColors.Background,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            this.Controls.Add(pnl);

            string[][] rows = {
                new[] { "1", "Crimper Anvil\nAnvil holder\nSupporting stopper", "Tidak ada\nkotoran", "Visual" },
                new[] { "2", "Posisi Ram", "I-marks harus\nlurus", "Visual" },
                new[] { "3", "Kondisi pot oil\nPA5", "Dlm range\nMax–Min", "Visual" },
                new[] { "4", "Wire stopper\nSafety cover\nStrip terminal\nEASY", "Tidak ada\nkerusakan", "Manual\ncrimping\nVisual" },
                new[] { "5", "Crimper front/rear\nAnvil punggung\nShear blade\nFeeding claw", "Tidak ada\ndamage area\ncrimping", "Visual" },
                new[] { "6", "Crimper Anvil", "Masuk\nstandard", "Micro-\nmeter" },
                new[] { "7", "Validasi Appl", "Sesuai\nschedule", "Visual" }
            };

            int yOff = 0;
            foreach (var r in rows)
            {
                int maxLines = Math.Max(r[1].Split('\n').Length, Math.Max(r[2].Split('\n').Length, r[3].Split('\n').Length));
                int rH = Math.Max(40, maxLines * 16 + 12);

                var rPnl = new Panel { Bounds = new Rectangle(0, yOff, COL1_W - 2, rH), BackColor = AppColors.Background };

                void AddCell(string txt, int x, int w, FontStyle fs = FontStyle.Regular) =>
                    rPnl.Controls.Add(new Label { Text = txt, Bounds = new Rectangle(x, 0, w, rH),
                        Font = new Font(AppFonts.FontFamily, 8f, fs), ForeColor = AppColors.TextPrimary,
                        TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle });

                AddCell(r[0],  0,  30, FontStyle.Bold);
                AddCell(r[1], 30, 105);
                AddCell(r[2],135,  85);
                AddCell(r[3],220,  58);
                pnl.Controls.Add(rPnl);
                yOff += rH;
            }
        }

        // ─── Applicator list panel (kanan), mulai di ROW_BODY ────────────
        private void BuildApplicatorList()
        {
            pnlApplicatorList = new Panel
            {
                Bounds = new Rectangle(COL2_X, ROW_BODY, COL2_W, BODY_H),
                BackColor = AppColors.Background,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(pnlApplicatorList);
        }

        // ─── Action buttons (kolom 3) ────────────────────────────────────
        private void BuildActionButtons()
        {
            int bW = COL3_W, bH = 36;

            btnPrev = new AppButton { Text = "◀ PREV", Type = AppButton.ButtonType.Secondary, Bounds = new Rectangle(COL3_X, ROW_BODY + 60, bW, bH), Visible = false, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnPrev.Click += (s, e) => GoPage(_currentPage - 1);
            this.Controls.Add(btnPrev);

            lblPageInfo = new Label
            {
                Bounds = new Rectangle(COL3_X, ROW_BODY + 100, bW, 22),
                Font = new Font(AppFonts.FontFamily, 9f), ForeColor = AppColors.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter, Visible = false, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            this.Controls.Add(lblPageInfo);

            btnNext = new AppButton { Text = "NEXT ▶", Type = AppButton.ButtonType.Secondary, Bounds = new Rectangle(COL3_X, ROW_BODY + 124, bW, bH), Visible = false, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnNext.Click += (s, e) => GoPage(_currentPage + 1);
            this.Controls.Add(btnNext);

            btnRecord = new AppButton { Text = "Record", Type = AppButton.ButtonType.Primary, Bounds = new Rectangle(COL3_X, ROW_BODY + 220, bW, bH), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnRecord.Click += BtnRecord_Click;
            this.Controls.Add(btnRecord);

            btnSimpan = new AppButton { Text = "Simpan", Type = AppButton.ButtonType.Primary, Bounds = new Rectangle(COL3_X, ROW_BODY + BODY_H - bH, bW, bH), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnSimpan.Click += BtnSimpan_Click;
            this.Controls.Add(btnSimpan);
        }

        // ─── Footer: dihapus (keterangan tidak digunakan) ───────────────
        private void BuildFooter() { /* Tidak ada footer */ }


        // ═════════════════════════════════════════════════════════════════
        //  DATA SETUP
        // ═════════════════════════════════════════════════════════════════

        private async void SetupFormAsync()
        {
            txtTanggal.InputValue = DateTime.Now.ToString("yyyy/MM/dd");
            try
            {
                _shifts = await _masterDataRepository.GetShiftsAsync() ?? new List<CachedShiftDto>();
                if (_shifts.Count > 0)
                {
                    cmbShift.SetDropdownItems(_shifts.Select(s => s.ShiftName).ToArray());
                    cmbShift.InputValue = _shifts[0].ShiftName;
                }

                if (UserSession.CurrentUser != null)
                {
                    cmbNik.InputType = AppInput.InputTypeEnum.Text;
                    cmbNik.Enabled = false;
                    cmbNik.InputValue = string.IsNullOrWhiteSpace(UserSession.CurrentUser.Nik)
                        ? UserSession.CurrentUser.Username
                        : UserSession.CurrentUser.Nik;
                }

                _machines = await _masterDataRepository.GetMachinesAsync() ?? new List<CachedMachineDto>();

                string machineIdStr = GetMachineId();
                CachedMachineDto autoMachine = null;
                if (int.TryParse(machineIdStr, out int configMachineId))
                    autoMachine = _machines.FirstOrDefault(m => m.MachineId == configMachineId);

                if (autoMachine != null)
                {
                    cmbMesin.InputType = AppInput.InputTypeEnum.Text;
                    cmbMesin.Enabled = false;
                    cmbMesin.InputValue = autoMachine.Code;
                    LoadApplicatorsForMachine(autoMachine.Code);
                }
                else if (_machines.Count > 0)
                {
                    cmbMesin.InputType = AppInput.InputTypeEnum.Dropdown;
                    cmbMesin.Enabled = true;
                    cmbMesin.SetDropdownItems(_machines.Select(m => m.Code).ToArray());
                    cmbMesin.InputValue = _machines[0].Code;
                    cmbMesin.InputValueChanged += (s, e) => LoadApplicatorsForMachine(cmbMesin.InputValue);
                    LoadApplicatorsForMachine(_machines[0].Code);
                }
                else
                {
                    cmbMesin.InputValue = "Mesin tidak ditemukan";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data: " + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadApplicatorsForMachine(string machineCode)
        {
            string excelPath = ResolveExcelPath(machineCode);

            if (string.IsNullOrEmpty(excelPath))
            {
                _sideAApplicators = new List<string>();
                _sideBApplicators = new List<string>();
                RebuildApplicatorRows();
                return;
            }

            if (!System.IO.File.Exists(excelPath))
            {
                MessageBox.Show(
                    $"File Excel tidak ditemukan:\n{excelPath}\n\nPastikan path MasterAplikator sudah benar.",
                    "File Tidak Ditemukan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _sideAApplicators = new List<string>();
                _sideBApplicators = new List<string>();
                RebuildApplicatorRows();
                return;
            }

            var (sideA, sideB) = ApplicatorExcelReader.ReadApplicators(excelPath, machineCode);
            _sideAApplicators = sideA;
            _sideBApplicators = sideB;
            RebuildApplicatorRows();
        }

        private string ResolveExcelPath(string machineCode)
        {
            string excelPath = @"C:\MTC_System\Data\MasterAplikator.xls";
            if (!System.IO.File.Exists(excelPath))
            {
                string fallback = excelPath + "x";
                if (System.IO.File.Exists(fallback)) excelPath = fallback;
            }
            return excelPath;
        }

        // ═════════════════════════════════════════════════════════════════
        //  SIDE + PAGE SWITCHING
        // ═════════════════════════════════════════════════════════════════

        private void SwitchSide(string side)
        {
            SaveCurrentPageJudgments();
            _currentSide = side;
            _currentPage = 0;
            lblSide.Text = $"Sisi  {side}";
            btnSideA.Type = side == "A" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnSideB.Type = side == "B" ? AppButton.ButtonType.Primary : AppButton.ButtonType.Secondary;
            btnSideA.Invalidate();
            btnSideB.Invalidate();
            RebuildApplicatorRows();
        }

        private void GoPage(int newPage)
        {
            int totalPages = TotalPages(CurrentApplicators());
            if (newPage < 0 || newPage >= totalPages) return;
            SaveCurrentPageJudgments();
            _currentPage = newPage;
            RebuildApplicatorRows();
        }

        private void SaveCurrentPageJudgments()
        {
            var jDict = _currentSide == "A" ? _judgmentsA : _judgmentsB;
            var nDict = _currentSide == "A" ? _ngItemsA : _ngItemsB;
            foreach (Control ctrl in pnlApplicatorList.Controls)
            {
                if (ctrl is ApplicatorRowControl row && !string.IsNullOrEmpty(row.ApplicatorCode))
                {
                    jDict[row.ApplicatorCode] = row.Judgment;
                    if (row.Judgment == "NG" && !string.IsNullOrEmpty(row.NgItems))
                        nDict[row.ApplicatorCode] = row.NgItems;
                    else
                        nDict.Remove(row.ApplicatorCode);
                }
            }
        }

        private List<string> CurrentApplicators() =>
            _currentSide == "A" ? _sideAApplicators : _sideBApplicators;

        private int TotalPages(List<string> applicators)
        {
            int count = applicators?.Count ?? 0;
            return count == 0 ? 1 : (int)Math.Ceiling(count / (double)PAGE_SIZE);
        }

        private void RebuildApplicatorRows()
        {
            pnlApplicatorList.SuspendLayout();
            pnlApplicatorList.Controls.Clear();

            var applicators = CurrentApplicators();
            var dict = _currentSide == "A" ? _judgmentsA : _judgmentsB;
            int totalPages = TotalPages(applicators);

            int startIdx = _currentPage * PAGE_SIZE;
            int endIdx = Math.Min(startIdx + PAGE_SIZE, applicators?.Count ?? 0);

            var rows = new List<ApplicatorRowControl>();

            // Data rows
            for (int i = startIdx; i < endIdx; i++)
            {
                var row = new ApplicatorRowControl { Width = COL2_W - 4 };
                row.ApplicatorCode = applicators[i];
                row.IsActive = true;
                if (dict.TryGetValue(applicators[i], out string saved))
                {
                    row.Judgment = saved;
                    if (saved == "NG")
                    {
                        var nDict = _currentSide == "A" ? _ngItemsA : _ngItemsB;
                        if (nDict.TryGetValue(applicators[i], out string ngItems))
                            row.NgItems = ngItems;
                    }
                }
                rows.Add(row);
            }

            // Empty filler rows
            int emptyCount = PAGE_SIZE - rows.Count;
            for (int i = 0; i < emptyCount; i++)
            {
                var row = new ApplicatorRowControl { Width = COL2_W - 4, ApplicatorCode = "", IsActive = false };
                rows.Add(row);
            }

            // Dock=Top reversed → add from end
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                rows[i].Dock = DockStyle.Top;
                pnlApplicatorList.Controls.Add(rows[i]);
            }

            pnlApplicatorList.ResumeLayout();

            // Pagination visibility
            bool multi = totalPages > 1;
            btnPrev.Visible = multi;
            btnNext.Visible = multi;
            lblPageInfo.Visible = multi;
            if (multi)
            {
                lblPageInfo.Text = $"Hal. {_currentPage + 1} / {totalPages}";
                btnPrev.Enabled = _currentPage > 0;
                btnNext.Enabled = _currentPage < totalPages - 1;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  SAVE LOGIC
        // ═════════════════════════════════════════════════════════════════

        private void BtnRecord_Click(object sender, EventArgs e)
        {
            // Cari machineId dari mesin yang sedang dipilih
            string machineCode = cmbMesin.InputValue ?? "";
            int mId = _machines?.FirstOrDefault(m => m.Code == machineCode)?.MachineId ?? 0;

            var historyForm = new ApplicatorPatrolHistoryForm(_repository, mId, machineCode);
            historyForm.ShowDialog(this);
        }

        private async void BtnSimpan_Click(object sender, EventArgs e)
        {
            btnSimpan.Enabled = false;
            btnSimpan.Text = "Menyimpan...";
            try
            {
                await SaveCurrentSideAsync();
                ToastNotification.ShowSuccess("Data patroli aplikator berhasil disimpan!");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSimpan.Enabled = true;
                btnSimpan.Text = "Simpan";
            }
        }

        private async Task SaveCurrentSideAsync()
        {
            SaveCurrentPageJudgments();

            int shiftId = _shifts?.FirstOrDefault(s => s.ShiftName == cmbShift.InputValue)?.ShiftId ?? 0;
            int machineId = _machines?.FirstOrDefault(m => m.Code == cmbMesin.InputValue)?.MachineId ?? 0;

            if (shiftId == 0 || machineId == 0)
                throw new InvalidOperationException("Shift atau Mesin tidak valid!");

            var log = new ApplicatorPatrolLogDto
            {
                PatrolDate = DateTime.Today,
                ShiftId = shiftId,
                UserId = (UserSession.CurrentUser?.UserId > 0) ? (int?)UserSession.CurrentUser.UserId : null,
                OperatorNik = UserSession.CurrentUser?.Nik ?? UserSession.CurrentUser?.Username ?? "",
                MachineId = machineId,
                Side = _currentSide,
                Notes = ""
            };

            var dict = _currentSide == "A" ? _judgmentsA : _judgmentsB;
            var nDict = _currentSide == "A" ? _ngItemsA : _ngItemsB;
            var applicators = CurrentApplicators();

            var details = new List<ApplicatorPatrolDetailDto>();
            foreach (var code in applicators)
            {
                if (string.IsNullOrEmpty(code)) continue;
                string judgment = dict.TryGetValue(code, out string j) ? j : "OK";
                string ngItems = (judgment == "NG" && nDict.TryGetValue(code, out string ni)) ? ni : null;
                details.Add(new ApplicatorPatrolDetailDto { ApplicatorCode = code, Judgment = judgment, NgItems = ngItems });
            }

            if (details.Count == 0)
                throw new InvalidOperationException("Tidak ada aplikator untuk disimpan.");

            await _repository.SavePatrolAsync(log, details);
        }
    }
}
