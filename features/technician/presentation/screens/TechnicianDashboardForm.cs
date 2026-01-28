using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.technician.data.repositories;
using mtc_app.features.technician.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.data.session;
using mtc_app.shared.presentation.styles; // Fix AppColors

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

        // Date Filter Feature
        private DateTimePicker dtpStart;
        private DateTimePicker dtpEnd;
        private Button btnFilter;

        public TechnicianDashboardForm() : this(new TechnicianRepository())
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
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            // Add Toolbar BEFORE Tabs so it docks to top
            this.Controls.Add(pnlToolbar);
            pnlToolbar.BringToFront(); // Ensure it's below header but above content if z-order matters

            // 2. Initialize Features into Toolbar
            InitializeDateFilter(pnlToolbar);
            InitializeAutoSwitch(pnlToolbar);
            
            // 3. Initialize Tabs (Dock=Fill will take remaining space)
            InitializeTabs();
            tabControl.BringToFront(); // Tabs should be main content
            pnlToolbar.SendToBack(); // Push toolbar up? No, Dock order matters.
            // Dock Order: Last Added is First in Dock hierarchy usually? No, it's z-order.
            // To be safe: Add Toolbar, Then Add Tabs.
            // Since InitializeTabs adds tabControl, let's ensure tabControl is added AFTER toolbar.
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

            var lblFrom = new Label { Text = "Periode:", AutoSize = true, Margin = new Padding(0, 5, 5, 0), Font = new Font("Segoe UI", 10F) };
            dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Font = new Font("Segoe UI", 10F), Value = DateTime.Now.AddDays(-7) };
            var lblTo = new Label { Text = "-", AutoSize = true, Margin = new Padding(5, 5, 5, 0) };
            dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Font = new Font("Segoe UI", 10F), Value = DateTime.Now };

            btnFilter = new Button 
            { 
                Text = "Terapkan", 
                Size = new Size(90, 27),
                BackColor = AppColors.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0)
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

        private void InitializeAutoSwitch(Panel parent)
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

            // Button Setup
            btnAutoSwitch = new Button
            {
                Text = "Auto Switch: OFF",
                Size = new Size(140, 30),
                Location = new Point(parent.Width - 160, 15), // Right aligned
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.WhiteSmoke,
                ForeColor = Color.DimGray,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAutoSwitch.FlatAppearance.BorderColor = Color.LightGray;
            
            btnAutoSwitch.Click += (s, e) =>
            {
                if (timerTabSwitch.Enabled)
                {
                    timerTabSwitch.Stop();
                    btnAutoSwitch.Text = "Auto Switch: OFF";
                    btnAutoSwitch.BackColor = Color.WhiteSmoke;
                    btnAutoSwitch.ForeColor = Color.DimGray;
                }
                else
                {
                    timerTabSwitch.Start();
                    btnAutoSwitch.Text = "Auto Switch: ON";
                    btnAutoSwitch.BackColor = AppColors.Success; 
                    btnAutoSwitch.ForeColor = Color.White;
                }
            };
            
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
                Text = "Auto Switch: OFF",
                Size = new Size(120, 30),
                Location = new Point(this.ClientSize.Width - 140, 15), // Top Right of Form
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat
            };
            btnAutoSwitch.Click += (s, e) =>
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
                Font = new Font("Segoe UI", 10F),
                Padding = new Point(15, 5)
            };

            // Tab 1: Work Queue
            var tabWorkQueue = new TabPage("Daftar Tunggu")
            {
                BackColor = Color.White
            };
            
            workQueueControl = new TechnicianWorkQueueControl(_repository)
            {
                Dock = DockStyle.Fill
            };
            tabWorkQueue.Controls.Add(workQueueControl);

            // Tab 2: Performance
            var tabPerformance = new TabPage("Performa")
            {
                BackColor = Color.White
            };
            
            performanceControl = new TechnicianPerformanceControl(_repository)
            {
                Dock = DockStyle.Fill
            };

            tabPerformance.Controls.Add(performanceControl);

            // Tab 3: Machine Analysis (NEW)
            var tabMachine = new TabPage("Analisis Mesin")
            {
                BackColor = Color.White
            };
            
            machinePerformanceControl = new MachinePerformanceControl(_repository)
            {
                Dock = DockStyle.Fill
            };
            tabMachine.Controls.Add(machinePerformanceControl);

            // Tab 4: Machine Monitor (Real-time)
            var tabMonitor = new TabPage("Monitoring Mesin")
            {
                BackColor = Color.White
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
            base.OnFormClosing(e);
        }
    }
}