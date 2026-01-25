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
        
        // Auto Switch Feature
        private Timer timerTabSwitch;
        private Button btnAutoSwitch;

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
            InitializeTabs();
            InitializeAutoSwitch();
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

            tabControl.TabPages.Add(tabWorkQueue);
            tabControl.TabPages.Add(tabPerformance);
            tabControl.TabPages.Add(tabMachine);

            // Load data when tab changes
            tabControl.SelectedIndexChanged += async (s, e) =>
            {
                if (tabControl.SelectedIndex == 1)
                {
                    await performanceControl.LoadDataAsync();
                }
                else if (tabControl.SelectedIndex == 2)
                {
                    await machinePerformanceControl.LoadDataAsync();
                }
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