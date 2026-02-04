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
using mtc_app.shared.infrastructure;
using mtc_app.shared.data.repositories;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineHistoryFormOperator : AppBaseForm
    {
        private readonly IMachineHistoryRepository _repository;
        private readonly IMasterDataRepository _masterDataRepository;
        
        // Header Inputs
        private AppInput inputNIK;
        private AppInput inputShift;
        private AppInput inputApplicator;
        
        // Dynamic Problem List
        private FlowLayoutPanel pnlProblems;
        private AppButton btnAddProblem;
        private AppButton btnSave;
        private List<ProblemInputControl> _problemControls = new List<ProblemInputControl>();
        
        // History Tab Controls
        private MachineHistoryListControl _historyControl;
        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;
        private AppButton _btnFilter;

        public MachineHistoryFormOperator() : this(ServiceLocator.CreateMachineHistoryRepository()) { }

        public MachineHistoryFormOperator(IMachineHistoryRepository repository)
        {
            _repository = repository;
            _masterDataRepository = ServiceLocator.CreateMasterDataRepository();
            InitializeComponent();
            InitializeCustomTabs();
            SetupInputs();
            
            this.WindowState = FormWindowState.Maximized;
            this.KeyPreview = true;
            this.KeyDown += HandleKeyDown;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.OnResize(EventArgs.Empty);
        }

        private void InitializeCustomTabs()
        {
            this.Controls.Remove(this.mainLayout);

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = AppFonts.Body,
                Padding = new Point(10, 5)
            };

            // === Tab 1: Report Tab ===
            var tabReport = new TabPage("Lapor Kerusakan") { BackColor = AppColors.CardBackground };
            
            var reportLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(30, 20, 30, 20),  // Normal padding, docking handles footer
                AutoScroll = true,
                WrapContents = false
            };
            this.mainLayout = reportLayout;
            tabReport.Controls.Add(reportLayout);
            tabControl.TabPages.Add(tabReport);

            // === Tab 2: History Tab ===
            var tabHistory = new TabPage("Riwayat Mesin") { BackColor = AppColors.CardBackground };
            
            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
            _dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Top = 15, Left = 10 };
            _dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Top = 15, Left = 140 };
            var lblTo = new Label { Text = "s/d", AutoSize = true, Top = 18, Left = 130 };
            _btnFilter = new AppButton { Text = "Filter", Type = AppButton.ButtonType.Primary, Width = 80, Height = 30, Top = 12, Left = 280 };
            _btnFilter.Click += async (s, e) => await LoadHistoryAsync();

            pnlFilter.Controls.AddRange(new Control[] { _dtpStart, lblTo, _dtpEnd, _btnFilter });
            tabHistory.Controls.Add(pnlFilter);

            _historyControl = new MachineHistoryListControl { Dock = DockStyle.Fill };
            tabHistory.Controls.Add(_historyControl);
            _historyControl.BringToFront();
            tabControl.TabPages.Add(tabHistory);

            // Remove all docked controls to rebuild z-order correctly
            this.Controls.Remove(panelHeader);
            this.Controls.Remove(panelFooter);
            
            // Add in correct order: Fill control first, then docked edges
            // In WinForms, controls added LATER have priority for docking
            this.Controls.Add(tabControl);    // Fill - lowest priority
            this.Controls.Add(panelFooter);   // Bottom - medium priority  
            this.Controls.Add(panelHeader);   // Top - highest priority (docks first)
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.ActiveControl == btnSave) return;
                this.SelectNextControl(this.ActiveControl, true, true, true, true);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void SetupInputs()
        {
            // === Header Inputs ===
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

            // === Problem Section ===
            var lblProblems = new Label 
            {
                Text = "Daftar Kerusakan:", 
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 20, 0, 5)
            };
            mainLayout.Controls.Add(lblProblems);

            pnlProblems = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            mainLayout.Controls.Add(pnlProblems);

            btnAddProblem = new AppButton
            {
                Text = "+ Tambah Masalah Lain",
                Width = 200,
                Type = AppButton.ButtonType.Secondary,
                Margin = new Padding(0, 10, 0, 20)  // Normal margin, layout padding handles footer
            };
            btnAddProblem.Click += (s, e) => AddProblemInput();
            mainLayout.Controls.Add(btnAddProblem);

            // Spacer to prevent footer from blocking content
            // Spacer to prevent footer from blocking content
            var spacer = new Panel { Height = 40, Width = 10, BackColor = Color.Transparent };
            mainLayout.Controls.Add(spacer);

            // === Save Button ===
            btnSave = new AppButton 
            { 
                Text = "Panggil Teknisi", 
                Type = AppButton.ButtonType.Primary, 
                Height = 55,
                Dock = DockStyle.Fill,
                Margin = new Padding(10)
            };
            btnSave.Click += SaveButton_Click;
            
            // Add to Footer Panel instead of Main Layout
            panelFooter.Controls.Clear();
            panelFooter.Padding = new Padding(20, 10, 20, 10);
            panelFooter.Controls.Add(btnSave);

            // Add initial problem
            AddProblemInput();
        }

        private AppInput CreateInput(string label, AppInput.InputTypeEnum type, bool required)
        {
            return new AppInput
            {
                LabelText = label,
                InputType = type,
                IsRequired = required,
                AllowCustomText = (type == AppInput.InputTypeEnum.Dropdown)
            };
        }

        private void AddProblemInput()
        {
            var problemControl = new ProblemInputControl(_problemControls.Count);
            problemControl.RemoveRequested += (s, e) => RemoveProblemInput(problemControl);
            
            _problemControls.Add(problemControl);
            pnlProblems.Controls.Add(problemControl);
            
            // Trigger resize to set correct width
            this.OnResize(EventArgs.Empty);
        }

        private void RemoveProblemInput(ProblemInputControl control)
        {
            if (_problemControls.Count <= 1)
            {
                AutoClosingMessageBox.Show("Minimal harus ada satu masalah.", "Info", 1500);
                return;
            }
            
            pnlProblems.Controls.Remove(control);
            _problemControls.Remove(control);
            control.Dispose();
            
            // Renumber remaining problems
            for (int i = 0; i < _problemControls.Count; i++)
            {
                _problemControls[i].UpdateIndex(i);
            }
        }

        private async void LoadOperatorsFromDB()
        {
            try
            {
                var niks = await _masterDataRepository.GetOperatorsAsync();
                inputNIK.SetDropdownItems(niks.ToArray());
            }
            catch { /* Ignore */ }
        }

        private async void LoadShiftsFromDB()
        {
            try
            {
                var shifts = await _masterDataRepository.GetShiftsAsync();
                inputShift.SetDropdownItems(shifts.Select(s => s.ShiftName).ToArray());
            }
            catch { /* Ignore */ }
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
            // Validate Header
            if (!inputNIK.ValidateInput() || !inputShift.ValidateInput() || !inputApplicator.ValidateInput())
            {
                MessageBox.Show("Mohon lengkapi semua data.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate Problems
            foreach (var prob in _problemControls)
            {
                if (!prob.InputType.ValidateInput() || !prob.InputFailure.ValidateInput())
                {
                    MessageBox.Show("Mohon lengkapi detail masalah.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try 
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Generate ticket code
                            string uuid = Guid.NewGuid().ToString();
                            string dateCode = DateTime.Now.ToString("yyMMdd");
                            int dailyCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM tickets WHERE DATE(created_at) = CURDATE()", transaction: trans);
                            string displayCode = $"TKT-{dateCode}-{(dailyCount + 1):D3}";

                            // Resolve IDs
                            int operatorId = conn.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE nik = @Nik", new { Nik = inputNIK.InputValue }, trans) ?? 1;
                            int? shiftId = conn.QueryFirstOrDefault<int?>("SELECT shift_id FROM shifts WHERE shift_name = @Name", new { Name = inputShift.InputValue }, trans);
                            
                            // [FIX] Get Machine ID from Config (Dynamic)
                            int machineId = 1;
                            if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                            {
                                machineId = configId;
                            }

                            // Insert Ticket
                            string insertTicketSql = @"
                                INSERT INTO tickets (ticket_uuid, ticket_display_code, machine_id, shift_id, operator_id, applicator_code, status_id, created_at)
                                VALUES (@Uuid, @Code, @MachineId, @ShiftId, @OpId, @AppCode, 1, NOW());
                                SELECT LAST_INSERT_ID();";

                            long ticketId = conn.ExecuteScalar<long>(insertTicketSql, new {
                                Uuid = uuid, Code = displayCode, MachineId = machineId, ShiftId = shiftId, 
                                OpId = operatorId, AppCode = inputApplicator.InputValue
                            }, trans);

                            // Insert Problems
                            string insertProblemSql = @"
                                INSERT INTO ticket_problems (ticket_id, problem_type_id, problem_type_remarks, failure_id, failure_remarks)
                                VALUES (@TicketId, @TypeId, @TypeRem, @FailId, @FailRem)";

                            foreach (var prob in _problemControls)
                            {
                                int? typeId = conn.QueryFirstOrDefault<int?>("SELECT type_id FROM problem_types WHERE type_name = @N", new { N = prob.InputType.InputValue }, trans);
                                int? failId = conn.QueryFirstOrDefault<int?>("SELECT failure_id FROM failures WHERE failure_name = @N", new { N = prob.InputFailure.InputValue }, trans);

                                conn.Execute(insertProblemSql, new {
                                    TicketId = ticketId,
                                    TypeId = typeId,
                                    TypeRem = (!typeId.HasValue) ? prob.InputType.InputValue : null,
                                    FailId = failId,
                                    FailRem = (!failId.HasValue) ? prob.InputFailure.InputValue : null
                                }, trans);
                            }

                            // Update machine status
                            conn.Execute("UPDATE machines SET current_status_id = 2 WHERE machine_id = @Id", new { Id = machineId }, trans);

                            trans.Commit();

                            AutoClosingMessageBox.Show($"Tiket Berhasil Dibuat!\nKode: {displayCode}", "Sukses", 2000);
                            
                            // Open Technician Form
                            var technicianForm = new MachineHistoryFormTechnician(ticketId);
                            this.Hide(); 
                                                technicianForm.FormClosed += (s, args) => 
                                                {
                                                    // If run successful (OK), close this form too -> returns to Login
                                                    if (technicianForm.DialogResult == DialogResult.OK)
                                                    {
                                                        this.Close();
                                                    }
                                                    else
                                                    {
                                                        this.Show();
                                                    }
                                                };
                                                technicianForm.Show();                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null) msg += $"\nDetails: {ex.InnerException.Message}";
                MessageBox.Show($"Gagal menyimpan: {msg}", "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            
            if (mainLayout == null) return;
            
            int contentWidth = mainLayout.ClientSize.Width - 80;
            if (contentWidth < 400) contentWidth = 400;
            
            foreach (Control c in mainLayout.Controls)
            {
                if (c is AppInput || c is AppButton || c == pnlProblems)
                {
                    c.Width = contentWidth;
                }
            }
            
            // Resize problem controls
            if (pnlProblems != null)
            {
                foreach (Control child in pnlProblems.Controls)
                {
                    child.Width = contentWidth;
                }
            }
        }
    }
}