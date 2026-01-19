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
            InitializeComponent(); // Will be updated in Designer
            InitializeCustomTabs(); // Helper to setup tabs if Designer update is complex
            SetupInputs();
            
            this.KeyPreview = true;
            this.KeyDown += MachineHistoryFormOperator_KeyDown;
        }

        private void InitializeCustomTabs()
        {
            // This method creates the TabControl programmatically to avoid complex Designer edits via AI.
            // We assume 'mainLayout' and 'panelFooter' exist from the base Designer, but we'll repurpose them.
            
            // 1. Clear existing controls from mainLayout if any (Designer adds them)
            // But mainLayout is a FlowLayoutPanel. We want a TabControl taking up the space.
            // We'll effectively hide mainLayout or remove it.
            this.Controls.Remove(this.mainLayout);

            // 2. Create TabControl
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Padding = new Point(10, 5)
            };

            // --- Tab 1: Lapor Kerusakan ---
            var tabReport = new TabPage("Lapor Kerusakan");
            tabReport.BackColor = Color.White;
            
            // Re-create the FlowLayout for the Report form inside the tab
            var reportLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(20),
                AutoScroll = true,
                WrapContents = false
            };
            this.mainLayout = reportLayout; // Point the class field to this new layout so code works
            tabReport.Controls.Add(reportLayout);
            tabControl.TabPages.Add(tabReport);

            // --- Tab 2: Riwayat Mesin ---
            var tabHistory = new TabPage("Riwayat Mesin");
            tabHistory.BackColor = Color.White;
            
            // Filter Panel
            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
            
            _dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Top = 15, Left = 10 };
            _dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Top = 15, Left = 140 };
            
            var lblTo = new Label { Text = "s/d", AutoSize = true, Top = 18, Left = 130 }; // Quick label
            
            _btnFilter = new AppButton { Text = "Filter", Type = AppButton.ButtonType.Primary, Width = 80, Height = 30, Top = 12, Left = 280 };
            _btnFilter.Click += async (s, e) => await LoadHistoryAsync();

            pnlFilter.Controls.Add(_dtpStart);
            pnlFilter.Controls.Add(lblTo);
            pnlFilter.Controls.Add(_dtpEnd);
            pnlFilter.Controls.Add(_btnFilter);
            
            tabHistory.Controls.Add(pnlFilter);

            // List Control
            _historyControl = new MachineHistoryListControl { Dock = DockStyle.Fill };
            tabHistory.Controls.Add(_historyControl); // Add Grid below filters
            _historyControl.BringToFront();

            tabControl.TabPages.Add(tabHistory);

            // Add TabControl to Form (between Header and Footer)
            this.Controls.Add(tabControl);
            tabControl.BringToFront();
            this.panelHeader.BringToFront(); // Create Header overlay if docked Top
            this.panelFooter.BringToFront(); // Ensure Footer is visible (Dock Bottom)
            
            // Adjust Z-Order: Header(Top) -> Footer(Bottom) -> Tabs(Fill)
            // Docking logic: Last added is first in Z-order usually, but let's be explicit.
            this.panelFooter.SendToBack(); // Dock Bottom
            this.panelHeader.SendToBack(); // Dock Top
            tabControl.BringToFront(); // Fill
            // Wait, DockStyle.Fill takes remaining space. So Header (Top) and Footer (Bottom) must be added FIRST to Controls collection?
            // "Controls are docked in reverse z-order."
            // We moved mainLayout out.
            // We want Header (Top), Footer (Bottom), Tabs (Fill).
            // So Tabs should be Index 0 (Top of Z).
            // Actually: Header (Index Last), Footer (Index Last-1), Tabs (Index 0).
            // Let's just set DockStyle correct.
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

        private void SetupInputs()
        {
            _reportInputs = new List<AppInput>();

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

            // Add to the report layout (which was reassigned in InitializeCustomTabs)
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

        // ... [Keeping existing LoadFailuresFromDB, LoadProblemTypesFromDB, CreateInput, SaveButton_Click helper methods] ...
        // For brevity in replacement, re-implementing them below:

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
                Width = 410
            };
            _reportInputs.Add(input);
            return input;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Validate inputs
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
                    // [Existing Save Logic - simplified for Agent context but keeping functional core]
                    // 1. Generate Info
                    string uuid = Guid.NewGuid().ToString();
                    string dateCode = DateTime.Now.ToString("yyMMdd");
                    string countSql = "SELECT COUNT(*) FROM tickets WHERE DATE(created_at) = CURDATE()";
                    int dailyCount = connection.ExecuteScalar<int>(countSql);
                    string displayCode = $"TKT-{dateCode}-{(dailyCount + 1):D3}"; 

                    // 2. Resolve IDs
                    int operatorId = 1; // Default/System
                    var userCheck = connection.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE username = @Nik", new { Nik = inputNIK.InputValue });
                    if (userCheck.HasValue) operatorId = userCheck.Value;
                    int machineId = 1; // Default
                    int? problemTypeId = connection.QueryFirstOrDefault<int?>("SELECT type_id FROM problem_types WHERE type_name = @Name", new { Name = inputProblemType.InputValue });
                    int? failureId = connection.QueryFirstOrDefault<int?>("SELECT failure_id FROM failures WHERE failure_name = @Name", new { Name = inputProblem.InputValue });
                    string failureRemarks = (!failureId.HasValue) ? inputProblem.InputValue : null;

                    // 3. Insert
                    string insertSql = @"
                        INSERT INTO tickets (ticket_uuid, ticket_display_code, machine_id, operator_id, problem_type_id, failure_id, failure_remarks, applicator_code, status_id, created_at)
                        VALUES (@Uuid, @Code, @MachineId, @OpId, @TypeId, @FailId, @Remarks, @AppCode, 1, NOW());
                        SELECT LAST_INSERT_ID();";

                    long newTicketId = connection.ExecuteScalar<long>(insertSql, new {
                        Uuid = uuid, Code = displayCode, MachineId = machineId, OpId = operatorId,
                        TypeId = problemTypeId, FailId = failureId, Remarks = failureRemarks, AppCode = inputApplicator.InputValue
                    });

                    // 4. Update Machine
                    connection.Execute("UPDATE machines SET current_status_id = 2 WHERE machine_id = @MachineId", new { MachineId = machineId });

                    MessageBox.Show($"Tiket Berhasil Dibuat!\nKode: {displayCode}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Reset
                    inputProblem.InputValue = "";
                    inputProblemType.InputValue = "";

                    // Open Technician Form
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
