using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineHistoryFormTechnician : AppBaseForm
    {
        private List<AppInput> _inputs;
        private Stopwatch stopwatch;
        private Stopwatch arrivalStopwatch;
        private Timer timer;
        private bool isVerified = false;

        // Named references for specific logic
        private AppInput inputNIK;
        private AppButton buttonVerify;
        private AppInput inputProblemCause;
        private AppInput inputProblemAction;
        private AppInput inputCounter;
        private AppInput inputSparepart;
        private CheckBox chk4M;
        private CheckBox chkTidak4M;
        private AppStarRating ratingOperator;
        private AppInput inputOperatorNote;
        
        private long _currentTicketId;
        private string _failureDetails;

        public MachineHistoryFormTechnician(long ticketId)
        {
            _currentTicketId = ticketId;
            InitializeComponent();
            LoadTicketDetails();
            SetupStopwatch();
            SetupInputs();
            UpdateUIState();
            UpdatePartRequestStatus();
        }

        private void LoadTicketDetails()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    // Construct readable details from normalized tables
                    string sql = @"
                        SELECT CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), 
                               IF(t.problem_type_remarks IS NOT NULL, CONCAT('[', t.problem_type_remarks, '] '), '')), 
                            CASE 
                                WHEN f.failure_name IS NOT NULL THEN f.failure_name
                                WHEN t.failure_remarks IS NOT NULL THEN t.failure_remarks
                                ELSE 'Belum Diisi'
                            END,
                            IF(t.applicator_code IS NOT NULL AND t.applicator_code != '', CONCAT(' (App: ', t.applicator_code, ')'), '')
                        )
                        FROM tickets t
                        LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
                        LEFT JOIN failures f ON t.failure_id = f.failure_id
                        WHERE t.ticket_id = @Id";

                    _failureDetails = connection.ExecuteScalar<string>(sql, new { Id = _currentTicketId });
                }
            }
            catch (Exception ex)
            {
                // Silent fail or minimal log to avoid disrupting flow if just display info
                Debug.WriteLine($"Failed to load ticket details: {ex.Message}");
            }
        }

        private void SetupStopwatch()
        {
            arrivalStopwatch = new Stopwatch();
            arrivalStopwatch.Start();

            stopwatch = new Stopwatch();
            timer = new Timer
            {
                Interval = 100 // 100ms update rate
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (arrivalStopwatch != null && arrivalStopwatch.IsRunning)
            {
                labelArrival.Text = $"{arrivalStopwatch.Elapsed:hh\\:mm\\:ss}";
            }

            if (stopwatch != null && stopwatch.IsRunning)
            {
                labelFinished.Text = $"{stopwatch.Elapsed:hh\\:mm\\:ss}";
            }

            // Re-introduce periodic check for real-time updates from stock control.
            // Throttled to check every 3 seconds to avoid excessive DB calls.
            if (isVerified && _currentTicketId > 0 && (int)stopwatch.Elapsed.TotalSeconds % 3 == 0)
            {
                UpdatePartRequestStatus();
            }
        }

        private void UpdatePartRequestStatus()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    var request = connection.QueryFirstOrDefault(
                        "SELECT status_id FROM part_requests WHERE ticket_id = @Id ORDER BY requested_at DESC", 
                        new { Id = _currentTicketId });

                    if (request != null)
                    {
                        inputSparepart.Enabled = false;
                        buttonRequestSparepart.Enabled = false;

                        switch (request.status_id)
                        {
                            case 1: // PENDING
                                buttonRequestSparepart.Text = "PERMINTAAN DIPROSES";
                                buttonRequestSparepart.BackColor = Color.Gray;
                                break;
                            case 2: // READY
                                buttonRequestSparepart.Text = "BARANG SIAP DI GUDANG";
                                buttonRequestSparepart.BackColor = AppColors.Success;
                                break;
                            default: // Other statuses
                                buttonRequestSparepart.Text = "REQUEST DITUTUP";
                                buttonRequestSparepart.BackColor = Color.DarkGray;
                                break;
                        }
                    }
                    else
                    {
                        // Enable if verified, disable otherwise
                        inputSparepart.Enabled = isVerified;
                        buttonRequestSparepart.Enabled = isVerified;
                        buttonRequestSparepart.Text = "Request Sparepart";
                        // Reset color if you have a default
                    }
                }
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"Failed to update part request status: {ex.Message}");
                // Don't crash the form, just log it.
            }
        }

        private void LoadParts()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    // Fetch parts: "P-001 - Sensor Proximity"
                    var parts = connection.Query<string>(
                        "SELECT CONCAT(IFNULL(part_code, 'N/A'), ' - ', part_name) FROM parts ORDER BY part_name");
                    
                    inputSparepart.SetDropdownItems(parts.AsList().ToArray());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat list sparepart: {ex.Message}");
            }
        }

        private void SetupInputs()
        {
            _inputs = new List<AppInput>();

            // 1. NIK Technician
            inputNIK = CreateInput("Inisial Teknisi", AppInput.InputTypeEnum.Text, true);

            // 2. Verify Button
            buttonVerify = new AppButton
            {
                Text = "Cek Teknisi",
                Type = AppButton.ButtonType.Primary, // Replaces manual green
                Margin = new Padding(5, 0, 5, 15) // Add spacing below
            };
            // Override color if Primary isn't green enough, but Primary is Blue. 
            // The original was Green (Success). I'll use Success color manually or define a Success Type in AppButton later?
            // AppButton only has Primary, Secondary, Outline, Danger.
            // I'll stick to Primary (Blue) for consistency or Danger (Red).
            // Or I can just manually set BackColor after init if AppButton allows it (it does in OnPaint but Type property might reset it).
            // Let's use Primary for now as it's the main action.
            
            buttonVerify.Click += ButtonVerify_Click;
            mainLayout.Controls.Add(buttonVerify);

            // Display Failure Details (Problem Mesin, Jenis Problem, No Aplikator)
            // Stored in _failureDetails as: "[Type] Problem (Aplikator: ...)"
            if (!string.IsNullOrEmpty(_failureDetails))
            {
                AppLabel labelDetailsTitle = new AppLabel
                {
                    Text = "Detail Kerusakan (Operator):",
                    Type = AppLabel.LabelType.Body, // Bold logic handling? AppLabel Body is Regular.
                    // Need Bold. Let's use Subtitle (Bold).
                    Margin = new Padding(5, 5, 5, 2)
                };
                // AppLabel Subtitle is typically Gray. Let's manually override Font if needed or just use Title.
                labelDetailsTitle.Font = new Font(AppFonts.Body.FontFamily, 9, FontStyle.Bold); 
                labelDetailsTitle.ForeColor = Color.DimGray;
                
                mainLayout.Controls.Add(labelDetailsTitle);

                AppLabel labelDetailsValue = new AppLabel
                {
                    Text = _failureDetails,
                    Type = AppLabel.LabelType.Body,
                    AutoSize = true,
                    MaximumSize = new Size(410, 0), // Match input width
                    Margin = new Padding(5, 0, 5, 15)
                };
                mainLayout.Controls.Add(labelDetailsValue);
            }

            // 3. Problem Cause
            inputProblemCause = CreateInput("Penyebab Masalah (Problem Cause)", AppInput.InputTypeEnum.Dropdown, true);
            inputProblemCause.AllowCustomText = true;

            // 4. Problem Action
            inputProblemAction = CreateInput("Tindakan Perbaikan (Problem Action)", AppInput.InputTypeEnum.Dropdown, true);
            inputProblemAction.AllowCustomText = true;
            
            LoadTechnicianMasterData();

            // 5. 4M Selection (Mutually Exclusive Checkboxes)
            var panel4MSelection = new Panel
            {
                AutoSize = true,
                Margin = new Padding(5, 10, 5, 5)
            };

            var lbl4MTitle = new Label
            {
                Text = "Apakah ada pergantian blade atau crimping dies?",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = AppColors.TextPrimary
            };
            panel4MSelection.Controls.Add(lbl4MTitle);

            chk4M = new CheckBox
            {
                Text = "Iya",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(0, 25),
                AutoSize = true,
                Checked = false
            };
            chk4M.CheckedChanged += (s, e) =>
            {
                if (chk4M.Checked && chkTidak4M.Checked)
                {
                    chkTidak4M.Checked = false; // Uncheck the other
                }
                // Enable Counter only if 4M is checked AND verified
                inputCounter.Enabled = isVerified && chk4M.Checked;
                if (!chk4M.Checked)
                {
                    inputCounter.InputValue = ""; // Clear when unchecked
                }
            };
            panel4MSelection.Controls.Add(chk4M);

            chkTidak4M = new CheckBox
            {
                Text = "Tidak",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(80, 25), // Horizontal spacing
                AutoSize = true,
                Checked = false
            };
            chkTidak4M.CheckedChanged += (s, e) =>
            {
                if (chkTidak4M.Checked && chk4M.Checked)
                {
                    chk4M.Checked = false; // Uncheck the other
                }
                // Disable Counter when Tidak 4M is checked
                if (chkTidak4M.Checked)
                {
                    inputCounter.Enabled = false;
                    inputCounter.InputValue = "";
                }
            };
            panel4MSelection.Controls.Add(chkTidak4M);

            // Set panel height to accommodate checkboxes
            panel4MSelection.Height = 50;
            mainLayout.Controls.Add(panel4MSelection);

            // 6. Counter Stroke (only enabled when 4M is checked)
            inputCounter = CreateInput("Jumlah Counter", AppInput.InputTypeEnum.Text, false);

            // 6. Sparepart Request (Permintaan Sparepart)
            inputSparepart = CreateInput("Permintaan Sparepart (Sparepart Request)", AppInput.InputTypeEnum.Dropdown, false);
            inputSparepart.AllowCustomText = true;
            LoadParts();

            // 7. Operator Review (Rating & Note)
            var panelReview = new Panel 
            { 
                Width = 410, 
                Height = 60, // Reduced from 150
                Margin = new Padding(10, 0, 0, 0) // Reduced top margin
            };

            var lblReview = new Label
            {
                Text = "Rating Operator:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(3, 0), // Slight text indentation to match AppInput label
                AutoSize = true
            };
            panelReview.Controls.Add(lblReview);

            ratingOperator = new AppStarRating
            {
                Location = new Point(0, 25),
                Rating = 0
            };
            panelReview.Controls.Add(ratingOperator);

            inputOperatorNote = new AppInput
            {
                LabelText = "Catatan untuk Operator (Opsional)",
                InputType = AppInput.InputTypeEnum.Text,
                IsRequired = false,
                Width = 410,
                Location = new Point(0, 60)
            };
            // Input location is handled by flow layout if added directly,
            // but we are adding to a standard fixed panel?
            // Wait, mainLayout is FlowLayoutPanel.
            // So adding inputOperatorNote to panelReview is better if we want them grouped.
            // But AppInput is a UserControl, so location within panelReview matters.
            
            // Adjust panel height if needed or let AppInput handle it.
            // AppInput is usually ~60-70px height.
            // Let's add AppInput to mainLayout directly to match styling of other inputs.
            
            // Re-think: Add Review Label + Star Rating as one unit, then Note as another AppInput
            
            // Clean up panelReview usage
            mainLayout.Controls.Add(panelReview); 
            // panelReview only contains Label and Stars now.
            
            // Add Note Input separately
            inputOperatorNote = CreateInput("Catatan untuk Operator", AppInput.InputTypeEnum.Text, false);
            // Override height/multiline if needed, but standard text is fine.
        }

        private void LoadTechnicianMasterData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    // Load Causes
                    var causes = connection.Query<string>("SELECT cause_name FROM failure_causes ORDER BY cause_name");
                    inputProblemCause.SetDropdownItems(causes.AsList().ToArray());

                    // Load Actions
                    var actions = connection.Query<string>("SELECT action_name FROM actions ORDER BY action_name");
                    inputProblemAction.SetDropdownItems(actions.AsList().ToArray());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data master: {ex.Message}");
            }
        }

        private AppInput CreateInput(string label, AppInput.InputTypeEnum type, bool required)
        {
            var input = new AppInput
            {
                LabelText = label,
                InputType = type,
                IsRequired = required,
                Width = 410 // Fixed width for consistency
            };

            _inputs.Add(input);
            mainLayout.Controls.Add(input); // Add to layout
            return input;
        }

        private void UpdateUIState()
        {
            bool enabled = isVerified;

            inputProblemCause.Enabled = enabled;
            inputProblemAction.Enabled = enabled;
            
            // Both 4M checkboxes enabled after verification
            chk4M.Enabled = enabled;
            chkTidak4M.Enabled = enabled;
            
            // Counter only enabled if verified AND 4M checkbox is checked
            inputCounter.Enabled = enabled && chk4M.Checked;
            
            // Sparepart inputs initially enabled if verified
            inputSparepart.Enabled = enabled;

            // Review controls
            ratingOperator.ReadOnly = !enabled;
            inputOperatorNote.Enabled = enabled;
            
            buttonRepairComplete.Enabled = enabled;
            
            // PERBAIKAN 1: Menggunakan nama variabel yang benar dari Designer (buttonRequestSparepart)
            buttonRequestSparepart.Enabled = enabled;

            inputNIK.Enabled = !enabled;
            buttonVerify.Enabled = !enabled;
            buttonVerify.Visible = !enabled; 
        }

        // Method ini dipanggil oleh tombol dynamic (B besar)
        private void ButtonRequestSparepart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputSparepart.InputValue))
            {
                MessageBox.Show("Isi detail permintaan sparepart terlebih dahulu.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Anda tidak bisa merubah sparepart request ketika anda memilih Yes. Lanjutkan?",
                "Konfirmasi Permintaan",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try 
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();

                        // Try to resolve Part ID
                        int? partId = null;
                        string inputValue = inputSparepart.InputValue;
                        string partCode = null;

                        // Parse "P-001 - Sensor Name" -> "P-001"
                        if (inputValue.Contains(" - "))
                        {
                            var parts = inputValue.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0)
                            {
                                partCode = parts[0].Trim();
                                partId = connection.QueryFirstOrDefault<int?>("SELECT part_id FROM parts WHERE part_code = @Code", new { Code = partCode });
                            }
                        }

                        // If not found by code (or manual input), try exact name match just in case
                        if (partId == null)
                        {
                             partId = connection.QueryFirstOrDefault<int?>("SELECT part_id FROM parts WHERE part_name = @Name", new { Name = inputValue });
                        }

                        string sql = "INSERT INTO part_requests (ticket_id, part_id, part_name_manual, qty, status_id, requested_at) VALUES (@TicketId, @PartId, @PartName, 1, 1, NOW())";
                        connection.Execute(sql, new { TicketId = _currentTicketId, PartId = partId, PartName = inputValue });
                    }

                    // Disable controls
                    inputSparepart.Enabled = false;
                    
                    // PERBAIKAN 2: Disable footer button juga (nama variabel disesuaikan)
                    buttonRequestSparepart.Enabled = false;
                    buttonRequestSparepart.BackColor = Color.Gray;

                    MessageBox.Show("Permintaan sparepart berhasil dikirim ke Stock Control.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Immediately update the UI to reflect the new status
                    UpdatePartRequestStatus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal request part: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // PERBAIKAN 3: Menambahkan Event Handler untuk tombol Footer (b kecil)
        // Method ini dipanggil otomatis oleh Designer saat tombol oranye di bawah diklik
        private void buttonRequestSparepart_Click(object sender, EventArgs e)
        {
            // Kita panggil logika yang sama dengan tombol dynamic
            ButtonRequestSparepart_Click(sender, e);
        }

        private void ButtonVerify_Click(object sender, EventArgs e)
        {
            string nik = inputNIK.InputValue;

            if (string.IsNullOrWhiteSpace(nik))
            {
                MessageBox.Show("Masukkan Inisial Teknisi.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try 
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    // The 'nik' column now contains initials, so this logic is correct again.
                    var tech = connection.QueryFirstOrDefault("SELECT user_id, full_name FROM users WHERE nik = @Nik", new { Nik = nik });

                    if (tech != null)
                    {
                        isVerified = true;
                        
                        // Update Ticket status to REPAIRING (2) and set technician_id
                        string updateSql = "UPDATE tickets SET status_id = 2, technician_id = @TechId, started_at = NOW() WHERE ticket_id = @TicketId";
                        connection.Execute(updateSql, new { TechId = tech.user_id, TicketId = _currentTicketId });

                        arrivalStopwatch.Stop();
                        stopwatch.Start();
                        timer.Start();

                        MessageBox.Show($"Verifikasi Berhasil!\nSelamat bekerja, {tech.full_name}.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateUIState();
                    }
                    else
                    {
                        MessageBox.Show("Inisial Teknisi tidak ditemukan di database.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // 1. Validate 4M Selection
            if (!chk4M.Checked && !chkTidak4M.Checked)
            {
                MessageBox.Show("Pilih salah satu: 4M atau Tidak 4M.", "Validasi Gagal", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Validate Standard Inputs
            bool isCauseValid = inputProblemCause.ValidateInput();
            bool isActionValid = inputProblemAction.ValidateInput();
            
            // 3. Validate Conditional Input (Counter)
            bool isCounterValid = true;
            if (chk4M.Checked)
            {
                // Manual check because IsRequired is false by default
                if (string.IsNullOrWhiteSpace(inputCounter.InputValue))
                {
                    inputCounter.SetError("Wajib diisi jika 4M.");
                    isCounterValid = false;
                }
            }

            // 4. Check All
            if (!isCauseValid || !isActionValid || !isCounterValid)
            {
                 MessageBox.Show("Mohon lengkapi data perbaikan.", "Validasi Gagal", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 5. Validate Rating
            if (ratingOperator.Rating == 0)
            {
                MessageBox.Show("Mohon berikan rating (bintang) untuk operator.", "Validasi Gagal", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try 
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Resolve IDs
                    int? causeId = connection.QueryFirstOrDefault<int?>("SELECT cause_id FROM failure_causes WHERE cause_name = @Name", new { Name = inputProblemCause.InputValue });
                    int? actionId = connection.QueryFirstOrDefault<int?>("SELECT action_id FROM actions WHERE action_name = @Name", new { Name = inputProblemAction.InputValue });

                    string causeRemarks = null;
                    if (!causeId.HasValue) causeRemarks = inputProblemCause.InputValue;

                    string actionManual = null;
                    if (!actionId.HasValue) actionManual = inputProblemAction.InputValue;

                    int counter = 0;
                    int.TryParse(inputCounter.InputValue, out counter);
                    
                    // Update Ticket: Status Completed (3), Finish Time, IDs
                    string sql = @"
                        UPDATE tickets 
                        SET status_id = 3, 
                            technician_finished_at = NOW(),
                            root_cause_id = @CauseId,
                            root_cause_remarks = @CauseRem,
                            action_id = @ActionId,
                            action_details_manual = @ActionMan,
                            counter_stroke = @Counter,
                            is_4m = @Is4M,
                            tech_rating_score = @TechScore,
                            tech_rating_note = @TechNote
                        WHERE ticket_id = @TicketId";
                    
                    connection.Execute(sql, new { 
                        CauseId = causeId, 
                        CauseRem = causeRemarks,
                        ActionId = actionId, 
                        ActionMan = actionManual,
                        Counter = counter,
                        Is4M = chk4M.Checked ? 1 : 0,
                        TechScore = ratingOperator.Rating,
                        TechNote = inputOperatorNote.InputValue,
                        TicketId = _currentTicketId 
                    });
                }

                stopwatch.Stop();
                timer.Stop();

                string duration = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                string finishTime = DateTime.Now.ToString("HH:mm");
                
                MessageBox.Show(
                    $"Perbaikan Selesai!\n\n" +
                    $"Kedatangan: {labelArrival.Text}\n" +
                    $"Selesai: {finishTime}\n" +
                    $"Durasi: {duration}\n\n" +
                    "Terima kasih atas kerja keras Anda!",
                    "Laporan Tersimpan",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // --- NEW WORKFLOW: Open Run Machine Screen ---
                MachineRunForm runForm = new MachineRunForm(_currentTicketId);
                if (runForm.ShowDialog() == DialogResult.OK)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PanelFooter_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(AppColors.Separator))
            {
                e.Graphics.DrawLine(pen, 0, 0, panelFooter.Width, 0);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            timer?.Stop();
            timer?.Dispose();
            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (mainLayout != null && _inputs != null)
            {
                foreach (var input in _inputs)
                {
                    input.Width = mainLayout.ClientSize.Width - 40;
                }
            }
        }
    }
}