using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.features.machine_history.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineHistoryFormOperator : AppBaseForm
    {
        private readonly IMachineHistoryRepository _repository;
        
        // Report Tab Controls
        private List<AppInput> _reportInputs;
        private AppInput inputNIK;
        private AppInput inputApplicator;
        private AppInput inputProblem;
        private AppInput inputProblemType;
        private AppInput inputShift;
        
        // History Tab Controls
        private MachineHistoryListControl _historyControl;
        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;
        private AppButton _btnFilter;

        // Composition Root
        public MachineHistoryFormOperator() : this(new MachineHistoryRepository())
        {
        }

        public MachineHistoryFormOperator(IMachineHistoryRepository repository)
        {
            _repository = repository;
            InitializeComponent(); 
            InitializeCustomTabs();
            SetupInputs();
            
            this.KeyPreview = true;
            this.KeyDown += MachineHistoryFormOperator_KeyDown;
        }

        private void InitializeCustomTabs()
        {
            this.Controls.Remove(this.mainLayout);

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Padding = new Point(10, 5)
            };

            // --- Tab 1: Lapor Kerusakan ---
            var tabReport = new TabPage("Lapor Kerusakan");
            tabReport.BackColor = Color.White;
            
            var reportLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(20),
                AutoScroll = true,
                WrapContents = false
            };
            this.mainLayout = reportLayout; 
            tabReport.Controls.Add(reportLayout);
            tabControl.TabPages.Add(tabReport);

            // --- Tab 2: Riwayat Mesin ---
            var tabHistory = new TabPage("Riwayat Mesin");
            tabHistory.BackColor = Color.White;
            
            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
            
            _dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Top = 15, Left = 10 };
            _dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Top = 15, Left = 140 };
            
            var lblTo = new Label { Text = "s/d", AutoSize = true, Top = 18, Left = 130 }; 
            
            _btnFilter = new AppButton { Text = "Filter", Type = AppButton.ButtonType.Primary, Width = 80, Height = 30, Top = 12, Left = 280 };
            _btnFilter.Click += async (s, e) => await LoadHistoryAsync();

            pnlFilter.Controls.Add(_dtpStart);
            pnlFilter.Controls.Add(lblTo);
            pnlFilter.Controls.Add(_dtpEnd);
            pnlFilter.Controls.Add(_btnFilter);
            
            tabHistory.Controls.Add(pnlFilter);

            _historyControl = new MachineHistoryListControl { Dock = DockStyle.Fill };
            tabHistory.Controls.Add(_historyControl); 
            _historyControl.BringToFront();

            tabControl.TabPages.Add(tabHistory);

            this.Controls.Add(tabControl);
            tabControl.BringToFront();
            this.panelHeader.BringToFront(); 
            this.panelFooter.BringToFront(); 
            
            this.panelFooter.SendToBack(); 
            this.panelHeader.SendToBack(); 
            tabControl.BringToFront(); 
        }

        private void MachineHistoryFormOperator_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.ActiveControl == buttonSave) return;
                this.SelectNextControl(this.ActiveControl, true, true, true, true);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
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

        private void LoadOperatorsFromDB()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    var niks = connection.Query<string>("SELECT nik FROM users WHERE role_id = 1 AND nik IS NOT NULL ORDER BY nik");
                    inputNIK.SetDropdownItems(niks.AsList().ToArray());
                }
            }
            catch { /* Ignore */ }
        }

        private void LoadShiftsFromDB()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    var shifts = connection.Query<string>("SELECT shift_name FROM shifts ORDER BY shift_name");
                    inputShift.SetDropdownItems(shifts.AsList().ToArray());
                }
            }
            catch { /* Ignore */ }
        }

        // REVERTED: Simple CreateInput without manual toggle checkbox logic
        private AppInput CreateInput(string label, AppInput.InputTypeEnum type, bool required)
        {
            var input = new AppInput
            {
                LabelText = label,
                InputType = type,
                IsRequired = required,
                Width = 410,
                // Allow custom text for Dropdowns by default to support manual entry
                AllowCustomText = (type == AppInput.InputTypeEnum.Dropdown) 
            };
            
            // Add directly to layout list
            _reportInputs.Add(input);
            return input;
        }

        private void SetupInputs()
        {
            _reportInputs = new List<AppInput>();

            // 1. NIK Operator
            inputNIK = CreateInput("NIK Operator", AppInput.InputTypeEnum.Dropdown, true);
            inputNIK.AllowCustomText = true;
            inputNIK.DropdownOpened += (s, e) => LoadOperatorsFromDB(); // Real-time refresh
            LoadOperatorsFromDB(); // Initial Load

            // 2. No. Aplikator
            inputApplicator = CreateInput("No. Aplikator", AppInput.InputTypeEnum.Text, false);

            // 3. Problem Mesin
            // Reverted to simple Dropdown with AllowCustomText (handled in CreateInput)
            inputProblem = CreateInput("Problem Mesin", AppInput.InputTypeEnum.Dropdown, true);
            LoadFailuresFromDB();

            // 3.5 Shift (Added)
            inputShift = CreateInput("Shift", AppInput.InputTypeEnum.Dropdown, true);
            inputShift.AllowCustomText = false; // Restrict to list only
            LoadShiftsFromDB();

            // 4. Jenis Problem
            inputProblemType = CreateInput("Jenis Problem", AppInput.InputTypeEnum.Dropdown, true);
            LoadProblemTypesFromDB();
            
            // IMPORTANT: Add controls to the layout so they are VISIBLE
            mainLayout?.Controls.AddRange(_reportInputs.ToArray());
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                var history = await _repository.GetHistoryAsync(_dtpStart.Value, _dtpEnd.Value);
                _historyControl.SetData(history);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat riwayat: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (_reportInputs.Any(i => !i.ValidateInput()))
            {
                MessageBox.Show("Mohon lengkapi data yang diperlukan.", "Validasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try 
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    string uuid = Guid.NewGuid().ToString();
                    string dateCode = DateTime.Now.ToString("yyMMdd");
                    string countSql = "SELECT COUNT(*) FROM tickets WHERE DATE(created_at) = CURDATE()";
                    int dailyCount = connection.ExecuteScalar<int>(countSql);
                    string displayCode = $"TKT-{dateCode}-{(dailyCount + 1):D3}"; 

                    // Resolve IDs
                    int operatorId = 1; 
                    var userCheck = connection.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE nik = @Nik", new { Nik = inputNIK.InputValue });
                    if (userCheck.HasValue) operatorId = userCheck.Value;

                    int machineId = 1;
                    int? shiftId = connection.QueryFirstOrDefault<int?>("SELECT shift_id FROM shifts WHERE shift_name = @Name", new { Name = inputShift.InputValue });
                    
                    int? problemTypeId = connection.QueryFirstOrDefault<int?>("SELECT type_id FROM problem_types WHERE type_name = @Name", new { Name = inputProblemType.InputValue });
                    string problemTypeRemarks = (!problemTypeId.HasValue) ? inputProblemType.InputValue : null;
                    
                    int? failureId = connection.QueryFirstOrDefault<int?>("SELECT failure_id FROM failures WHERE failure_name = @Name", new { Name = inputProblem.InputValue });
                    string failureRemarks = (!failureId.HasValue) ? inputProblem.InputValue : null;

                    string insertSql = @"
                        INSERT INTO tickets (ticket_uuid, ticket_display_code, machine_id, shift_id, operator_id, problem_type_id, problem_type_remarks, failure_id, failure_remarks, applicator_code, status_id, created_at)
                        VALUES (@Uuid, @Code, @MachineId, @ShiftId, @OpId, @TypeId, @TypeRemarks, @FailId, @Remarks, @AppCode, 1, NOW());
                        SELECT LAST_INSERT_ID();";

                    long newTicketId = connection.ExecuteScalar<long>(insertSql, new {
                        Uuid = uuid, Code = displayCode, MachineId = machineId, ShiftId = shiftId, OpId = operatorId,
                        TypeId = problemTypeId, TypeRemarks = problemTypeRemarks, FailId = failureId, Remarks = failureRemarks, AppCode = inputApplicator.InputValue
                    });

                    // Update Machine Status to DOWN (2)
                    connection.Execute("UPDATE machines SET current_status_id = 2 WHERE machine_id = @MachineId", new { MachineId = machineId });

                    MessageBox.Show($"Tiket Berhasil Dibuat!\nKode: {displayCode}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    inputProblem.InputValue = "";
                    inputProblemType.InputValue = "";

                    var technicianForm = new MachineHistoryFormTechnician(newTicketId);
                    this.Hide(); 
                    technicianForm.FormClosed += (s, args) => this.Show(); 
                    technicianForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan data: {ex.Message}", "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PanelFooter_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(AppColors.Separator))
            {
                e.Graphics.DrawLine(pen, 0, 0, panelFooter.Width, 0);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (mainLayout != null && _reportInputs != null)
            {
                foreach (var input in _reportInputs)
                {
                    input.Width = mainLayout.ClientSize.Width - 40;
                }
            }
        }
    }
}
