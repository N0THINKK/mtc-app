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
    public partial class MachineHistoryFormOperator : AppBaseForm
    {
        private List<AppInput> _inputs;
        
        // Named references for specific logic
        private AppInput inputNIK;
        private AppInput inputApplicator;
        private AppInput inputProblem;
        private AppInput inputProblemType;

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
            _inputs = new List<AppInput>();

            // 1. NIK Operator
            inputNIK = CreateInput("NIK Operator", AppInput.InputTypeEnum.Dropdown, true);
            inputNIK.AllowCustomText = true;
            inputNIK.SetDropdownItems(new[] { "12345", "67890" });

            // 2. No. Aplikator
            inputApplicator = CreateInput("No. Aplikator", AppInput.InputTypeEnum.Text, false);

            // 3. Problem Mesin
            inputProblem = CreateInput("Problem Mesin", AppInput.InputTypeEnum.Dropdown, true);
            inputProblem.AllowCustomText = true;
            LoadFailuresFromDB();

            // 4. Jenis Problem
            inputProblemType = CreateInput("Jenis Problem", AppInput.InputTypeEnum.Dropdown, true);
            inputProblemType.AllowCustomText = true;
            LoadProblemTypesFromDB();

            // Add all to layout
            mainLayout.Controls.AddRange(_inputs.ToArray());
        }

        private void LoadFailuresFromDB()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    var failures = connection.Query<string>("SELECT failure_name FROM failures ORDER BY failure_name");
                    inputProblem.SetDropdownItems(failures.AsList().ToArray());
                }
            }
            catch { /* Ignore */ }
        }

        private void LoadProblemTypesFromDB()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    var types = connection.Query<string>("SELECT type_name FROM problem_types ORDER BY type_name");
                    inputProblemType.SetDropdownItems(types.AsList().ToArray());
                }
            }
            catch { /* Ignore */ }
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
                    int operatorId = 1; 
                    var userCheck = connection.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE username = @Nik", new { Nik = inputNIK.InputValue });
                    if (userCheck.HasValue) operatorId = userCheck.Value;

                    int machineId = 1; 

                    // Resolve Problem Type ID & Failure ID
                    int? problemTypeId = connection.QueryFirstOrDefault<int?>("SELECT type_id FROM problem_types WHERE type_name = @Name", new { Name = inputProblemType.InputValue });
                    int? failureId = connection.QueryFirstOrDefault<int?>("SELECT failure_id FROM failures WHERE failure_name = @Name", new { Name = inputProblem.InputValue });

                    string failureRemarks = null;
                    // If failure not found in DB, treat as manual remark
                    if (!failureId.HasValue)
                    {
                        failureRemarks = inputProblem.InputValue;
                    }

                    // 3. Insert to Database and Get ID
                    string insertSql = @"
                        INSERT INTO tickets 
                        (ticket_uuid, ticket_display_code, machine_id, operator_id, 
                         problem_type_id, failure_id, failure_remarks, applicator_code,
                         status_id, created_at)
                        VALUES 
                        (@Uuid, @Code, @MachineId, @OpId, 
                         @TypeId, @FailId, @Remarks, @AppCode,
                         1, NOW());
                        SELECT LAST_INSERT_ID();";

                    // Use ExecuteScalar to get the new ID
                    long newTicketId = connection.ExecuteScalar<long>(insertSql, new {
                        Uuid = uuid,
                        Code = displayCode,
                        MachineId = machineId,
                        OpId = operatorId,
                        TypeId = problemTypeId,
                        FailId = failureId,
                        Remarks = failureRemarks,
                        AppCode = inputApplicator.InputValue
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
            using (var pen = new Pen(AppColors.Separator))
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