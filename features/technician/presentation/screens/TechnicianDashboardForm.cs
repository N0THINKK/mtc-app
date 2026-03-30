using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using mtc_app.features.stock.data.repositories;
using mtc_app.features.technician.data.repositories;
// using mtc_app.features.technician.logic; // <-- HAPUS INI
using mtc_app.features.technician.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.data.session;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.infrastructure;

namespace mtc_app.features.technician.presentation.screens
{
    public partial class TechnicianDashboardForm : AppBaseForm
    {
        private readonly ITechnicianRepository _repository;
        private readonly long _technicianId;
        
        // Child Controls
        private TabControl tabControl;
        private TechnicianWorkQueueControl workQueueControl;
        private TechnicianPerformanceControl performanceControl;
        private MachinePerformanceControl machinePerformanceControl;
        private MachineMonitorControl machineMonitorControl;
        private StockDataControl stockDataControl;
        private TechnicianPatrolControl patrolControl;
        
        // Auto Switch Feature
        private Timer timerTabSwitch;
        private Button btnAutoSwitch;
        private NumericUpDown nudInterval;
        private Button btnSetInterval;
        private int _autoSwitchStage = 0;

        // Date Filter Feature
        private DateTimePicker dtpStart;
        private DateTimePicker dtpEnd;
        private Button btnFilter;

        // [HAPUS BAGIAN INI] Background Logger
        // private Timer timerLogger; 

        public TechnicianDashboardForm() : this(ServiceLocator.CreateTechnicianRepository())
        {
        }

        public TechnicianDashboardForm(ITechnicianRepository repository)
        {
            _repository = repository;
            
            if (UserSession.CurrentUser != null)
            {
                _technicianId = UserSession.CurrentUser.UserId;
            }
            else
            {
                _technicianId = 0;
            }

            InitializeComponent();
            
            // 1. Setup Toolbar Container (TableLayoutPanel: 2 columns)
            var pnlToolbar = BuildToolbar();
            this.Controls.Add(pnlToolbar);
            pnlToolbar.BringToFront(); 

            // 2. Initialize Tabs (Dock=Fill will take remaining space)
            InitializeTabs();
            tabControl.BringToFront(); 

            this.Shown += TechnicianDashboardForm_Shown;

            // [HAPUS BAGIAN INI] 
            // 3. Start Background Logger (Every 5 Mins)
            // timerLogger = new Timer { Interval = 300000 }; 
            // timerLogger.Tick += async (s, e) => { await new MachineDataLogger().LogMachineDataAsync(); };
            // timerLogger.Start();
            
            // Initial Log on Startup
            // _ = new MachineDataLogger().LogMachineDataAsync();
            
            // CATATAN:
            // Sekarang logging dilakukan oleh Windows Service "MtcMachineLogger".
            // Dashboard ini hanya bertugas MENAMPILKAN data (Read-Only).
        }

        private async void TechnicianDashboardForm_Shown(object sender, EventArgs e)
        {
            await Task.Delay(50); // Delay kecil untuk memastikan UI sudah siap
            if (tabControl.SelectedIndex == 0)
            {
                machineMonitorControl.StartMonitoring();
            }
            LoadCurrentTabData(); // Load data untuk tab yang aktif saat ini
        }

        // ========================================================
        // Toolbar: Two-column layout (Left=Date Filter, Right=Auto Switch)
        // ========================================================
        private TableLayoutPanel BuildToolbar()
        {
            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = AppDimens.HeaderHeight,
                BackColor = AppColors.CardBackground,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(AppDimens.PaddingSmall)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Left column: Date Filter
            var flowFilter = BuildDateFilterFlow();
            toolbar.Controls.Add(flowFilter, 0, 0);

            // Right column: Auto Switch
            var flowAutoSwitch = BuildAutoSwitchFlow();
            toolbar.Controls.Add(flowAutoSwitch, 1, 0);

            return toolbar;
        }

        private FlowLayoutPanel BuildDateFilterFlow()
        {
            var flowFilter = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Left,
                Dock = DockStyle.Fill
            };

            var lblFrom = new Label 
            { 
                Text = "Periode:", 
                AutoSize = true, 
                Margin = new Padding(0, AppDimens.MarginSmall, AppDimens.MarginSmall, 0), 
                Font = AppFonts.Title 
            };
            
            DateTime firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtpStart = new DateTimePicker 
            { 
                Format = DateTimePickerFormat.Short, 
                Width = 140, 
                Font = AppFonts.Title, 
                Value = firstDayOfMonth 
            };
            
