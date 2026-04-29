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
using mtc_app.shared.data.session;
using mtc_app.features.authentication.presentation.screens; 
using mtc_app.features.applicator_patrol.data.services;
using System.IO;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineHistoryFormOperator : AppBaseForm
    {
        private readonly IMachineHistoryRepository _repository;
        private readonly IMasterDataRepository _masterDataRepository;
        
        // Excel paths for MasterAplikator (same as applicator patrol)
        private static readonly Dictionary<string, string> ExcelPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "AC90", @"C:\AC90HMI\MasterAplikator.xls" },
            { "AC95", @"D:\AC95HMI\MasterAplikator.xls" },
            { "AC81", @"" }
        };
        
        // Header Inputs
        private AppInput inputOperatorNik; // [BARU] Input untuk NIK Operator
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
        private ComboBox _cmbArea;
        private AppButton _btnFilter;

        // Pending Ticket Indicator
        private TabControl _tabControl;
        private LinkLabel _lnkPendingTicket;
        private MachineHistoryDto _pendingTicket;

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

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.OnResize(EventArgs.Empty);

            await Task.Delay(50); 

            LoadAreas();
            LoadApplicatorsFromExcel();
            await CheckForPendingTicketAsync();
        }

        // Main Responsive Layout Containers
        private TableLayoutPanel _rootLayout;
        private TableLayoutPanel _tab1Layout;
        private FlowLayoutPanel _formLayout; 
        private Panel _problemsLayout; 

        private void InitializeCustomTabs()
        {
            this.Controls.Clear(); 

            // === 1. Root Layout (Header, Content) ===
            _rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = AppColors.Background
            };
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); 
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); 

            panelHeader.Dock = DockStyle.Fill;
            _rootLayout.Controls.Add(panelHeader, 0, 0);

            // === 2. Tab Control (Content) ===
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = AppFonts.Body,
                Padding = new Point(10, 5)
            };
            _rootLayout.Controls.Add(_tabControl, 0, 1);
            this.Controls.Add(_rootLayout);

            // === Tab 1: Report Tab ===
            var tabReport = new TabPage("Lapor Kerusakan") { BackColor = AppColors.CardBackground };

            _tab1Layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20, 10, 0, 10) 
            };
            _tab1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _tab1Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); 
            _tab1Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    

            _formLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 0, 20, 0) 
            };
            _formLayout.Resize += (s, e) => 
            {
                int newWidth = _formLayout.ClientSize.Width - 20;
                if (newWidth < 100) return; // Prevent collapse

                foreach (Control c in _formLayout.Controls)
                {
                    if (c == _problemsLayout)
                    {
                        c.Width = newWidth;
                        foreach (Control pc in _problemsLayout.Controls)
                        {
                            pc.Width = newWidth;
                        }
                    }
                    else
                    {
                        c.Width = newWidth;
                    }
                }
            };
            _tab1Layout.Controls.Add(_formLayout, 0, 0);

            panelFooter.Parent = null; 
            panelFooter.Controls.Clear();
            panelFooter.Dock = DockStyle.Fill;
            panelFooter.Height = 80; 
            panelFooter.Padding = new Padding(0); 
            _tab1Layout.Controls.Add(panelFooter, 0, 1);

            tabReport.Controls.Add(_tab1Layout);
            _tabControl.TabPages.Add(tabReport);

            // === Tab 2: History Tab ===
            var tabHistory = new TabPage("Riwayat Mesin") { BackColor = AppColors.CardBackground };
            var pnlFilter = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Top, 
                Height = 60, 
                Padding = new Padding(10, 15, 10, 10),
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = false
            };
            
            _dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
            var lblTo = new Label { Text = "s/d", AutoSize = true, Margin = new Padding(5, 5, 5, 0) }; 
            _dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
            
            var lblArea = new Label { Text = "Area:", AutoSize = true, Margin = new Padding(10, 5, 5, 0) };
            _cmbArea = new ComboBox 
            { 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                Width = 100,
                Font = AppFonts.BodySmall
            };
            _cmbArea.Items.Add("Semua"); 
            _cmbArea.SelectedIndex = 0;

            _btnFilter = new AppButton { Text = "Filter", Type = AppButton.ButtonType.Primary, Width = 80, Height = 30, Margin = new Padding(10, 0, 0, 0) };
            _btnFilter.Click += async (s, e) => await LoadHistoryAsync();

            pnlFilter.Controls.AddRange(new Control[] { _dtpStart, lblTo, _dtpEnd, lblArea, _cmbArea, _btnFilter });
            tabHistory.Controls.Add(pnlFilter);

            _historyControl = new MachineHistoryListControl { Dock = DockStyle.Fill };
            _historyControl.ItemClicked += HistoryControl_ItemClicked;
            tabHistory.Controls.Add(_historyControl);
            _historyControl.BringToFront();

            _tabControl.TabPages.Add(tabHistory);

            _tabControl.SelectedIndexChanged += async (s, e) =>
            {
                if (_tabControl.SelectedTab == tabHistory)
                {
                    await LoadHistoryAsync();
                }
            };

            _lnkPendingTicket = new LinkLabel
            {
                Text = "⚠️ CONTINUE PROBLEM",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold), 
                LinkColor = Color.Gold, 
                ActiveLinkColor = Color.Yellow,
                LinkBehavior = LinkBehavior.HoverUnderline,
                AutoSize = true,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent 
            };
            panelHeader.Controls.Add(_lnkPendingTicket);
            RepositionPendingLink(); 
            _lnkPendingTicket.LinkClicked += LnkPendingTicket_LinkClicked;
        }
        
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RepositionPendingLink();
        }

        private void RepositionPendingLink()
        {
            if (_lnkPendingTicket != null && panelHeader != null)
            {
                _lnkPendingTicket.Location = new Point(panelHeader.Width - _lnkPendingTicket.Width - 5, 15);
            }
        }

        private void SetupInputs()
        {
            void AddToForm(Control c, int bottomMargin = 10)
            {
                 c.Width = _formLayout.ClientSize.Width > 0 ? _formLayout.ClientSize.Width - 20 : 500;
                 c.Margin = new Padding(0, 0, 0, bottomMargin);
                 _formLayout.Controls.Add(c);
            }

            // 1. NIK Operator [BARU]
            inputOperatorNik = CreateInput("NIK Operator", AppInput.InputTypeEnum.Text, true);
            // Mengisi otomatis NIK dari UserSession jika login
            if (UserSession.IsLoggedIn)
            {
                inputOperatorNik.InputValue = UserSession.CurrentUser?.Username ?? "";
            }
            AddToForm(inputOperatorNik);

            // 2. Shift
            inputShift = CreateInput("Shift", AppInput.InputTypeEnum.Dropdown, true);
            inputShift.AllowCustomText = false;
            LoadShiftsFromDB();
            AddToForm(inputShift);

            // 3. Applicator (Dropdown dari prdmst.csv, bisa kosong)
            inputApplicator = CreateInput("No. Aplikator", AppInput.InputTypeEnum.Dropdown, false);
            inputApplicator.AllowCustomText = true;
            AddToForm(inputApplicator);

            // 4. Problems Label
            var lblProblems = new Label 
            {
                Text = "Daftar Kerusakan:", 
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 20, 0, 5)
            };
            AddToForm(lblProblems);

            // 5. Problems Container — auto-sizing FlowLayoutPanel
            _problemsLayout = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            
            AddToForm(_problemsLayout, 0);

            // 6. Add Problem Button
            btnAddProblem = new AppButton
            {
                Text = "+ Tambah Problem Lain",
                Width = 200, 
                Height = 40,
                Type = AppButton.ButtonType.Secondary,
                Margin = new Padding(0, 10, 0, 20)
            };
            btnAddProblem.Click += (s, e) => AddProblemInput();
            AddToForm(btnAddProblem);

            // === Footer Action Button ===
             btnSave = new AppButton 
            { 
                Text = "Panggil Teknisi", 
                Type = AppButton.ButtonType.Primary, 
                Height = 55,
                Dock = DockStyle.Fill,
                Margin = new Padding(10) 
            };
            btnSave.Click += SaveButton_Click;
            
            panelFooter.Controls.Add(btnSave);

            AddProblemInput();
        }

        private const int ProblemRowHeight = 250;

        private void AddProblemInput()
        {
            var problemControl = new ProblemInputControl(_problemControls.Count);
            problemControl.RemoveRequested += (s, e) => RemoveProblemInput(problemControl);
            problemControl.Margin = new Padding(0, 0, 0, 0); 
            
            _problemControls.Add(problemControl);
            
            problemControl.Width = _formLayout.ClientSize.Width > 20 ? _formLayout.ClientSize.Width - 20 : 500;
            _problemsLayout.Controls.Add(problemControl);
        }

        private void RemoveProblemInput(ProblemInputControl control)
        {
            if (_problemControls.Count <= 1)
            {
                AutoClosingMessageBox.Show("Minimal harus ada satu problem.", "Info", 1500);
                return;
            }
            
            _problemControls.Remove(control);
            _problemsLayout.Controls.Remove(control);
            control.Dispose();
            
            // Re-position remaining controls
            for (int i = 0; i < _problemControls.Count; i++)
            {
                _problemControls[i].UpdateIndex(i);
            }
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

        private async void LoadShiftsFromDB()
        {
            try
            {
                var shifts = await _masterDataRepository.GetShiftsAsync();
                inputShift.SetDropdownItems(shifts.Select(s => s.ShiftName).ToArray());
            }
            catch { /* Ignore */ }
        }

        private void LoadApplicatorsFromExcel()
        {
            try
            {
                // Resolve machine code from config
                string machineCode = "";
                if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                {
                    var machines = Task.Run(() => _masterDataRepository.GetMachinesAsync()).Result;
                    var machine = machines?.FirstOrDefault(m => m.MachineId == configId);
                    if (machine != null) machineCode = machine.Code ?? "";
                }

                // Find matching Excel path
                string excelPath = null;
                foreach (var key in ExcelPaths.Keys)
                {
                    if (!string.IsNullOrEmpty(machineCode) && machineCode.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                    {
                        string configured = ExcelPaths[key];
                        if (!string.IsNullOrEmpty(configured))
                        {
                            excelPath = Path.IsPathRooted(configured) ? configured : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configured);
                            if (!File.Exists(excelPath) && excelPath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                            {
                                string fallback = excelPath + "x";
                                if (File.Exists(fallback)) excelPath = fallback;
                            }
                        }
                        break;
                    }
                }

                if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath)) return;

                // Read applicators from MasterAplikator.xls
                var (sideA, sideB) = ApplicatorExcelReader.ReadApplicators(excelPath, machineCode);
                var allApplicators = sideA.Union(sideB).OrderBy(x => x).ToArray();

                if (allApplicators.Length > 0)
                {
                    inputApplicator.SetDropdownItems(allApplicators);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MachineHistory] Error loading applicators: {ex.Message}");
            }
        }

        private async void LoadAreas()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var areas = await conn.QueryAsync<string>("SELECT area_name FROM machine_areas WHERE area_name != 'Lain2' ORDER BY area_name");
                    foreach (var area in areas)
                    {
                        if (!_cmbArea.Items.Contains(area)) 
                            _cmbArea.Items.Add(area);
                    }
                }
            }
            catch { /* Ignore */ }
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                string areaFilter = null;
                if (_cmbArea.SelectedItem != null && _cmbArea.SelectedItem.ToString() != "Semua")
                {
                    areaFilter = _cmbArea.SelectedItem.ToString();
                }

                int? machineId = null;
                if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                {
                    machineId = configId;
                }

                var startDate = _dtpStart.Value.Date;
                var endDate = _dtpEnd.Value.Date.AddDays(1).AddTicks(-1);

                var history = await _repository.GetHistoryAsync(startDate, endDate, null, areaFilter, machineId);
                _historyControl.SetData(history);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat riwayat: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            // [BARU] Validasi untuk input NIK Operator juga
            if (!inputOperatorNik.ValidateInput() || !inputShift.ValidateInput())
            {
                MessageBox.Show("Mohon lengkapi data wajib (NIK Operator dan Shift).", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var prob in _problemControls)
            {
                if (!prob.InputType.ValidateInput() || !prob.InputFailure.ValidateInput())
                {
                    MessageBox.Show("Mohon lengkapi detail problem.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try 
            {
                int machineId = 1;
                if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                {
                    machineId = configId;
                }

                var request = new CreateTicketRequest
                {
                    // [BARU] Mengambil NIK Operator dari form input, bukan dari session langsung
                    OperatorNik = inputOperatorNik.InputValue, 
                    ShiftName = inputShift.InputValue,
                    ApplicatorCode = inputApplicator.InputValue,
                    MachineId = machineId,
                    Problems = _problemControls.Select(p => new TicketProblemRequest 
                    { 
                        ProblemTypeName = p.InputType.InputValue,
                        FailureName = p.InputFailure.InputValue 
                    }).ToList()
                };

                var result = await _repository.CreateTicketAsync(request);

                string successMsg = (result.TicketId < 0) 
                    ? "Tiket Disimpan Offline.\nMenunggu Sinkronisasi." 
                    : $"Tiket Berhasil Dibuat!\nKode: {result.TicketCode}";

                AutoClosingMessageBox.Show(successMsg, "Sukses", 2000);

                OpenTechnicianForm(result.TicketId);
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

        private async Task CheckForPendingTicketAsync()
        {
            try
            {
                int machineId = 1;
                if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                {
                    machineId = configId;
                }

                _pendingTicket = await _repository.GetActiveTicketForMachineAsync(machineId);
                
                if (_pendingTicket != null)
                {
                    _lnkPendingTicket.Text = $"⚠️ CONTINUE PROBLEM ({_pendingTicket.StatusName.ToUpper()})";
                    _lnkPendingTicket.Visible = true;
                    // Recalculate position after text change
                    RepositionPendingLink();
                }
                else
                {
                    _lnkPendingTicket.Visible = false;
                }
            }
            catch
            {
                _lnkPendingTicket.Visible = false;
            }
        }

        private async void LnkPendingTicket_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_pendingTicket == null) return;

            _tabControl.SelectedIndex = 1;
            await LoadHistoryAsync();
            OpenTechnicianForm(_pendingTicket.TicketId);
        }

        private void HistoryControl_ItemClicked(object sender, MachineHistoryDto item)
        {
            if (item.StatusId == 1 || item.StatusId == 2)
            {
                OpenTechnicianForm(item.TicketId);
            }
        }

        private void OpenTechnicianForm(long ticketId)
        {
            var technicianForm = new MachineHistoryFormTechnician(ticketId);
            this.Hide();
            
            // Current form hides, waits for technician form to close
            technicianForm.FormClosed += (s, args) =>
            {
                // Closing this form triggers the original LoginForm (which opened this) to show itself
                this.Close();
            };
            technicianForm.Show();
        }
    }
}