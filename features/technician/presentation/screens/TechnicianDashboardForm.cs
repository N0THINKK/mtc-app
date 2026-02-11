using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.technician.data.repositories;
using mtc_app.features.technician.logic; // [NEW] For Logger
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

        // Background Logger
        private Timer timerLogger;

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
            
            // 1. Setup Toolbar Container
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = AppDimens.HeaderHeight,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(AppDimens.PaddingSmall)
            };
            // Add Toolbar BEFORE Tabs so it docks to top
            this.Controls.Add(pnlToolbar);
            pnlToolbar.BringToFront(); 

            // 2. Initialize Features into Toolbar
            InitializeDateFilter(pnlToolbar);
            InitializeAutoSwitch(pnlToolbar);
            
            // 3. Initialize Tabs (Dock=Fill will take remaining space)
            InitializeTabs();
            tabControl.BringToFront(); 

            // 4. Start Background Logger (Every 5 Mins)
            timerLogger = new Timer { Interval = 300000 }; // 5 Minutes
            timerLogger.Tick += async (s, e) => { await new MachineDataLogger().LogMachineDataAsync(); };
            timerLogger.Start();
            
            // Initial Log on Startup
            _ = new MachineDataLogger().LogMachineDataAsync();
        }

        private void InitializeDateFilter(Panel parent)
        {
            var flowFilter = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(10, 15), // Vertical center roughly
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            var lblFrom = new Label { Text = "Periode:", AutoSize = true, Margin = new Padding(0, AppDimens.MarginSmall, AppDimens.MarginSmall, 0), Font = AppFonts.Title };
            
            // [UI-FIX] Default start date = 1st of current month
            DateTime firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 140, Font = AppFonts.Title, Value = firstDayOfMonth };
            
            var lblTo = new Label { Text = "-", AutoSize = true, Margin = new Padding(AppDimens.MarginSmall, AppDimens.MarginSmall, AppDimens.MarginSmall, 0), Font = AppFonts.Title };
            dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 140, Font = AppFonts.Title, Value = DateTime.Now };

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
            parent.Controls.Add(flowFilter);
        }

        private async void LoadCurrentTabData()
        {
            DateTime start = dtpStart.Value.Date;
            DateTime end = dtpEnd.Value.Date.AddDays(1).AddSeconds(-1); // End of day

            if (tabControl.SelectedIndex == 1) // Performa
            {
                await performanceControl.LoadDataAsync(start, end);
            }
            else if (tabControl.SelectedIndex == 2) // Analisis Mesin
            {
                await machinePerformanceControl.LoadDataAsync(start, end);
            }
        }

        private int _autoSwitchStage = 0;

        private void AutoSwitch_Tick(object sender, EventArgs e)
        {
            if (tabControl.TabCount <= 0) return;

            const int totalStages = 5; // 3 tabs + 2 modes for the 4th tab
            _autoSwitchStage = (_autoSwitchStage + 1) % totalStages;

            switch (_autoSwitchStage)
            {
                case 0: // Stage 0: Tab 1 (Work Queue)
                    tabControl.SelectedIndex = 0;
                    break;
                case 1: // Stage 1: Tab 2 (Performance)
                    tabControl.SelectedIndex = 1;
                    break;
                case 2: // Stage 2: Tab 3 (Machine Analysis)
                    tabControl.SelectedIndex = 2;
                    break;
                case 3: // Stage 3: Tab 4 (Monitor) - Set to Output
                    tabControl.SelectedIndex = 3;
                    machineMonitorControl?.SetMetric(0); // 0 for "Produksi (Output)"
                    break;
                case 4: // Stage 4: Tab 4 (Monitor) - Set to Efficiency
                    tabControl.SelectedIndex = 3;
                    machineMonitorControl?.SetMetric(1); // 1 for "Efisiensi (Waktu)"
                    break;
            }
        }

        private void InitializeAutoSwitch(Panel parent)
        {
            // Timer Setup
            timerTabSwitch = new Timer();
            timerTabSwitch.Interval = 10000; // 10 Seconds default
            timerTabSwitch.Tick += AutoSwitch_Tick;

            btnSetInterval = new Button
            {
                Text = "Set",
                Size = new Size(50, 30),
                Location = new Point(parent.Width - 240, 19),  // Adjusted for input spacing
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = AppColors.Primary,
                ForeColor = AppColors.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = AppFonts.Caption,
                Cursor = Cursors.Hand
            };
            btnSetInterval.Click += (s, e) =>
            {
                timerTabSwitch.Interval = (int)nudInterval.Value * 1000;
                MessageBox.Show($"Auto switch interval set to {nudInterval.Value} seconds.", "Pengaturan Tersimpan", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Button Setup
            btnAutoSwitch = new Button
            {
                Text = "Auto Switch: OFF",
                Size = new Size(160, 40),
                Location = new Point(parent.Width - 170, 15), // Further right
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = AppColors.Surface,
                ForeColor = AppColors.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Font = AppFonts.Button,
                Cursor = Cursors.Hand
            };
            btnAutoSwitch.FlatAppearance.BorderColor = AppColors.Border;
            
            btnAutoSwitch.Click += (s, e) =>
            {
                if (timerTabSwitch.Enabled)
                {
                    timerTabSwitch.Stop();
                    _autoSwitchStage = -1; // Reset stage so it starts from 0 on next play
                    btnAutoSwitch.Text = "Auto Switch: OFF";
                    btnAutoSwitch.BackColor = AppColors.Surface;
                    btnAutoSwitch.ForeColor = AppColors.TextSecondary;
                }
                else
                {
                    // Update interval before starting just in case
                    timerTabSwitch.Interval = (int)nudInterval.Value * 1000;
                    timerTabSwitch.Start();
                    // Manually trigger first tick to avoid initial delay
                    AutoSwitch_Tick(null, EventArgs.Empty);
                    btnAutoSwitch.Text = "Auto Switch: ON";
                    btnAutoSwitch.BackColor = AppColors.Success; 
                    btnAutoSwitch.ForeColor = AppColors.TextInverse;
                }
            };
            
            parent.Controls.Add(lblInterval);
            parent.Controls.Add(nudInterval);
            parent.Controls.Add(btnSetInterval);
            parent.Controls.Add(btnAutoSwitch);
        }

        private void InitializeAutoSwitch()
        {
            // Timer Setup
            timerTabSwitch = new Timer();
            timerTabSwitch.Interval = 10000; // 10 Seconds
            timerTabSwitch.Tick += (s, e) =>
            {
                if (tabControl.TabCount > 0)
                {
                    int nextIndex = (tabControl.SelectedIndex + 1) % tabControl.TabCount;
                    tabControl.SelectedIndex = nextIndex;
                }
            };

            // Button Setup (Added directly to Form, on top of everything)
            btnAutoSwitch = new Button
            {
                Size = new Size(60, 28),
                Minimum = 5,
                Maximum = 3600,
                Value = 10, // Default to 10 seconds for faster switching
                Font = AppFonts.Body,
                Margin = new Padding(AppDimens.MarginSmall, 5, 0, 0)
            };

            // Interval Label
            var lblInterval = new Label 
            { 
                Text = "Interval (s):", 
                AutoSize = true, 
                Font = AppFonts.Body,
                Margin = new Padding(0, 10, 0, 0)
            };

            // RightToLeft order: Button first (rightmost), then Set, then NUD, then label
            flowAutoSwitch.Controls.AddRange(new Control[] { btnAutoSwitch, btnSetInterval, nudInterval, lblInterval });
            return flowAutoSwitch;
        }

        private void AutoSwitch_Tick(object sender, EventArgs e)
        {
            if (tabControl.TabCount <= 0) return;

            const int totalStages = 5; // 3 tabs + 2 modes for the 4th tab
            _autoSwitchStage = (_autoSwitchStage + 1) % totalStages;

            switch (_autoSwitchStage)
            {
                case 0: // Stage 0: Tab 1 (Work Queue)
                    tabControl.SelectedIndex = 0;
                    break;
                case 1: // Stage 1: Tab 2 (Performance)
                    tabControl.SelectedIndex = 1;
                    break;
                case 2: // Stage 2: Tab 3 (Machine Analysis)
                    tabControl.SelectedIndex = 2;
                    break;
                case 3: // Stage 3: Tab 4 (Monitor) - Set to Output
                    tabControl.SelectedIndex = 3;
                    machineMonitorControl?.SetMetric(0); // 0 for "Produksi (Output)"
                    break;
                case 4: // Stage 4: Tab 4 (Monitor) - Set to Efficiency
                    tabControl.SelectedIndex = 3;
                    machineMonitorControl?.SetMetric(1); // 1 for "Efisiensi (Waktu)"
                    break;
            }
        }

        // ========================================================
        // Tab Data Loading
        // ========================================================
        private async void LoadCurrentTabData()
        {
            DateTime start = dtpStart.Value.Date;
            DateTime end = dtpEnd.Value.Date.AddDays(1).AddSeconds(-1); // End of day

            if (tabControl.SelectedIndex == 1) // Performa
            {
                await performanceControl.LoadDataAsync(start, end);
            }
            else if (tabControl.SelectedIndex == 2) // Analisis Mesin
            {
                if (timerTabSwitch.Enabled)
                {
                    timerTabSwitch.Stop();
                    btnAutoSwitch.Text = "Auto Switch: OFF";
                    btnAutoSwitch.BackColor = Color.WhiteSmoke;
                    btnAutoSwitch.ForeColor = Color.Black;
                }
                else
                {
                    timerTabSwitch.Start();
                    btnAutoSwitch.Text = "Auto Switch: ON";
                    btnAutoSwitch.BackColor = AppColors.Success; // Green indicates active
                    btnAutoSwitch.ForeColor = Color.White;
                }
            };
            
            this.Controls.Add(btnAutoSwitch);
            btnAutoSwitch.BringToFront(); // Ensure it is above the header panel
        }

        private void InitializeTabs()
        {
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = AppFonts.BodySmall,
                Padding = new Point(15, 5)
            };

            // Tab 1: Work Queue
            var tabWorkQueue = new TabPage("Daftar Tunggu")
            {
                BackColor = AppColors.CardBackground
            };
            
            workQueueControl = new TechnicianWorkQueueControl(_repository)
            {
                Dock = DockStyle.Fill
            };
            tabWorkQueue.Controls.Add(workQueueControl);

            // Tab 2: Performance
            var tabPerformance = new TabPage("Performa")
            {
                BackColor = AppColors.CardBackground
            };
            
            performanceControl = new TechnicianPerformanceControl(_repository)
            {
                Dock = DockStyle.Fill
            };

            tabPerformance.Controls.Add(performanceControl);

            // Tab 3: Machine Analysis (NEW)
            var tabMachine = new TabPage("Analisis Mesin")
            {
                BackColor = AppColors.CardBackground
            };
            
            machinePerformanceControl = new MachinePerformanceControl(_repository)
            {
                Dock = DockStyle.Fill
            };
            tabMachine.Controls.Add(machinePerformanceControl);

            // Tab 4: Machine Monitor (Real-time)
            var tabMonitor = new TabPage("Monitoring Mesin")
            {
                BackColor = AppColors.CardBackground
            };
            
            machineMonitorControl = new MachineMonitorControl
            {
                Dock = DockStyle.Fill
            };
            tabMonitor.Controls.Add(machineMonitorControl);

            tabControl.TabPages.Add(tabWorkQueue);
            tabControl.TabPages.Add(tabPerformance);
            tabControl.TabPages.Add(tabMachine);
            tabControl.TabPages.Add(tabMonitor);

            // Load data when tab changes
            tabControl.SelectedIndexChanged += (s, e) =>
            {
                // Manage Real-time monitoring
                if (tabControl.SelectedTab == tabMonitor)
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
            
            timerLogger?.Stop();
            timerLogger?.Dispose();
            
            base.OnFormClosing(e);
        }
    }
}