using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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
        
        // Sparepart section
        private Label labelSparepartTitle;
        private List<SparepartRequestControl> sparepartControls;
        
        public MachineHistoryFormTechnician()
        {
            InitializeComponent();
            SetupStopwatch();
            SetupInputs();
            SetupSparepartSection();
            UpdateUIState();
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

            inputNIK = CreateInput("NIK Technician", ModernInputControl.InputTypeEnum.Text, true);

            buttonVerify = new Button
            {
                Text = "Cek Teknisi",
                AutoSize = true,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 5, 15)
            };
            buttonVerify.FlatAppearance.BorderSize = 0;
            buttonVerify.Click += ButtonVerify_Click;
            mainLayout.Controls.Add(buttonVerify);

            inputProblemCause = CreateInput("Penyebab Masalah (Problem Cause)", ModernInputControl.InputTypeEnum.Text, true);
            inputProblemAction = CreateInput("Tindakan Perbaikan (Problem Action)", ModernInputControl.InputTypeEnum.Text, true);
            inputCounter = CreateInput("Counter Stroke / Blade / Dies", ModernInputControl.InputTypeEnum.Text, false);
        }

        private void SetupSparepartSection()
        {
            sparepartControls = new List<SparepartRequestControl>();
            
            labelSparepartTitle = new Label
            {
                Text = "Sparepart yang Diminta",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                AutoSize = true,
                Margin = new Padding(5, 20, 5, 10),
                Visible = false
            };
            mainLayout.Controls.Add(labelSparepartTitle);
        }

        private ModernInputControl CreateInput(string label, ModernInputControl.InputTypeEnum type, bool required)
        {
            var input = new ModernInputControl
            {
                LabelText = label,
                InputType = type,
                IsRequired = required,
                Width = 410
            };

            _inputs.Add(input);
            mainLayout.Controls.Add(input);
            return input;
        }

        private void UpdateUIState()
        {
            bool enabled = isVerified;

            inputProblemCause.Enabled = enabled;
            inputProblemAction.Enabled = enabled;
            inputCounter.Enabled = enabled;
            
            buttonRepairComplete.Enabled = enabled;
            buttonRequestSparepart.Enabled = enabled;

            inputNIK.Enabled = !enabled;
            buttonVerify.Enabled = !enabled;
            buttonVerify.Visible = !enabled;
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
                stopwatch.Start();
                UpdateUIState();
            }
            else
            {
                MessageBox.Show("NIK Teknisi tidak ditemukan.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonRequestSparepart_Click(object sender, EventArgs e)
        {
            AddSparepartControl();
        }

        private void AddSparepartControl()
        {
            // Show title if first sparepart
            if (sparepartControls.Count == 0)
            {
                labelSparepartTitle.Visible = true;
            }

            var sparepartControl = new SparepartRequestControl();
            sparepartControl.OnRemoveRequested += RemoveSparepartControl;
            
            sparepartControls.Add(sparepartControl);
            mainLayout.Controls.Add(sparepartControl);
            
            // Set the control index for proper ordering
            mainLayout.Controls.SetChildIndex(sparepartControl, mainLayout.Controls.IndexOf(labelSparepartTitle) + 1 + sparepartControls.IndexOf(sparepartControl));
        }

        private void RemoveSparepartControl(SparepartRequestControl control)
        {
            if (sparepartControls.Contains(control))
            {
                sparepartControls.Remove(control);
                mainLayout.Controls.Remove(control);
                control.Dispose();

                // Hide title if no spareparts left
                if (sparepartControls.Count == 0)
                {
                    labelSparepartTitle.Visible = false;
                }
            }
        }

        private bool ValidateRequiredInputs()
        {
            return inputProblemCause.ValidateInput() && 
                   inputProblemAction.ValidateInput() && 
                   inputCounter.ValidateInput();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!ValidateRequiredInputs())
            {
                MessageBox.Show("Mohon lengkapi data perbaikan.", "Validasi Gagal", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            stopwatch.Stop();
            timer.Stop();

            string summary = BuildSummary();
            
            MessageBox.Show(summary, "Laporan Tersimpan", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private string BuildSummary()
        {
            string duration = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            string finishTime = DateTime.Now.ToString("HH:mm");
            
            string summary = $"Perbaikan Selesai!\n\n" +
                           $"Kedatangan: {labelArrival.Text}\n" +
                           $"Selesai: {finishTime}\n" +
                           $"Durasi: {duration}\n\n" +
                           $"Penyebab: {inputProblemCause.InputValue}\n" +
                           $"Tindakan: {inputProblemAction.InputValue}";

            if (sparepartControls.Any())
            {
                summary += "\n\nSparepart yang Diminta:";
                foreach (var control in sparepartControls)
                {
                    summary += $"\n- {control.SparepartName}";
                }
            }

            return summary;
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

    // Separate control for sparepart request
    public class SparepartRequestControl : Panel
    {
        private TextBox textBoxSparepart;
        private Button buttonRemove;
        
        public event Action<SparepartRequestControl> OnRemoveRequested;
        
        public string SparepartName => textBoxSparepart.Text;

        public SparepartRequestControl()
        {
            SetupControl();
        }

        private void SetupControl()
        {
            this.Height = 45;
            this.Width = 410;
            this.Margin = new Padding(5, 5, 5, 10);

            textBoxSparepart = new TextBox
            {
                Location = new Point(0, 10),
                Width = 350,
                Height = 30,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle
            };

            buttonRemove = new Button
            {
                Text = "×",
                Location = new Point(360, 10),
                Width = 40,
                Height = 30,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            buttonRemove.FlatAppearance.BorderSize = 0;
            buttonRemove.Click += (s, e) => OnRemoveRequested?.Invoke(this);

            this.Controls.Add(textBoxSparepart);
            this.Controls.Add(buttonRemove);
        }
    }
}