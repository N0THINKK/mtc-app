using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app;
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
            
            // Enable Enter key navigation
            this.KeyPreview = true;
            this.KeyDown += MachineHistoryFormOperator_KeyDown;
        }

        private void MachineHistoryFormOperator_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // If Save Button is focused, let it act naturally
                if (this.ActiveControl == buttonSave) return;

                // Move focus to next control
                // true, true, true, true = forward, tabStopOnly, nested, wrap
                this.SelectNextControl(this.ActiveControl, true, true, true, true);
                
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
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

            try 
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    // 1. Generate UUID & Ticket Code
                    string uuid = Guid.NewGuid().ToString();
                    string dateCode = DateTime.Now.ToString("yyMMdd");
                    
                    // Get sequence for today (simple approach)
                    string countSql = "SELECT COUNT(*) FROM tickets WHERE DATE(created_at) = CURDATE()";
                    int dailyCount = connection.ExecuteScalar<int>(countSql);
                    string displayCode = $"TKT-{dateCode}-{(dailyCount + 1):D3}"; 

                    // 2. Resolve IDs
                    // Operator: Try to find user by NIK (assuming NIK is username), else default to 1 (System/Admin)
                    // Machine: Default to 1 for now (should come from config)
                    int operatorId = 1; 
                    var userCheck = connection.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE username = @Nik", new { Nik = inputNIK.InputValue });
                    if (userCheck.HasValue) operatorId = userCheck.Value;

                    int machineId = 1; 

                    // 3. Insert to Database and Get ID
                    string insertSql = @"
                        INSERT INTO tickets 
                        (ticket_uuid, ticket_display_code, machine_id, operator_id, failure_details, status_id, created_at)
                        VALUES 
                        (@Uuid, @Code, @MachineId, @OpId, @Details, 1, NOW());
                        SELECT LAST_INSERT_ID();";

                    // Combine Problem Type and Problem Description
                    string fullDetails = $"[{inputProblemType.InputValue}] {inputProblem.InputValue} (Aplikator: {inputApplicator.InputValue})";

                    // Use ExecuteScalar to get the new ID
                    long newTicketId = connection.ExecuteScalar<long>(insertSql, new {
                        Uuid = uuid,
                        Code = displayCode,
                        MachineId = machineId,
                        OpId = operatorId,
                        Details = fullDetails
                    });

                    // Success Feedback
                    MessageBox.Show(
                        $"Tiket Berhasil Dibuat!\n\n" +
                        $"Kode Tiket: {displayCode}\n" +
                        $"Status: MENUNGGU TEKNISI",
                        "Laporan Terkirim",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    
                    // Reset inputs
                    inputProblem.InputValue = "";
                    inputProblemType.InputValue = "";

                    // Open Technician Form with Ticket ID
                    var technicianForm = new MachineHistoryFormTechnician(newTicketId);
                    this.Hide(); 
                    technicianForm.FormClosed += (s, args) => this.Show(); 
                    technicianForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan data ke database:\n{ex.Message}", "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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