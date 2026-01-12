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
        
        public MachineHistoryFormTechnician()
        {
            InitializeComponent();
            SetupStopwatch();
            SetupInputs();
            UpdateUIState();
        }

        private void SetupStopwatch()
        {
            // Stopwatch kedatangan
            arrivalStopwatch = new Stopwatch();
            arrivalStopwatch.Start(); // langsung jalan saat form dibuka

            // Stopwatch perbaikan
            stopwatch = new Stopwatch();

            timer = new Timer
            {
                Interval = 100
            };
            timer.Tick += Timer_Tick;
            timer.Start(); // timer boleh langsung hidup
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Show elapsed time in the "Selesai Reparasi" / Top Right spot as requested (Stopwatch)
            // Or should it be separate? The requirement says "shows the stopwatch on top right corner".
            // We'll use labelFinished as the stopwatch display for now.
            var elapsed = stopwatch.Elapsed;
            labelFinished.Text = $"{elapsed:hh\\:mm\\:ss}";

            // Kedatangan Teknisi (stopwatch sejak form dibuka)
            if (arrivalStopwatch != null && arrivalStopwatch.IsRunning)
            {
                labelArrival.Text = $"{arrivalStopwatch.Elapsed:hh\\:mm\\:ss}";
            }

            // Durasi Perbaikan (setelah verifikasi)
            if (stopwatch != null && stopwatch.IsRunning)
            {
                labelFinished.Text = $"{stopwatch.Elapsed:hh\\:mm\\:ss}";
            }
        }

        private void SetupInputs()
        {
            _inputs = new List<ModernInputControl>();

            inputNIK = CreateInput("NIK Technician", ModernInputControl.InputTypeEnum.Text, true);

            // 2. Verify Button (Added manually to layout)
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

            // 3. Problem Cause (Penyebab)
            inputProblemCause = CreateInput("Penyebab Masalah (Problem Cause)", ModernInputControl.InputTypeEnum.Text, true);
            
            // 4. Problem Action (Tindakan Perbaikan)
            inputProblemAction = CreateInput("Tindakan Perbaikan (Problem Action)", ModernInputControl.InputTypeEnum.Text, true);

            // 5. Counter Stroke / Counter Blade / Crimping Dies
            inputCounter = CreateInput("Counter Stroke / Blade / Dies", ModernInputControl.InputTypeEnum.Text, false);

            // Add inputs to list for validation later
            // Note: NIK is already added by CreateInput.
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
            // If not verified, disable everything except NIK and Verify
            bool enabled = isVerified;

            inputProblemCause.Enabled = enabled;
            inputProblemAction.Enabled = enabled;
            inputCounter.Enabled = enabled;
            
            buttonRepairComplete.Enabled = enabled;
            buttonRequestSparepart.Enabled = enabled; // Even if logic is skipped, button state follows flow

            inputNIK.Enabled = !enabled;
            buttonVerify.Enabled = !enabled;
            buttonVerify.Visible = !enabled; // Hide after verify? Or just disable. Let's hide to be clean.
        }

        private void ButtonVerify_Click(object sender, EventArgs e)
        {
            string nik = inputNIK.InputValue;

            // Dummy Data Validation
            if (string.IsNullOrWhiteSpace(nik))
            {
                MessageBox.Show("Masukkan NIK Teknisi.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Simple dummy check (Allow specific IDs or just any non-empty for this demo if not specified strict list)
            // Requirement: "you can use data dummy if needed"
            if (nik == "12345" || nik == "admin" || nik.Length >= 3) 
            {
                isVerified = true;
                
                // Stop stopwatch kedatangan
                arrivalStopwatch.Stop();
                stopwatch.Start();
                // timer.Start();

                UpdateUIState();
            }
            else
            {
                MessageBox.Show("NIK Teknisi tidak ditemukan.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e) // This is mapped to buttonRepairComplete
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

            // Stop Timer
            stopwatch.Stop();
            timer.Stop();

            // Show Summary
            string duration = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            string finishTime = DateTime.Now.ToString("HH:mm");
            string arrivalDuration = arrivalStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            
            // "text on top right, it will be RepairFinished" - update label to final time?
            // or keep duration?
            // Let's update label to Finish Time as per "RepairFinished" label name implies
            // But user also said "shows the stopwatch on top right corner".
            // I'll show a message and maybe close the form. 
            
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