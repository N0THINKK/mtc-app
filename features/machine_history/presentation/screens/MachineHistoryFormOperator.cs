using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.machine_history.presentation.components;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineHistoryFormOperator : Form
    {
        private List<ModernInputControl> _inputs;
        // Named references for specific logic
        private ModernInputControl inputNIK;
        private ModernInputControl inputApplicator;
        private ModernInputControl inputProblem;
        private ModernInputControl inputProblemType;

        public MachineHistoryFormOperator()
        {
            InitializeComponent();
            SetupInputs();
        }

        private void SetupInputs()
        {
            _inputs = new List<ModernInputControl>();

            // 1. NIK Operator
            inputNIK = CreateInput("NIK Operator", ModernInputControl.InputTypeEnum.Dropdown, true);
            inputNIK.SetDropdownItems(new[] { "12345", "67890" });

            // 2. No. Aplikator
            inputApplicator = CreateInput("No. Aplikator", ModernInputControl.InputTypeEnum.Text, false);

            // 3. Problem Mesin
            inputProblem = CreateInput("Problem Mesin", ModernInputControl.InputTypeEnum.Dropdown, true);
            inputProblem.AllowCustomText = true;
            inputProblem.SetDropdownItems(new[] {
                "Bellmouth tidak standart", "Tergores", "Servo",
                "Fraying Core", "Stripping NG", "Tidak Stripping",
                "Cacat Crimp sisi A", "Cacat Crimp sisi B",
                "Cacat Strip sisi A", "Cacat Strip sisi B",
                "BDCS", "Deformasi Terminal", "Mesin Off",
                "Terminal Crack", "Rear tidak seimbang",
                "Insulation Tidak Tercrimping", "Komputer Mati",
                "Insulation Tercrimping", "CFM mati", "CFM tidak connect",
                "Conveyor tidak berputar", "Seal error", "Seal Sobek",
                "Seal Maju Mundur", "Seal tidak Insert",
                "Jalur Chipping Buntu", "Tekanan Udara NG",
                "Wire Terbelit", "Damage Insulatiom",
                "Kanban Tidak Bisa diBarcode", "Flash", "Cross section NG"
            });

            // 4. Jenis Problem
            inputProblemType = CreateInput("Jenis Problem", ModernInputControl.InputTypeEnum.Dropdown, true);
            inputProblemType.SetDropdownItems(new[] {
                "Aplikator", "Servo", "Cutting / Stripping NG",
                "Rubber Seal", "CPU / Monitor problem", "CFM error", "lainnya"
            });

            // Add all to layout
            mainLayout.Controls.AddRange(_inputs.ToArray());
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
            return input;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Validate all inputs
            bool isValid = true;
            foreach (var input in _inputs)
            {
                if (!input.ValidateInput())
                {
                    isValid = false;
                }
            }

            if (!isValid)
            {
                MessageBox.Show("Mohon lengkapi data yang diperlukan.", "Validasi Gagal", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Prepare Data (Optional: Pass to next form if needed in future)
            var data = new
            {
                NIK = inputNIK.InputValue,
                Applicator = inputApplicator.InputValue,
                Problem = inputProblem.InputValue,
                ProblemType = inputProblemType.InputValue
            };

            // Open Technician Form
            var technicianForm = new MachineHistoryFormTechnician();
            this.Hide(); // Hide current form
            technicianForm.FormClosed += (s, args) => this.Show(); // Show back when closed
            technicianForm.Show();
        }

        private void PanelFooter_Paint(object sender, PaintEventArgs e)
        {
            // Draw top border for footer
            using (var pen = new Pen(Color.FromArgb(230, 230, 230)))
            {
                e.Graphics.DrawLine(pen, 0, 0, panelFooter.Width, 0);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Responsive width for inputs
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