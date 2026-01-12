using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.machine_history.presentation.components;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineHistoryFormTechnician : Form
    {
        private List<ModernInputControl> _inputs;
        private Stopwatch stopwatch;
        private Stopwatch arrivalStopwatch;
        private Timer timer;
        private bool isVerified = false;

        // Named references for specific logic
        private ModernInputControl inputNIK;
        private Button buttonVerify;
        private ModernInputControl inputProblemCause;
        private ModernInputControl inputProblemAction;
        private ModernInputControl inputCounter;
        private ModernInputControl inputSparepart;
        
        // Reference ke tombol dynamic (tombol yang dibuat lewat koding, bukan designer)
        private Button buttonSendSparepartDynamic; 

        public MachineHistoryFormTechnician()
        {
            InitializeComponent();
            SetupStopwatch();
            SetupInputs();
            UpdateUIState();
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
        }

        private void SetupInputs()
        {
            _inputs = new List<ModernInputControl>();

            // 1. NIK Technician
            inputNIK = CreateInput("NIK Technician", ModernInputControl.InputTypeEnum.Text, true);

            // 2. Verify Button
            buttonVerify = new Button
            {
                Text = "Cek Teknisi",
                AutoSize = true,
                BackColor = Color.FromArgb(40, 167, 69), // Green
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 5, 15) // Add spacing below
            };
            buttonVerify.FlatAppearance.BorderSize = 0;
            buttonVerify.Click += ButtonVerify_Click;
            mainLayout.Controls.Add(buttonVerify);

            // 3. Problem Cause
            inputProblemCause = CreateInput("Penyebab Masalah (Problem Cause)", ModernInputControl.InputTypeEnum.Dropdown, true);
            inputProblemCause.AllowCustomText = true;
            inputProblemCause.SetDropdownItems(new[] {
                "Baut pengunci kendor", "Crimping Dies Aus", "Cutter Blade Kotor",
                "Langkah tidak Stabil", "LM Guide Aus", "Malservo Error",
                "Roll Terminal NG", "Sensor Kotor", "Spring Aus",
                "Spring Patah", "Terminal tidak center"
            });

            // 4. Problem Action
            inputProblemAction = CreateInput("Tindakan Perbaikan (Problem Action)", ModernInputControl.InputTypeEnum.Dropdown, true);
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
            inputCounter = CreateInput("Counter Stroke / Blade / Dies", ModernInputControl.InputTypeEnum.Text, false);

            // 6. Sparepart Request (Permintaan Sparepart)
            inputSparepart = CreateInput("Permintaan Sparepart (Sparepart Request)", ModernInputControl.InputTypeEnum.Text, false);
            inputSparepart.Multiline = true; 

            // 7. Send Sparepart Button (Dynamic Button di dalam Layout)
            // buttonSendSparepartDynamic = new Button
            // {
            //     Text = "Kirimkan Permintaan Sparepart",
            //     Size = new Size(250, 40),
            //     BackColor = Color.FromArgb(255, 152, 0), // Orange
            //     ForeColor = Color.White,
            //     FlatStyle = FlatStyle.Flat,
            //     Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            //     Cursor = Cursors.Hand,
            //     Margin = new Padding(5, 5, 5, 20)
            // };
            // buttonSendSparepartDynamic.FlatAppearance.BorderSize = 0;
            // // Kita arahkan ke logic yang sama
            // buttonSendSparepartDynamic.Click += ButtonSendSparepart_Click; 
            // mainLayout.Controls.Add(buttonSendSparepartDynamic);
        }

        private ModernInputControl CreateInput(string label, ModernInputControl.InputTypeEnum type, bool required)
        {
            var input = new ModernInputControl
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
            
            // Tombol dynamic di dalam layout
            if(buttonSendSparepartDynamic != null) 
                buttonSendSparepartDynamic.Enabled = enabled;

            buttonRepairComplete.Enabled = enabled;
            
            // PERBAIKAN 1: Menggunakan nama variabel yang benar dari Designer (buttonSendSparepartFooter)
            buttonSendSparepartFooter.Enabled = enabled;

            inputNIK.Enabled = !enabled;
            buttonVerify.Enabled = !enabled;
            buttonVerify.Visible = !enabled; 
        }

        // Method ini dipanggil oleh tombol dynamic (B besar)
        private void ButtonSendSparepart_Click(object sender, EventArgs e)
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
                // Disable controls
                inputSparepart.Enabled = false;
                
                // Disable dynamic button
                if (buttonSendSparepartDynamic != null)
                {
                    buttonSendSparepartDynamic.Enabled = false;
                    buttonSendSparepartDynamic.BackColor = Color.Gray;
                }

                // PERBAIKAN 2: Disable footer button juga (nama variabel disesuaikan)
                buttonSendSparepartFooter.Enabled = false;
                buttonSendSparepartFooter.BackColor = Color.Gray;

                MessageBox.Show("Permintaan sparepart berhasil dikirim.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // PERBAIKAN 3: Menambahkan Event Handler untuk tombol Footer (b kecil)
        // Method ini dipanggil otomatis oleh Designer saat tombol oranye di bawah diklik
        private void buttonSendSparepart_Click(object sender, EventArgs e)
        {
            // Kita panggil logika yang sama dengan tombol dynamic
            ButtonSendSparepart_Click(sender, e);
        }

        private void ButtonVerify_Click(object sender, EventArgs e)
        {
            string nik = inputNIK.InputValue;

            if (string.IsNullOrWhiteSpace(nik))
            {
                MessageBox.Show("Masukkan NIK Teknisi.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (nik == "12345" || nik == "admin" || nik.Length >= 3) 
            {
                isVerified = true;

                arrivalStopwatch.Stop();
                // labelArrival.Text = DateTime.Now.ToString("HH:mm");
                stopwatch.Start();
                timer.Start();

                UpdateUIState();
            }
            else
            {
                MessageBox.Show("NIK Teknisi tidak ditemukan.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            stopwatch.Stop();
            timer.Stop();

            string duration = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            string finishTime = DateTime.Now.ToString("HH:mm");
            
            MessageBox.Show(
                $"Perbaikan Selesai!\n\n" +
                $"Kedatangan: {labelArrival.Text}\n" +
                $"Selesai: {finishTime}\n" +
                $"Durasi: {duration}\n\n" +
                $"Penyebab: {inputProblemCause.InputValue}\n" +
                $"Tindakan: {inputProblemAction.InputValue}",
                "Laporan Tersimpan",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            this.Close();
        }

        private void PanelFooter_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(230, 230, 230)))
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