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

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.OnResize(EventArgs.Empty);
            await CheckForPendingTicketAsync();
        }

        // Main Responsive Layout Containers
        private TableLayoutPanel _rootLayout;
        private TableLayoutPanel _tab1Layout;
        private TableLayoutPanel _formLayout; 
        private TableLayoutPanel _problemsLayout; 

        private void InitializeCustomTabs()
        {
            // Clear existing controls from Designer
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
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Content (Tabs)

            // Header (Preserve existing panelHeader)
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

            // Tab 1 Main Layout (Form Fields vs Action Button)
            _tab1Layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20, 10, 20, 10)
            };
            _tab1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _tab1Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Form Fields (Scrollable)
            _tab1Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Action Button

            // row 0: Form Fields Container
            _formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoScroll = true,
                Padding = new Padding(0, 0, 10, 0) // Right padding for scrollbar
            };
            _formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _tab1Layout.Controls.Add(_formLayout, 0, 0);

            // row 1: Action Button Container (Repurpose panelFooter)
            panelFooter.Parent = null; // Detach from form
            panelFooter.Controls.Clear();
            panelFooter.Dock = DockStyle.Fill;
            panelFooter.Height = 80; 
            panelFooter.Padding = new Padding(0); 
            _tab1Layout.Controls.Add(panelFooter, 0, 1);

            tabReport.Controls.Add(_tab1Layout);
            _tabControl.TabPages.Add(tabReport);

            // === Tab 2: History Tab ===
            var tabHistory = new TabPage("Riwayat Mesin") { BackColor = AppColors.CardBackground };
            // Filter Panel
            var pnlFilter = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Top, 
                Height = 60, 
                Padding = new Padding(10, 15, 10, 10),
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = false
            };
            
            _dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
            var lblTo = new Label { Text = "s/d", AutoSize = true, Margin = new Padding(5, 5, 5, 0) }; 
            _dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
            _btnFilter = new AppButton { Text = "Filter", Type = AppButton.ButtonType.Primary, Width = 80, Height = 30 };
            _btnFilter.Click += async (s, e) => await LoadHistoryAsync();

            pnlFilter.Controls.AddRange(new Control[] { _dtpStart, lblTo, _dtpEnd, _btnFilter });
            tabHistory.Controls.Add(pnlFilter);

            _historyControl = new MachineHistoryListControl { Dock = DockStyle.Fill };
            _historyControl.ItemClicked += HistoryControl_ItemClicked;
            tabHistory.Controls.Add(_historyControl);
            _historyControl.BringToFront();

            _tabControl.TabPages.Add(tabHistory);

            // === PENDING TICKET LINK ===
            _lnkPendingTicket = new LinkLabel
            {
                Text = "⚠️ CONTINUE PROBLEM", // [PERUBAHAN 1] Text Default
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
            _lnkPendingTicket.Location = new Point(panelHeader.Width - _lnkPendingTicket.Width - 20, 15);
            _lnkPendingTicket.LinkClicked += LnkPendingTicket_LinkClicked;
            panelHeader.Controls.Add(_lnkPendingTicket);
        }
        
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_lnkPendingTicket != null && _lnkPendingTicket.Visible)
            {
                _lnkPendingTicket.Location = new Point(panelHeader.Width - _lnkPendingTicket.Width - 20, 15);
            }
        }

        private void SetupInputs()
        {
            void AddToForm(Control c, int bottomMargin = 10)
            {
                 c.Dock = DockStyle.Top; 
                 c.Margin = new Padding(0, 0, 0, bottomMargin);
                 _formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                 _formLayout.Controls.Add(c, 0, _formLayout.RowCount++);
            }

            // 1. NIK
            inputNIK = CreateInput("NIK Operator", AppInput.InputTypeEnum.Dropdown, true);
            inputNIK.AllowCustomText = true;
            inputNIK.DropdownOpened += (s, e) => LoadOperatorsFromDB();
            LoadOperatorsFromDB();
            AddToForm(inputNIK);

            // 2. Shift
            inputShift = CreateInput("Shift", AppInput.InputTypeEnum.Dropdown, true);
            inputShift.AllowCustomText = false;
            LoadShiftsFromDB();
            AddToForm(inputShift);

            // 3. Applicator
            inputApplicator = CreateInput("No. Aplikator", AppInput.InputTypeEnum.Text, true);
            inputApplicator.CharacterCasing = CharacterCasing.Upper;
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

            // 5. Problems Container
            _problemsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, 
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                ColumnCount = 1,
                Padding = new Padding(0)
            };
            _problemsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
            _formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _formLayout.Controls.Add(_problemsLayout, 0, _formLayout.RowCount++);

            // 6. Add Problem Button
            btnAddProblem = new AppButton
            {
                Text = "+ Tambah Masalah Lain",
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

        private void AddProblemInput()
        {
            var problemControl = new ProblemInputControl(_problemControls.Count);
            problemControl.RemoveRequested += (s, e) => RemoveProblemInput(problemControl);
            problemControl.Dock = DockStyle.Top;
            problemControl.AutoSize = true;
            problemControl.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            
            _problemControls.Add(problemControl);
            
            _problemsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _problemsLayout.Controls.Add(problemControl, 0, _problemsLayout.RowCount++);
        }

        private void RemoveProblemInput(ProblemInputControl control)
        {
            if (_problemControls.Count <= 1)
            {
                AutoClosingMessageBox.Show("Minimal harus ada satu masalah.", "Info", 1500);
                return;
            }
            
            _problemsLayout.Controls.Remove(control);
            _problemControls.Remove(control);
            control.Dispose();
            
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

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            if (!inputNIK.ValidateInput() || !inputShift.ValidateInput() || !inputApplicator.ValidateInput())
            {
                MessageBox.Show("Mohon lengkapi semua data.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                int machineId = 1;
                if (int.TryParse(DatabaseHelper.GetMachineId(), out int configId))
                {
                    machineId = configId;
                }

                var request = new CreateTicketRequest
                {
                    OperatorNik = inputNIK.InputValue,
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

                var technicianForm = new MachineHistoryFormTechnician(result.TicketId);
                this.Hide(); 
                technicianForm.FormClosed += (s, args) => 
                {
                    if (technicianForm.DialogResult == DialogResult.OK)
                    {
                        this.Close();
                    }
                    else
                    {
                        this.Show();
                    }
                };
                technicianForm.Show();
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
                    // [PERUBAHAN 2] Teks diganti jadi CONTINUE PROBLEM
                    // Ditambah info status dalam kurung agar user tetap tahu kondisinya
                    _lnkPendingTicket.Text = $"⚠️ CONTINUE PROBLEM ({_pendingTicket.StatusName.ToUpper()})";
                    _lnkPendingTicket.Visible = true;
                    _lnkPendingTicket.Location = new Point(panelHeader.Width - _lnkPendingTicket.Width - 20, 15);
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
            technicianForm.FormClosed += async (s, args) =>
            {
                if (technicianForm.DialogResult == DialogResult.OK)
                {
                    this.Close();
                }
                else
                {
                    this.Show();
                    await CheckForPendingTicketAsync();
                    await LoadHistoryAsync();
                }
            };
            technicianForm.Show();
        }
    }
}