            var lblTo = new Label 
            { 
                Text = "-", 
                AutoSize = true, 
                Margin = new Padding(AppDimens.MarginSmall, AppDimens.MarginSmall, AppDimens.MarginSmall, 0), 
                Font = AppFonts.Title 
            };
            dtpEnd = new DateTimePicker 
            { 
                Format = DateTimePickerFormat.Short, 
                Width = 140, 
                Font = AppFonts.Title, 
                Value = DateTime.Now 
            };

            btnFilter = new Button 
            { 
                Text = "Terapkan", 
                Size = new Size(110, 35),
                BackColor = AppColors.Primary,
                ForeColor = AppColors.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = AppFonts.Button,
                Cursor = Cursors.Hand,
                Margin = new Padding(AppDimens.GapStandard, 0, 0, 0)
            };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadCurrentTabData();

            flowFilter.Controls.AddRange(new Control[] { lblFrom, dtpStart, lblTo, dtpEnd, btnFilter });
            return flowFilter;
        }

        private FlowLayoutPanel BuildAutoSwitchFlow()
        {
            var flowAutoSwitch = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Fill
            };

            // Timer Setup
            timerTabSwitch = new Timer();
            timerTabSwitch.Interval = 60000; // 10 Seconds default
            timerTabSwitch.Tick += AutoSwitch_Tick;

            // Button (added first because FlowDirection is RightToLeft)
            btnAutoSwitch = new Button
            {
                Text = "Auto Switch: OFF",
                Size = new Size(160, 40),
                BackColor = AppColors.Surface,
                ForeColor = AppColors.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Font = AppFonts.Button,
                Cursor = Cursors.Hand,
                Margin = new Padding(AppDimens.MarginSmall, 0, 0, 0)
            };
            btnAutoSwitch.FlatAppearance.BorderColor = AppColors.Border;
            
            btnAutoSwitch.Click += (s, e) =>
            {
                if (timerTabSwitch.Enabled)
                {
                    timerTabSwitch.Stop();
                    _autoSwitchStage = -1; // Reset stage
                    btnAutoSwitch.Text = "Auto Switch: OFF";
                    btnAutoSwitch.BackColor = AppColors.Surface;
                    btnAutoSwitch.ForeColor = AppColors.TextSecondary;
                }
                else
                {
                    timerTabSwitch.Interval = (int)nudInterval.Value * 1000;
                    timerTabSwitch.Start();
                    AutoSwitch_Tick(null, EventArgs.Empty);
                    btnAutoSwitch.Text = "Auto Switch: ON";
                    btnAutoSwitch.BackColor = AppColors.Success; 
                    btnAutoSwitch.ForeColor = AppColors.TextInverse;
                }
            };

