using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.machine_history.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineHistoryFormTechnician : AppBaseForm
    {
        private Stopwatch stopwatch;
        private Stopwatch arrivalStopwatch;
        private Timer timer;
        private bool isVerified = false;

        // UI Controls
        private AppInput inputNIK;
        private AppButton buttonVerify;
        
        // Multi-Problem List
        private FlowLayoutPanel pnlProblems;
        private List<TechnicianProblemItemControl> _problemControls = new List<TechnicianProblemItemControl>();

        // Other Inputs
        private AppInput inputCounter;
        private AppInput inputSparepart;
        // Removed duplicate buttons defined in Designer
        private CheckBox chk4M;
        private CheckBox chkTidak4M;
        private AppStarRating ratingOperator;
        private AppInput inputOperatorNote;
        
        private long _currentTicketId;

        public MachineHistoryFormTechnician(long ticketId)
        {
            _currentTicketId = ticketId;
            InitializeComponent();
            SetupStopwatch();
            SetupInputs();
            LoadTicketProblems(); // New logic
            UpdateUIState();
            UpdatePartRequestStatus();

            // Compact UI
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.OnResize(EventArgs.Empty);
        }

        private void LoadTicketProblems()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string sql = @" 
                        SELECT 
                            tp.problem_id,
                            CONCAT(
                                IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                                IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown'))
                            ) AS ProblemInfo
                        FROM ticket_problems tp
                        LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                        LEFT JOIN failures f ON tp.failure_id = f.failure_id
                        WHERE tp.ticket_id = @Id";

                    var problems = connection.Query(sql, new { Id = _currentTicketId });

                    foreach (var p in problems)
                    {
                        var control = new TechnicianProblemItemControl((long)p.problem_id, (string)p.ProblemInfo, isVerified);
                        _problemControls.Add(control);
                        pnlProblems.Controls.Add(control);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat detail masalah: {ex.Message}");
            }
        }

        private void SetupStopwatch()
        {
            arrivalStopwatch = new Stopwatch();
            arrivalStopwatch.Start();

            stopwatch = new Stopwatch();
            timer = new Timer { Interval = 100 };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (arrivalStopwatch != null && arrivalStopwatch.IsRunning)
                labelArrival.Text = arrivalStopwatch.Elapsed.ToString(@"hh\:mm\:ss");

            if (stopwatch != null && stopwatch.IsRunning)
                labelFinished.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");

            if (isVerified && _currentTicketId > 0 && (int)stopwatch.Elapsed.TotalSeconds % 3 == 0)
                UpdatePartRequestStatus();
        }

        private void UpdatePartRequestStatus()
        {
            try { using (var conn = DatabaseHelper.GetConnection()) {
                var request = conn.QueryFirstOrDefault("SELECT status_id FROM part_requests WHERE ticket_id = @Id ORDER BY requested_at DESC", new { Id = _currentTicketId });
                if (request != null) {
                    inputSparepart.Enabled = false;
                    buttonRequestSparepart.Enabled = false;
                    if (request.status_id == 1) { buttonRequestSparepart.Text = "PERMINTAAN DIPROSES"; buttonRequestSparepart.BackColor = Color.Gray; }
                    else if (request.status_id == 2) { buttonRequestSparepart.Text = "BARANG SIAP DI GUDANG"; buttonRequestSparepart.BackColor = AppColors.Success; }
                    else { buttonRequestSparepart.Text = "REQUEST DITUTUP"; buttonRequestSparepart.BackColor = Color.DarkGray; }
                } else {
                    inputSparepart.Enabled = isVerified;
                    buttonRequestSparepart.Enabled = isVerified;
                    buttonRequestSparepart.Text = "Request Sparepart";
                }
            }} catch { }
        }

        private void LoadParts()
        {
            try { using (var conn = DatabaseHelper.GetConnection()) {
                var parts = conn.Query<string>("SELECT CONCAT(IFNULL(part_code, 'N/A'), ' - ', part_name) FROM parts ORDER BY part_name");
                inputSparepart.SetDropdownItems(parts.AsList().ToArray());
            }} catch { }
        }

        private void SetupInputs()
        {
            // 1. Inisial Teknisi
            inputNIK = new AppInput { LabelText = "Inisial Teknisi", InputType = AppInput.InputTypeEnum.Text, IsRequired = true, Width = 450 };
            inputNIK.CharacterCasing = CharacterCasing.Upper;
            mainLayout.Controls.Add(inputNIK);

            buttonVerify = new AppButton { Text = "Cek Teknisi", Type = AppButton.ButtonType.Primary, Margin = new Padding(5, 0, 5, 15) };
            buttonVerify.Click += ButtonVerify_Click;
            mainLayout.Controls.Add(buttonVerify);

            // 2. Dynamic Problem List Container
            var lblProblems = new Label { Text = "Daftar Perbaikan:", Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 10, 0, 5) };
            mainLayout.Controls.Add(lblProblems);

            pnlProblems = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Width = 460, WrapContents = false };
            mainLayout.Controls.Add(pnlProblems);

            // 3. 4M Analysis
            var panel4M = new Panel { AutoSize = true, Margin = new Padding(5, 10, 5, 5), Height = 50 };
            var lbl4M = new Label { Text = "Apakah ada pergantian blade/dies?", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(0, 0), AutoSize = true };
            chk4M = new CheckBox { Text = "Iya", Location = new Point(0, 25), AutoSize = true };
            chkTidak4M = new CheckBox { Text = "Tidak", Location = new Point(80, 25), AutoSize = true };
            
            chk4M.CheckedChanged += (s, e) => { if (chk4M.Checked) chkTidak4M.Checked = false; inputCounter.Enabled = isVerified && chk4M.Checked; if (!chk4M.Checked) inputCounter.InputValue = ""; };
            chkTidak4M.CheckedChanged += (s, e) => { if (chkTidak4M.Checked) chk4M.Checked = false; if (chkTidak4M.Checked) { inputCounter.Enabled = false; inputCounter.InputValue = ""; } };
            
            panel4M.Controls.AddRange(new Control[] { lbl4M, chk4M, chkTidak4M });
            mainLayout.Controls.Add(panel4M);

            // 4. Counter & Sparepart
            inputCounter = new AppInput { LabelText = "Jumlah Counter", InputType = AppInput.InputTypeEnum.Text, Width = 450, IsRequired = false };
            mainLayout.Controls.Add(inputCounter);

            inputSparepart = new AppInput { LabelText = "Permintaan Sparepart", InputType = AppInput.InputTypeEnum.Dropdown, Width = 450, IsRequired = false, AllowCustomText = true };
            mainLayout.Controls.Add(inputSparepart);
            LoadParts();

            // 5. Rating
            var panelReview = new Panel { Width = 450, Height = 60, Margin = new Padding(10, 0, 0, 0) };
            var lblReview = new Label { Text = "Rating Operator:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(3, 0), AutoSize = true };
            ratingOperator = new AppStarRating { Location = new Point(0, 25), Rating = 0 };
            panelReview.Controls.AddRange(new Control[] { lblReview, ratingOperator });
            mainLayout.Controls.Add(panelReview);

            inputOperatorNote = new AppInput { LabelText = "Catatan untuk Operator", InputType = AppInput.InputTypeEnum.Text, Width = 450, IsRequired = false };
            mainLayout.Controls.Add(inputOperatorNote);
        }

        private void UpdateUIState()
        {
            bool enabled = isVerified;
            
            // Enable all problem items
            foreach (var prob in _problemControls)
            {
                prob.SetEnabled(enabled);
            }

            chk4M.Enabled = enabled;
            chkTidak4M.Enabled = enabled;
            inputCounter.Enabled = enabled && chk4M.Checked;
            inputSparepart.Enabled = enabled;
            ratingOperator.ReadOnly = !enabled;
            inputOperatorNote.Enabled = enabled;
            buttonRepairComplete.Enabled = enabled;
            buttonRequestSparepart.Enabled = enabled;

            inputNIK.Enabled = !enabled;
            buttonVerify.Enabled = !enabled;
            buttonVerify.Visible = !enabled; 
        }

        private void ButtonVerify_Click(object sender, EventArgs e)
        {
            string nik = inputNIK.InputValue;
            if (string.IsNullOrWhiteSpace(nik)) { MessageBox.Show("Masukkan Inisial.", "Validasi"); return; }

            try { using (var conn = DatabaseHelper.GetConnection()) {
                conn.Open();
                var tech = conn.QueryFirstOrDefault("SELECT user_id, full_name FROM users WHERE nik = @Nik", new { Nik = nik });
                if (tech != null) {
                    conn.Execute("UPDATE tickets SET status_id = 2, technician_id = @Id, started_at = NOW() WHERE ticket_id = @TId", new { Id = tech.user_id, TId = _currentTicketId });
                    isVerified = true; arrivalStopwatch.Stop(); stopwatch.Start(); timer.Start();
                    AutoClosingMessageBox.Show($"Verifikasi Berhasil!\nSelamat bekerja, {tech.full_name}.", "Sukses", 2000);
                    UpdateUIState();
                } else { MessageBox.Show("Inisial tidak ditemukan."); }
            }} catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void ButtonRequestSparepart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputSparepart.InputValue)) { MessageBox.Show("Isi detail sparepart."); return; }
            if (MessageBox.Show("Lanjutkan request?", "Konfirmasi", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                try { using (var conn = DatabaseHelper.GetConnection()) {
                    conn.Open();
                    int? partId = null; string val = inputSparepart.InputValue;
                    if (val.Contains(" - ")) {
                        var parts = val.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0) partId = conn.QueryFirstOrDefault<int?>("SELECT part_id FROM parts WHERE part_code = @C", new { C = parts[0].Trim() });
                    }
                    if (partId == null) partId = conn.QueryFirstOrDefault<int?>("SELECT part_id FROM parts WHERE part_name = @N", new { N = val });
                    
                    conn.Execute("INSERT INTO part_requests (ticket_id, part_id, part_name_manual, qty, status_id, requested_at) VALUES (@TId, @PId, @Name, 1, 1, NOW())", new { TId = _currentTicketId, PId = partId, Name = val });
                }
                inputSparepart.Enabled = false; buttonRequestSparepart.Enabled = false; buttonRequestSparepart.BackColor = Color.Gray;
                AutoClosingMessageBox.Show("Request terkirim.", "Sukses", 2000); UpdatePartRequestStatus();
                } catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }
        
        private void buttonRequestSparepart_Click(object sender, EventArgs e) => ButtonRequestSparepart_Click(sender, e);

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!chk4M.Checked && !chkTidak4M.Checked) { MessageBox.Show("Pilih 4M / Tidak."); return; }
            
            // Validate Problems
            foreach (var prob in _problemControls)
            {
                if (!prob.InputCause.ValidateInput() || !prob.InputAction.ValidateInput()) { MessageBox.Show("Lengkapi semua detail perbaikan."); return; }
            }

            if (chk4M.Checked && string.IsNullOrWhiteSpace(inputCounter.InputValue)) { inputCounter.SetError("Wajib diisi."); return; }
            if (ratingOperator.Rating == 0) { MessageBox.Show("Beri rating operator."); return; }

            try { using (var conn = DatabaseHelper.GetConnection()) {
                conn.Open();
                using (var trans = conn.BeginTransaction()) {
                    try {
                        // Update Header
                        string sql = @"UPDATE tickets SET status_id = 3, technician_finished_at = NOW(), counter_stroke = @Cnt, is_4m = @Is4M, tech_rating_score = @Sc, tech_rating_note = @Nt WHERE ticket_id = @Id";
                        conn.Execute(sql, new { 
                            Cnt = int.TryParse(inputCounter.InputValue, out int c) ? c : 0, 
                            Is4M = chk4M.Checked ? 1 : 0, 
                            Sc = ratingOperator.Rating, Nt = inputOperatorNote.InputValue, Id = _currentTicketId 
                        }, transaction: trans);

                        // Update Details Loop
                        string detailSql = "UPDATE ticket_problems SET root_cause_id = @CId, root_cause_remarks = @CRem, action_id = @AId, action_details_manual = @ARem WHERE problem_id = @PId";
                        foreach (var prob in _problemControls) {
                            int? cId = conn.QueryFirstOrDefault<int?>("SELECT cause_id FROM failure_causes WHERE cause_name = @N", new { N = prob.InputCause.InputValue }, transaction: trans);
                            int? aId = conn.QueryFirstOrDefault<int?>("SELECT action_id FROM actions WHERE action_name = @N", new { N = prob.InputAction.InputValue }, transaction: trans);
                            
                            conn.Execute(detailSql, new {
                                CId = cId, CRem = (!cId.HasValue ? prob.InputCause.InputValue : null),
                                AId = aId, ARem = (!aId.HasValue ? prob.InputAction.InputValue : null),
                                PId = prob.ProblemId
                            }, transaction: trans);
                        }
                        
                        trans.Commit();
                        stopwatch.Stop(); timer.Stop();
                        AutoClosingMessageBox.Show($"Perbaikan Selesai!\nDurasi: {stopwatch.Elapsed.ToString(@"hh\:mm\:ss")}", "Sukses", 2000);
                        
                        MachineRunForm runForm = new MachineRunForm(_currentTicketId);
                        if (runForm.ShowDialog() == DialogResult.OK) this.Close();
                    } 
                    catch { trans.Rollback(); throw; }
                }
            }} catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void PanelFooter_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(AppColors.Separator)) e.Graphics.DrawLine(pen, 0, 0, panelFooter.Width, 0);
        }
        protected override void OnFormClosing(FormClosingEventArgs e) { timer?.Stop(); timer?.Dispose(); base.OnFormClosing(e); }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (mainLayout != null)
            {
                int newWidth = mainLayout.ClientSize.Width - 100;

                foreach (Control c in mainLayout.Controls)
                {
                    if (c is AppInput || c == pnlProblems || c is Panel || c is AppButton)
                    {
                        c.Width = newWidth;
                    }
                }

                // Resize Problem Items
                if (pnlProblems != null)
                {
                    foreach (Control child in pnlProblems.Controls)
                    {
                        child.Width = newWidth - 10;
                    }
                }
            }
        }
    }
}