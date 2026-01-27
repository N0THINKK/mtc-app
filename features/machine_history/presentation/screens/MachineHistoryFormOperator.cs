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
        private AppInput inputNIK;
        private AppInput inputShift;
        private AppInput inputApplicator;
        
        // Dynamic Problem List
        private FlowLayoutPanel pnlProblems;
        private AppButton btnAddProblem;
        private List<ProblemInputControl> _problemControls = new List<ProblemInputControl>();
        
        // History Tab Controls
        private MachineHistoryListControl _historyControl;
        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;
        private AppButton _btnFilter;

        public MachineHistoryFormOperator() : this(new MachineHistoryRepository())
        {
        }

        public MachineHistoryFormOperator(IMachineHistoryRepository repository)
        {
            _repository = repository;
            InitializeComponent(); 
            InitializeCustomTabs();
            SetupInputs();
            
            
            // Compact UI
            // this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            // this.StartPosition = FormStartPosition.CenterScreen;
            // this.MinimumSize = new Size(1024, 768);
            this.WindowState = FormWindowState.Maximized; // Full Screen Start

            this.KeyPreview = true;
            this.KeyDown += MachineHistoryFormOperator_KeyDown;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Force layout update after form is loaded and maximized
            this.OnResize(EventArgs.Empty);
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

        private void LoadOperatorsFromDB()
        {
            try { using (var conn = DatabaseHelper.GetConnection()) {
                var niks = conn.Query<string>("SELECT nik FROM users WHERE role_id = 1 AND nik IS NOT NULL ORDER BY nik");
                inputNIK.SetDropdownItems(niks.AsList().ToArray());
            }} catch { }
        }

        private void LoadShiftsFromDB()
        {
            try { using (var conn = DatabaseHelper.GetConnection()) {
                var shifts = conn.Query<string>("SELECT shift_name FROM shifts ORDER BY shift_name");
                inputShift.SetDropdownItems(shifts.AsList().ToArray());
            }} catch { }
        }

        private AppInput CreateInput(string label, AppInput.InputTypeEnum type, bool required)
        {
            var input = new AppInput
            {
                LabelText = label,
                InputType = type,
                IsRequired = required,
                Width = 450, // Slightly wider for consistency with problem panel
                AllowCustomText = (type == AppInput.InputTypeEnum.Dropdown) 
            };
            return input;
        }

        private void SetupInputs()
        {
            // 1. General Info Inputs
            inputNIK = CreateInput("NIK Operator", AppInput.InputTypeEnum.Dropdown, true);
            inputNIK.AllowCustomText = true;
            inputNIK.DropdownOpened += (s, e) => LoadOperatorsFromDB();
            LoadOperatorsFromDB();

            inputShift = CreateInput("Shift", AppInput.InputTypeEnum.Dropdown, true);
            inputShift.AllowCustomText = false;
            LoadShiftsFromDB();

            inputApplicator = CreateInput("No. Aplikator", AppInput.InputTypeEnum.Text, true);
            inputApplicator.CharacterCasing = CharacterCasing.Upper;

            mainLayout.Controls.Add(inputNIK);
            mainLayout.Controls.Add(inputShift);
            mainLayout.Controls.Add(inputApplicator);

            // 2. Dynamic Problem Section
            var lblProblems = new Label 
            {
                Text = "Daftar Kerusakan:", 
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 20, 0, 5)
            };
            mainLayout.Controls.Add(lblProblems);

            pnlProblems = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Width = 460,
                WrapContents = false
            };
            mainLayout.Controls.Add(pnlProblems);

            btnAddProblem = new AppButton
            {
                Text = "+ Tambah Masalah Lain",
                Width = 200,
                Type = AppButton.ButtonType.Secondary,
                Margin = new Padding(0, 5, 0, 20)
            };
            btnAddProblem.Click += (s, e) => AddProblemInput();
            mainLayout.Controls.Add(btnAddProblem);

            // Add Initial Problem Input
            AddProblemInput();
        }

        private void AddProblemInput()
        {
            var problemControl = new ProblemInputControl(_problemControls.Count);
            problemControl.RemoveRequested += (s, e) => RemoveProblemInput(problemControl);
            
            _problemControls.Add(problemControl);
            pnlProblems.Controls.Add(problemControl);
        }

        private void RemoveProblemInput(ProblemInputControl control)
        {
            if (_problemControls.Count <= 1)
            {
                MessageBox.Show("Minimal harus ada satu masalah.", "Info");
                return;
            }
            
            pnlProblems.Controls.Remove(control);
            _problemControls.Remove(control);
            control.Dispose();
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
            // Validate Header Inputs
            if (!inputNIK.ValidateInput() || !inputShift.ValidateInput() || !inputApplicator.ValidateInput())
            {
                MessageBox.Show("Mohon lengkapi data header.", "Validasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate Problems
            foreach (var prob in _problemControls)
            {
                if (!prob.InputType.ValidateInput() || !prob.InputFailure.ValidateInput())
                {
                    MessageBox.Show("Mohon lengkapi detail masalah.", "Validasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try 
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string uuid = Guid.NewGuid().ToString();
                            string dateCode = DateTime.Now.ToString("yyMMdd");
                            string countSql = "SELECT COUNT(*) FROM tickets WHERE DATE(created_at) = CURDATE()";
                            int dailyCount = connection.ExecuteScalar<int>(countSql, transaction: transaction);
                            string displayCode = $"TKT-{dateCode}-{(dailyCount + 1):D3}"; 

                            // Resolve Header IDs
                            int operatorId = 1; 
                            var userCheck = connection.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE nik = @Nik", new { Nik = inputNIK.InputValue }, transaction: transaction);
                            if (userCheck.HasValue) operatorId = userCheck.Value;

                            int machineId = 1;
                            int? shiftId = connection.QueryFirstOrDefault<int?>("SELECT shift_id FROM shifts WHERE shift_name = @Name", new { Name = inputShift.InputValue }, transaction: transaction);

                            // 1. Insert Header Ticket (Note: problem/failure columns are removed/ignored)
                            string insertTicketSql = @"
                                INSERT INTO tickets (ticket_uuid, ticket_display_code, machine_id, shift_id, operator_id, applicator_code, status_id, created_at)
                                VALUES (@Uuid, @Code, @MachineId, @ShiftId, @OpId, @AppCode, 1, NOW());
                                SELECT LAST_INSERT_ID();";

                            long ticketId = connection.ExecuteScalar<long>(insertTicketSql, new {
                                Uuid = uuid, Code = displayCode, MachineId = machineId, ShiftId = shiftId, OpId = operatorId,
                                AppCode = inputApplicator.InputValue
                            }, transaction: transaction);

                            // 2. Insert Problem Details
                            string insertDetailSql = @"
                                INSERT INTO ticket_problems (ticket_id, problem_type_id, problem_type_remarks, failure_id, failure_remarks)
                                VALUES (@TicketId, @TypeId, @TypeRem, @FailId, @FailRem)";

                            foreach (var prob in _problemControls)
                            {
                                int? typeId = connection.QueryFirstOrDefault<int?>("SELECT type_id FROM problem_types WHERE type_name = @Name", new { Name = prob.InputType.InputValue }, transaction: transaction);
                                string typeRem = (!typeId.HasValue) ? prob.InputType.InputValue : null;

                                int? failId = connection.QueryFirstOrDefault<int?>("SELECT failure_id FROM failures WHERE failure_name = @Name", new { Name = prob.InputFailure.InputValue }, transaction: transaction);
                                string failRem = (!failId.HasValue) ? prob.InputFailure.InputValue : null;

                                connection.Execute(insertDetailSql, new {
                                    TicketId = ticketId, TypeId = typeId, TypeRem = typeRem, FailId = failId, FailRem = failRem
                                }, transaction: transaction);
                            }

                            // 3. Update Machine Status
                            connection.Execute("UPDATE machines SET current_status_id = 2 WHERE machine_id = @MachineId", new { MachineId = machineId }, transaction: transaction);

                            transaction.Commit();

                            AutoClosingMessageBox.Show($"Tiket Berhasil Dibuat!\nKode: {displayCode}", "Sukses", 2000);
                            
                            // Reset Form
                            // We can create a new Technician Form for immediate follow up if needed
                            var technicianForm = new MachineHistoryFormTechnician(ticketId);
                            this.Hide(); 
                            technicianForm.FormClosed += (s, args) => this.Show(); 
                            technicianForm.Show();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
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
            if (mainLayout != null)
            {
                int newWidth = mainLayout.ClientSize.Width - 100; // 50px padding each side
                
                foreach (Control c in mainLayout.Controls)
                {
                    // Resize Inputs, Problem Panel, and Add Button
                    if (c is AppInput || c == pnlProblems || c == btnAddProblem)
                    {
                        c.Width = newWidth;
                    }
                }
                
                // Resize children inside Problem Panel
                if (pnlProblems != null)
                {
                    foreach (Control child in pnlProblems.Controls)
                    {
                        child.Width = newWidth - 10; // Slightly smaller to fit inside panel
                    }
                }
            }
        }
    }
}