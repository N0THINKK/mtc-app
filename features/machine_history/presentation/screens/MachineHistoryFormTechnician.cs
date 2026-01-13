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
        }

        private void LoadTicketDetails()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string sql = "SELECT failure_details FROM tickets WHERE ticket_id = @Id";
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

            // Check Sparepart Status every tick (Consider throttling this to every 5s for performance)
            // For demo, every 1s is fine.
            if (_currentTicketId > 0 && DateTime.Now.Second % 5 == 0) // Cek tiap detik ke-0, 5, 10...
            {
                CheckSparepartStatus();
            }
        }

        private void CheckSparepartStatus()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    // Cek apakah ada request yang sudah READY (2)
                    int readyCount = connection.ExecuteScalar<int>(
                        "SELECT COUNT(*) FROM part_requests WHERE ticket_id = @Id AND status_id = 2", 
                        new { Id = _currentTicketId });

                    if (readyCount > 0)
                    {
                        // Ubah tampilan tombol request jadi Notifikasi
                        if (buttonRequestSparepart.Enabled == false && buttonRequestSparepart.Text != "BARANG SIAP DI GUDANG!")
                        {
                            buttonRequestSparepart.Text = "BARANG SIAP DI GUDANG!";
                            buttonRequestSparepart.Type = AppButton.ButtonType.Primary; // Use Primary instead of custom green
                            buttonRequestSparepart.BackColor = AppColors.Success; // Explicit override if needed or rely on Type
                            buttonRequestSparepart.Enabled = true; // Enable click to confirm taken? Or just info.
                        }
                    }
                }
            }
            catch { /* Silent fail */ }
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
            inputNIK = CreateInput("NIK Technician", AppInput.InputTypeEnum.Text, true);

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
            inputProblemCause.SetDropdownItems(new[] {
                "Baut pengunci kendor", "Crimping Dies Aus", "Cutter Blade Kotor",
                "Langkah tidak Stabil", "LM Guide Aus", "Malservo Error",
                "Roll Terminal NG", "Sensor Kotor", "Spring Aus",
                "Spring Patah", "Terminal tidak center"
            });

            // 4. Problem Action
            inputProblemAction = CreateInput("Tindakan Perbaikan (Problem Action)", AppInput.InputTypeEnum.Dropdown, true);
            inputProblemAction.AllowCustomText = true;
            inputProblemAction.SetDropdownItems(new[] {
                "Adjust Diameter Konduktor", "Adjust Langkah Terminal",
                "Ganti Crimping Dies", "Ganti Malservo",
                "Ganti I/O mesin", "Ganti Spring Supporting Stopper",
                "Ganti CFM", "Ganti Cutter Blade",
                "Ganti Cutting Punch", "Ganti Wire Holder",
                "Jig ulang FH11", "Ganti Roll Terminal"
            });

            // 5. Counter Stroke
            inputCounter = CreateInput("Counter Stroke / Blade / Dies", AppInput.InputTypeEnum.Text, false);

            // 6. Sparepart Request (Permintaan Sparepart)
            inputSparepart = CreateInput("Permintaan Sparepart (Sparepart Request)", AppInput.InputTypeEnum.Dropdown, false);
            inputSparepart.AllowCustomText = true;
            LoadParts();
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
            inputCounter.Enabled = enabled;
            
            // Sparepart inputs initially enabled if verified
            inputSparepart.Enabled = enabled;
            
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
                        string sql = "INSERT INTO part_requests (ticket_id, part_name_manual, qty, status_id, requested_at) VALUES (@TicketId, @PartName, 1, 1, NOW())";
                        connection.Execute(sql, new { TicketId = _currentTicketId, PartName = inputSparepart.InputValue });
                    }

                    // Disable controls
                    inputSparepart.Enabled = false;
                    
                    // PERBAIKAN 2: Disable footer button juga (nama variabel disesuaikan)
                    buttonRequestSparepart.Enabled = false;
                    buttonRequestSparepart.BackColor = Color.Gray;

                    MessageBox.Show("Permintaan sparepart berhasil dikirim ke Stock Control.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("Masukkan NIK Teknisi.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try 
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    // Check if user exists and is Technician (assuming role 'Technician' or similar)
                    // We use broad check: allow any valid user for now, or filter by role_id if strict.
                    // Let's assume role_id 2 is Technician based on previous discussion.
                    var tech = connection.QueryFirstOrDefault("SELECT user_id, full_name FROM users WHERE username = @Nik", new { Nik = nik });

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
                        MessageBox.Show("NIK Teknisi tidak ditemukan di database.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            // Validate inputs
            if (!inputProblemCause.ValidateInput() || 
                !inputProblemAction.ValidateInput() || 
                !inputCounter.ValidateInput())
            {
                 MessageBox.Show("Mohon lengkapi data perbaikan.", "Validasi Gagal", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try 
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    // Update Ticket: Status Completed (3), Finish Time, Action Details
                    string sql = @"
                        UPDATE tickets 
                        SET status_id = 3, 
                            technician_finished_at = NOW(),
                            action_details = @Action
                        WHERE ticket_id = @TicketId";
                    
                    // Combine Cause + Action + Counter into one detail string
                    string fullAction = $"Penyebab: {inputProblemCause.InputValue} | Tindakan: {inputProblemAction.InputValue} | Counter: {inputCounter.InputValue}";

                    connection.Execute(sql, new { Action = fullAction, TicketId = _currentTicketId });
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

                this.Close();
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