            // Set Interval Button
            btnSetInterval = new Button
            {
                Text = "Set",
                Size = new Size(50, 30),
                BackColor = AppColors.Primary,
                ForeColor = AppColors.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = AppFonts.Caption,
                Cursor = Cursors.Hand,
                Margin = new Padding(AppDimens.MarginSmall, 5, 0, 0)
            };
            btnSetInterval.Click += (s, e) =>
            {
                timerTabSwitch.Interval = (int)nudInterval.Value * 1000;
                MessageBox.Show($"Auto switch setiap {nudInterval.Value} detik.", "Pengaturan Tersimpan", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Interval NumericUpDown
            nudInterval = new NumericUpDown
            {
                Size = new Size(60, 28),
                Minimum = 5,
                Maximum = 3600,
                Value = 10,
                Font = AppFonts.Body,
                Margin = new Padding(AppDimens.MarginSmall, 5, 0, 0)
            };

            // Interval Label
            var lblInterval = new Label 
            { 
                Text = "Interval (detik):", 
                AutoSize = true, 
                Font = AppFonts.Body,
                Margin = new Padding(0, 10, 0, 0)
            };

            flowAutoSwitch.Controls.AddRange(new Control[] { btnAutoSwitch, btnSetInterval, nudInterval, lblInterval });
            return flowAutoSwitch;
        }

        private void AutoSwitch_Tick(object sender, EventArgs e)
        {
            if (tabControl.TabCount <= 0) return;

            const int totalStages = 7; 
            _autoSwitchStage = (_autoSwitchStage + 1) % totalStages;

            switch (_autoSwitchStage)
            {
                case 0: // Monitor - Output
                    tabControl.SelectedIndex = 0;
                    machineMonitorControl?.SetMetric(0);
                    break;
                case 1: // Monitor - Efficiency
                    tabControl.SelectedIndex = 0;
                    machineMonitorControl?.SetMetric(1);
                    break;
                case 2: // Work Queue
                    tabControl.SelectedIndex = 1;
                    break;
                case 3: // Data Part
                    tabControl.SelectedIndex = 2;
                    break;
                case 4: // Performance
                    tabControl.SelectedIndex = 3;
                    break;
                case 5: // Machine Analysis
                    tabControl.SelectedIndex = 4;
                    break;
                case 6: // Patroli Checksheet
                    tabControl.SelectedIndex = 5;
                    break;
            }
        }

        // ========================================================
        // Tab Data Loading
        // ========================================================
        private async void LoadCurrentTabData()
        {
            DateTime start = dtpStart.Value.Date;
            DateTime end = dtpEnd.Value.Date.AddDays(1).AddSeconds(-1); 

            if (tabControl.SelectedIndex == 2) // Data Part
            {
                await stockDataControl.LoadDataAsync(start, end);
            }
            else if (tabControl.SelectedIndex == 3) // Performa
            {
                await performanceControl.LoadDataAsync(start, end);
            }
            else if (tabControl.SelectedIndex == 4) // Analisis Mesin
            {
                await machinePerformanceControl.LoadDataAsync(start, end);
            }
            else if (tabControl.SelectedIndex == 5) // Patroli Checksheet
            {
                await patrolControl.LoadDataAsync(start, end);
            }
        }

        // ========================================================
        // Tab Initialization
        // ========================================================
        private void InitializeTabs()
        {
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = AppFonts.BodySmall,
                Padding = new Point(15, 5)
            };

            // Tab 1: Work Queue
            var tabWorkQueue = new TabPage("Daftar Tunggu") { BackColor = AppColors.CardBackground };
            workQueueControl = new TechnicianWorkQueueControl(_repository) { Dock = DockStyle.Fill };
            tabWorkQueue.Controls.Add(workQueueControl);

            // Tab 2: Stock Data (Part Requests)
            var tabStockData = new TabPage("Data Part") { BackColor = AppColors.CardBackground };
            stockDataControl = new StockDataControl(ServiceLocator.CreateStockRepository()) { Dock = DockStyle.Fill };
            tabStockData.Controls.Add(stockDataControl);

            // Tab 3: Performance
            var tabPerformance = new TabPage("Performa") { BackColor = AppColors.CardBackground };
            performanceControl = new TechnicianPerformanceControl(_repository) { Dock = DockStyle.Fill };
            tabPerformance.Controls.Add(performanceControl);

            // Tab 4: Machine Analysis
            var tabMachine = new TabPage("Downtime") { BackColor = AppColors.CardBackground };
            machinePerformanceControl = new MachinePerformanceControl(_repository) { Dock = DockStyle.Fill };
            tabMachine.Controls.Add(machinePerformanceControl);
            
            // Tab 5: Patroli Checksheet
            var tabPatroli = new TabPage("Patroli NG") { BackColor = AppColors.CardBackground };
            patrolControl = new TechnicianPatrolControl(_repository) { Dock = DockStyle.Fill };
            tabPatroli.Controls.Add(patrolControl);

            // Tab 6: Machine Monitor (Real-time)
            var tabMonitor = new TabPage("Output") { BackColor = AppColors.CardBackground };
            machineMonitorControl = new MachineMonitorControl { Dock = DockStyle.Fill };
            tabMonitor.Controls.Add(machineMonitorControl);

            tabControl.TabPages.Add(tabMonitor);
            tabControl.TabPages.Add(tabWorkQueue);
            tabControl.TabPages.Add(tabStockData);
            tabControl.TabPages.Add(tabPerformance);
            tabControl.TabPages.Add(tabMachine);
            tabControl.TabPages.Add(tabPatroli);

            // Load data when tab changes
            tabControl.SelectedIndexChanged += (s, e) =>
            {
                // Manage Real-time monitoring
                if (tabControl.SelectedIndex == 0) // Monitor tab
                {
                    machineMonitorControl.StartMonitoring();
                }
                else
                {
                    machineMonitorControl.StopMonitoring();
                }

                LoadCurrentTabData();
            };

            this.Controls.Add(tabControl);

            // Start work queue auto-refresh
            if (!this.DesignMode)
            {
                workQueueControl.StartAutoRefresh();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            workQueueControl?.StopAutoRefresh();
            
            // [HAPUS BAGIAN INI]
            // timerLogger?.Stop();
            // timerLogger?.Dispose();
            
            base.OnFormClosing(e);
        }
    }
}