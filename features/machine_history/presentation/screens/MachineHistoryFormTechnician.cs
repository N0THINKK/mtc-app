using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.machine_history.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineHistoryFormTechnician : AppBaseForm
    {
        private readonly long _currentTicketId;
        private bool _isVerified = false;
        
        // Stopwatch
        private Stopwatch _arrivalStopwatch;
        private Stopwatch _repairStopwatch;
        private Timer _timer;

        // UI Controls
        private AppInput inputNIK;
        private AppButton btnVerify;
        
        // Multi-Problem List
        private FlowLayoutPanel pnlProblems;
        private Button btnAddProblem;
        private List<TechnicianProblemItemControl> _problemControls = new List<TechnicianProblemItemControl>();

        // Other Inputs
        private AppInput inputCounter;
        private AppInput inputSparepart;
        private CheckBox chk4M;
        private CheckBox chkTidak4M;
        private AppStarRating ratingOperator;
        private AppInput inputOperatorNote;

        public MachineHistoryFormTechnician(long ticketId)
        {
            _currentTicketId = ticketId;
            InitializeComponent();
            SetupStopwatch();
            SetupInputs();
            LoadTicketProblems();
            UpdateUIState();
            
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.OnResize(EventArgs.Empty);
        }

        private void SetupStopwatch()
        {
            _arrivalStopwatch = new Stopwatch();
            _arrivalStopwatch.Start();

            _repairStopwatch = new Stopwatch();
            
            _timer = new Timer { Interval = 100 };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private int _tickCounter = 0;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_arrivalStopwatch?.IsRunning == true)
                labelArrival.Text = _arrivalStopwatch.Elapsed.ToString(@"hh\:mm\:ss");

            if (_repairStopwatch?.IsRunning == true)
                labelFinished.Text = _repairStopwatch.Elapsed.ToString(@"hh\:mm\:ss");

            // [FIX] Poll for part status every 3 seconds (30 ticks * 100ms interval)
            _tickCounter++;
            if (_isVerified && _tickCounter % 30 == 0)
            {
                UpdatePartRequestStatus();
            }
        }

        private void LoadTicketProblems()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            tp.problem_id,
                            COALESCE(pt.type_name, tp.problem_type_remarks, '') AS ProblemType,
                            COALESCE(f.failure_name, tp.failure_remarks, '') AS ProblemDetail
                        FROM ticket_problems tp
                        LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                        LEFT JOIN failures f ON tp.failure_id = f.failure_id
                        WHERE tp.ticket_id = @Id";

                    var problems = conn.Query(sql, new { Id = _currentTicketId });

                    foreach (var p in problems)
                    {
                        var control = new TechnicianProblemItemControl(
                            (long)p.problem_id, 
                            (string)p.ProblemType, 
                            (string)p.ProblemDetail, 
                            _isVerified
                        );
                        _problemControls.Add(control);
                        pnlProblems.Controls.Add(control);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat masalah: {ex.Message}", "Error");
            }
        }

        private void BtnAddProblem_Click(object sender, EventArgs e)
        {
            var control = new TechnicianProblemItemControl(0, "", "", _isVerified); // ID 0 for new
            _problemControls.Add(control);
            pnlProblems.Controls.Add(control);
            
            // Re-apply enabled state logic (Cause/Action disabled if not verified)
            control.SetEnabled(_isVerified);
            
            // Adjust width
            if (mainLayout != null)
            {
                 int contentWidth = mainLayout.ClientSize.Width - 80;
                 if (contentWidth < 400) contentWidth = 400;
                 control.Width = contentWidth;
            }
        }

        private void SetupInputs()
        {
            // === Technician NIK ===
            inputNIK = new AppInput 
            { 
                LabelText = "Inisial Teknisi", 
                InputType = AppInput.InputTypeEnum.Text, 
                IsRequired = true,
                CharacterCasing = CharacterCasing.Upper
            };
            mainLayout.Controls.Add(inputNIK);

            btnVerify = new AppButton 
            { 
                Text = "Verifikasi Teknisi", 
                Type = AppButton.ButtonType.Primary, 
                Margin = new Padding(0, 0, 0, 15)
            };
            btnVerify.Click += BtnVerify_Click;
            mainLayout.Controls.Add(btnVerify);

            // === Problem List ===
            var lblProblems = new Label 
            { 
                Text = "Daftar Perbaikan:", 
                Font = AppFonts.Subtitle, 
                AutoSize = true, 
                Margin = new Padding(0, 10, 0, 5) 
            };
            mainLayout.Controls.Add(lblProblems);

            pnlProblems = new FlowLayoutPanel 
            { 
                FlowDirection = FlowDirection.TopDown, 
                AutoSize = true, 
                WrapContents = false 
            };
            mainLayout.Controls.Add(pnlProblems);

            // === Add Problem Button (Accessible by All) ===
            btnAddProblem = new Button
            {
                Text = "+ Tambah Masalah Lain",
                Size = new Size(200, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.WhiteSmoke,
                ForeColor = AppColors.Primary,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 5, 0, 15)
            };
            btnAddProblem.FlatAppearance.BorderColor = AppColors.Primary;
            btnAddProblem.FlatAppearance.BorderSize = 1;
            btnAddProblem.Click += BtnAddProblem_Click;
            mainLayout.Controls.Add(btnAddProblem);

            // === 4M Analysis ===
            var panel4M = new Panel { AutoSize = true, Height = 50, Margin = new Padding(0, 15, 0, 5) };
            var lbl4M = new Label 
            { 
                Text = "Apakah ada pergantian blade/crimping dies?", 
                Font = AppFonts.Subtitle, 
                Location = new Point(0, 0), 
                AutoSize = true 
            };
            chk4M = new CheckBox { Text = "Iya", Location = new Point(0, 25), AutoSize = true };
            chkTidak4M = new CheckBox { Text = "Tidak", Location = new Point(80, 25), AutoSize = true };
            
            chk4M.CheckedChanged += (s, e) => { 
                if (chk4M.Checked) chkTidak4M.Checked = false; 
                inputCounter.Enabled = _isVerified && chk4M.Checked; 
                if (!chk4M.Checked) inputCounter.InputValue = ""; 
            };
            chkTidak4M.CheckedChanged += (s, e) => { 
                if (chkTidak4M.Checked) { chk4M.Checked = false; inputCounter.Enabled = false; inputCounter.InputValue = ""; } 
            };
            
            panel4M.Controls.AddRange(new Control[] { lbl4M, chk4M, chkTidak4M });
            mainLayout.Controls.Add(panel4M);

            // === Counter ===
            inputCounter = new AppInput { LabelText = "Jumlah Counter", InputType = AppInput.InputTypeEnum.Text, IsRequired = false };
            mainLayout.Controls.Add(inputCounter);

            // === Sparepart ===
            inputSparepart = new AppInput { LabelText = "Permintaan Sparepart", InputType = AppInput.InputTypeEnum.Dropdown, IsRequired = false, AllowCustomText = true };
            mainLayout.Controls.Add(inputSparepart);
            LoadParts();

            // === Rating ===
            var panelRating = new Panel { Width = 450, Height = 60, Margin = new Padding(0, 10, 0, 0) };
            var lblRating = new Label { Text = "Rating Operator:", Font = AppFonts.Subtitle, Location = new Point(0, 0), AutoSize = true };
            ratingOperator = new AppStarRating { Location = new Point(0, 25), Rating = 0 };
            panelRating.Controls.AddRange(new Control[] { lblRating, ratingOperator });
            mainLayout.Controls.Add(panelRating);

            // === Notes ===
            inputOperatorNote = new AppInput { LabelText = "Catatan untuk Operator", InputType = AppInput.InputTypeEnum.Text, IsRequired = false };
            mainLayout.Controls.Add(inputOperatorNote);
            var spacer = new Panel { Height = 30, Width = 10, BackColor = Color.Transparent };
            mainLayout.Controls.Add(spacer);
        }

        private void LoadParts()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var parts = conn.Query<string>("SELECT CONCAT(IFNULL(part_code, 'N/A'), ' - ', part_name) FROM parts ORDER BY part_name");
                    inputSparepart.SetDropdownItems(parts.AsList().ToArray());
                }
            }
            catch { /* Ignore */ }
        }

        private void UpdateUIState()
        {
            bool enabled = _isVerified;
            
            foreach (var prob in _problemControls)
                prob.SetEnabled(enabled);

            chk4M.Enabled = enabled;
            chkTidak4M.Enabled = enabled;
            inputCounter.Enabled = enabled && chk4M.Checked;
            inputSparepart.Enabled = enabled;
            ratingOperator.ReadOnly = !enabled;
            inputOperatorNote.Enabled = enabled;
            buttonRepairComplete.Enabled = enabled;
            buttonRequestSparepart.Enabled = enabled;

            inputNIK.Enabled = !enabled;
            btnVerify.Enabled = !enabled;
            btnVerify.Visible = !enabled;
        }

        private void UpdatePartRequestStatus()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var request = conn.QueryFirstOrDefault("SELECT status_id FROM part_requests WHERE ticket_id = @Id ORDER BY requested_at DESC", new { Id = _currentTicketId });
                    if (request != null)
                    {
                        inputSparepart.Enabled = false;
                        buttonRequestSparepart.Enabled = false;
                        
                        int statusId = (int)request.status_id;
                        if (statusId == 1) { buttonRequestSparepart.Text = "PERMINTAAN DIPROSES"; buttonRequestSparepart.BackColor = Color.Gray; }
                        else if (statusId == 2) { buttonRequestSparepart.Text = "BARANG SIAP DI GUDANG"; buttonRequestSparepart.BackColor = AppColors.Success; }
                        else { buttonRequestSparepart.Text = "REQUEST DITUTUP"; buttonRequestSparepart.BackColor = Color.DarkGray; }
                    }
                    else
                    {
                        inputSparepart.Enabled = _isVerified;
                        buttonRequestSparepart.Enabled = _isVerified;
                        buttonRequestSparepart.Text = "Request Sparepart";
                    }
                }
            }
            catch { /* Ignore */ }
        }

        private void BtnVerify_Click(object sender, EventArgs e)
        {
            string nik = inputNIK.InputValue?.Trim();
            if (string.IsNullOrWhiteSpace(nik))
            {
                MessageBox.Show("Masukkan Inisial Teknisi.", "Validasi");
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var tech = conn.QueryFirstOrDefault("SELECT user_id, full_name FROM users WHERE nik = @Nik", new { Nik = nik });
                    
                    if (tech != null)
                    {
                        conn.Execute("UPDATE tickets SET status_id = 2, technician_id = @Id, started_at = NOW() WHERE ticket_id = @TId", 
                            new { Id = tech.user_id, TId = _currentTicketId });
                        
                        _isVerified = true;
                        _arrivalStopwatch.Stop();
                        _repairStopwatch.Start();
                        
                        AutoClosingMessageBox.Show($"Verifikasi Berhasil!\nSelamat bekerja, {tech.full_name}.", "Sukses", 2000);
                        UpdateUIState();
                    }
                    else
                    {
                        MessageBox.Show("Inisial tidak ditemukan.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void buttonRequestSparepart_Click(object sender, EventArgs e)
        {
            if (!_isVerified) return;
            
            string val = inputSparepart.InputValue?.Trim();
            if (string.IsNullOrWhiteSpace(val))
            {
                MessageBox.Show("Isi detail sparepart.", "Validasi");
                return;
            }

            if (MessageBox.Show("Lanjutkan request sparepart?", "Konfirmasi", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    int? partId = null;
                    
                    if (val.Contains(" - "))
                    {
                        var parts = val.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                            partId = conn.QueryFirstOrDefault<int?>("SELECT part_id FROM parts WHERE part_code = @C", new { C = parts[0].Trim() });
                    }
                    
                    if (partId == null)
                        partId = conn.QueryFirstOrDefault<int?>("SELECT part_id FROM parts WHERE part_name = @N", new { N = val });
                    
                    conn.Execute("INSERT INTO part_requests (ticket_id, part_id, part_name_manual, qty, status_id, requested_at) VALUES (@TId, @PId, @Name, 1, 1, NOW())", 
                        new { TId = _currentTicketId, PId = partId, Name = val });
                    
                    inputSparepart.Enabled = false;
                    buttonRequestSparepart.Enabled = false;
                    buttonRequestSparepart.BackColor = Color.Gray;
                    
                    AutoClosingMessageBox.Show("Request terkirim.", "Sukses", 2000);
                    UpdatePartRequestStatus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!_isVerified)
            {
                MessageBox.Show("Verifikasi teknisi terlebih dahulu.", "Validasi");
                return;
            }
            
            if (!chk4M.Checked && !chkTidak4M.Checked)
            {
                MessageBox.Show("Pilih opsi 4M.", "Validasi");
                return;
            }
            
            foreach (var prob in _problemControls)
            {
                if (!prob.InputProblemType.ValidateInput() || !prob.InputProblemDetail.ValidateInput() ||
                    !prob.InputCause.ValidateInput() || !prob.InputAction.ValidateInput())
                {
                    MessageBox.Show("Lengkapi semua detail perbaikan.", "Validasi");
                    return;
                }
            }

            if (chk4M.Checked && string.IsNullOrWhiteSpace(inputCounter.InputValue))
            {
                inputCounter.SetError("Wajib diisi jika 4M.");
                return;
            }
            
            if (ratingOperator.Rating == 0)
            {
                MessageBox.Show("Beri rating operator.", "Validasi");
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Update Ticket Header
                            string sql = @"UPDATE tickets SET status_id = 3, technician_finished_at = NOW(), 
                                counter_stroke = @Cnt, is_4m = @Is4M, tech_rating_score = @Sc, tech_rating_note = @Nt 
                                WHERE ticket_id = @Id";
                            
                            conn.Execute(sql, new { 
                                Cnt = int.TryParse(inputCounter.InputValue, out int c) ? c : 0, 
                                Is4M = chk4M.Checked ? 1 : 0, 
                                Sc = ratingOperator.Rating, 
                                Nt = inputOperatorNote.InputValue, 
                                Id = _currentTicketId 
                            }, trans);

                            // Update Problem Details (including technician edits to problem type/detail)
                            string detailSql = @"UPDATE ticket_problems SET 
                                problem_type_id = @TId, problem_type_remarks = @TRem,
                                failure_id = @FId, failure_remarks = @FRem,
                                root_cause_id = @CId, root_cause_remarks = @CRem, 
                                action_id = @AId, action_details_manual = @ARem 
                                WHERE problem_id = @PId";
                            
                            foreach (var prob in _problemControls)
                            {
                                // Resolve IDs or use remarks
                                int? tId = conn.QueryFirstOrDefault<int?>("SELECT type_id FROM problem_types WHERE type_name = @N", new { N = prob.InputProblemType.InputValue }, trans);
                                int? fId = conn.QueryFirstOrDefault<int?>("SELECT failure_id FROM failures WHERE failure_name = @N", new { N = prob.InputProblemDetail.InputValue }, trans);
                                int? cId = conn.QueryFirstOrDefault<int?>("SELECT cause_id FROM failure_causes WHERE cause_name = @N", new { N = prob.InputCause.InputValue }, trans);
                                int? aId = conn.QueryFirstOrDefault<int?>("SELECT action_id FROM actions WHERE action_name = @N", new { N = prob.InputAction.InputValue }, trans);
                                
                                conn.Execute(detailSql, new {
                                    TId = tId,
                                    TRem = (!tId.HasValue) ? prob.InputProblemType.InputValue : null,
                                    FId = fId,
                                    FRem = (!fId.HasValue) ? prob.InputProblemDetail.InputValue : null,
                                    CId = cId, 
                                    CRem = (!cId.HasValue) ? prob.InputCause.InputValue : null,
                                    AId = aId, 
                                    ARem = (!aId.HasValue) ? prob.InputAction.InputValue : null,
                                    PId = prob.ProblemId
                                }, trans);
                            }
                            
                            trans.Commit();
                            _repairStopwatch.Stop();
                            _timer.Stop();
                            
                            AutoClosingMessageBox.Show($"Perbaikan Selesai!\nDurasi: {_repairStopwatch.Elapsed:hh\\:mm\\:ss}", "Sukses", 2000);
                            
                            var runForm = new MachineRunForm(_currentTicketId);
                            if (runForm.ShowDialog() == DialogResult.OK)
                                this.Close();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void PanelFooter_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(AppColors.Separator))
                e.Graphics.DrawLine(pen, 0, 0, panelFooter.Width, 0);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            if (mainLayout == null) return;
            
            int contentWidth = mainLayout.ClientSize.Width - 80;
            if (contentWidth < 400) contentWidth = 400;
            
            foreach (Control c in mainLayout.Controls)
            {
                if (c is AppInput || c is AppButton || c == pnlProblems || c is Panel || c == btnAddProblem)
                {
                    c.Width = contentWidth;
                }
            }
            
            if (pnlProblems != null)
            {
                foreach (Control child in pnlProblems.Controls)
                {
                    child.Width = contentWidth;
                }
            }
        }
    }
}