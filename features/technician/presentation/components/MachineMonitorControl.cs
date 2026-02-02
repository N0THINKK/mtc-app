using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Dapper;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class MachineMonitorControl : UserControl
    {
        private const int REFRESH_RATE_MS = 5000; // Refresh dashboard every 5 seconds

        private Timer _timer;
        private Chart _chart;
        private Label _lblStatus;
        private ComboBox _comboSort;
        
        // Required for Designer support
        private System.ComponentModel.IContainer components = null;

        public MachineMonitorControl()
        {
            InitializeComponent();
            SetupTimer();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.Padding = new Padding(20);

            // 1. Header Layout
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50 };
            
            var lblTitle = new Label 
            { 
                Text = "Monitoring Output Produksi (Live)", 
                Font = AppFonts.MetricSmall,
                AutoSize = true,
                Location = new Point(0, 5)
            };

            _lblStatus = new Label
            {
                Text = "Loading...",
                Font = AppFonts.BodySmall,
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(400, 12)
            };

            _comboSort = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Location = new Point(pnlHeader.Width - 160, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _comboSort.Items.AddRange(new object[] { "Total Product", "Cutter Count", "Press A", "Press B" });
            _comboSort.SelectedIndex = 0;
            _comboSort.SelectedIndexChanged += (s, e) => LoadData();

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(_lblStatus);
            pnlHeader.Controls.Add(_comboSort);
            this.Controls.Add(pnlHeader);

            // 2. Chart Control
            _chart = new Chart();
            _chart.Dock = DockStyle.Fill;
            _chart.BackColor = Color.White;
            
            // Area
            var chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Angle = -45; // Miringkan label jika nama mesin panjang
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            _chart.ChartAreas.Add(chartArea);

            // Legend
            var legend = new Legend("MainLegend");
            legend.Docking = Docking.Top;
            _chart.Legends.Add(legend);

            this.Controls.Add(_chart);
        }

        private void SetupTimer()
        {
            _timer = new Timer();
            _timer.Interval = REFRESH_RATE_MS;
            _timer.Tick += (s, e) => LoadData();
        }

        public void StartMonitoring()
        {
            LoadData();
            _timer.Start();
        }

        public void StopMonitoring()
        {
            _timer.Stop();
        }

        private async void LoadData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    string orderBy = "all_product";
                    string label = "Total Product";
                    
                    switch (_comboSort.SelectedIndex)
                    {
                        case 1: orderBy = "count_cutter"; label = "Cutter Count"; break;
                        case 2: orderBy = "count_press_a"; label = "Press A"; break;
                        case 3: orderBy = "count_press_b"; label = "Press B"; break;
                    }

                    string sql = $@"
                        SELECT 
                            m.machine_number, 
                            m.machine_area,
                            l.all_product,
                            l.count_cutter,
                            l.count_press_a,
                            l.count_press_b,
                            l.last_updated
                        FROM machine_process_logs l
                        JOIN machines m ON l.machine_id = m.machine_id
                        ORDER BY l.{orderBy} DESC
                        LIMIT 20"; // Show top 20 active machines

                    var data = await conn.QueryAsync(sql);
                    
                    UpdateChart(data, label);
                    
                    _lblStatus.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
                    _lblStatus.ForeColor = AppColors.Success;
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Error loading data";
                _lblStatus.ForeColor = AppColors.Error;
            }
        }

        private void UpdateChart(System.Collections.Generic.IEnumerable<dynamic> data, string seriesName)
        {
            _chart.Series.Clear();
            
            var series = new Series(seriesName);
            series.ChartType = SeriesChartType.Column;
            series.IsValueShownAsLabel = true;
            series.Color = AppColors.Primary;

            foreach (var row in data)
            {
                // Machine Label: "TRX.01"
                string machineLabel = $"{row.machine_area}.{row.machine_number}";
                
                long val = 0;
                if (seriesName == "Total Product") val = row.all_product;
                else if (seriesName == "Cutter Count") val = row.count_cutter;
                else if (seriesName == "Press A") val = row.count_press_a;
                else if (seriesName == "Press B") val = row.count_press_b;

                series.Points.AddXY(machineLabel, val);
            }

            _chart.Series.Add(series);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
