using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.technician.data.repositories;
using mtc_app.features.technician.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.data.session;

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

            tabControl.TabPages.Add(tabWorkQueue);
            tabControl.TabPages.Add(tabPerformance);

            // Load data when tab changes
            tabControl.SelectedIndexChanged += async (s, e) =>
            {
                if (tabControl.SelectedIndex == 1)
                {
                    await performanceControl.LoadDataAsync();
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