using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Dapper;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class MachineMonitorControl : UserControl
    {
        private const int REFRESH_RATE_MS = 5000; 

        private Timer _timer;
        private Chart _chart;
        private ComboBox _comboMetric;
        private Label _lblStatus;
        
        private System.ComponentModel.IContainer components = null;

        // Data Structure to hold all metrics
        private class MachineData
        {
            public string MachineName { get; set; }
            public long ProducedLots { get; set; }
            public long ProducedPieces { get; set; }
            public double AutoTime { get; set; }
            public double MonitorTime { get; set; }
        }

        public MachineMonitorControl()
        {
            InitializeComponent();
            SetupTimer();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.CardBackground;
            this.Padding = new Padding(AppDimens.MarginLarge);

            // 1. Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = AppDimens.RowHeight };
            
            var lblTitle = new Label 
            { 
                Text = "Monitoring Output Produksi (Realtime)", 
                Font = AppFonts.MetricSmall,
                AutoSize = true,
                Location = new Point(0, 5)
            };

            _lblStatus = new Label
            {
                Text = "Scanning...",
                Font = AppFonts.BodySmall,
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(350, 12)
            };

            // Metric Filter
            _comboMetric = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Location = new Point(pnlHeader.Width - 200, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = AppFonts.BodySmall
            };
            // Only 2 Options for Grouped View
            _comboMetric.Items.AddRange(new object[] { "Produksi (Output)", "Efisiensi (Waktu)" });
            _comboMetric.SelectedIndex = 0;
            _comboMetric.SelectedIndexChanged += async (s, e) => await LoadData();

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(_lblStatus);
            pnlHeader.Controls.Add(_comboMetric);
            this.Controls.Add(pnlHeader);

            // 2. Chart
            _chart = new Chart();
            _chart.Dock = DockStyle.Fill;
            _chart.BackColor = Color.White;
            
            var chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Angle = -45;
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            
            // Primary Y Axis
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            
            // Secondary Y Axis (For Pieces which have huge numbers)
            chartArea.AxisY2.Enabled = AxisEnabled.Auto;
            chartArea.AxisY2.MajorGrid.Enabled = false;
            
            _chart.ChartAreas.Add(chartArea);

            var legend = new Legend("MainLegend");
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Center;
            _chart.Legends.Add(legend);

            this.Controls.Add(_chart);
        }

        private void SetupTimer()
        {
            _timer = new Timer();
            _timer.Interval = REFRESH_RATE_MS;
            _timer.Tick += async (s, e) => await LoadData();
        }

        public void StartMonitoring() { _ = LoadData(); _timer.Start(); }
        public void StopMonitoring() { _timer.Stop(); }

        private async Task LoadData()
        {
            try
            {
                // 1. Get List of Machines from DB
                IEnumerable<dynamic> machines;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    machines = await conn.QueryAsync("SELECT machine_type, machine_area, machine_number FROM machines ORDER BY machine_type, machine_area, machine_number");
                }

                // 2. Process each machine (Fetch ALL data)
                var machineList = new List<MachineData>();
                string selectedMetric = _comboMetric.SelectedItem?.ToString() ?? "Produksi (Output)";

                foreach (var m in machines)
                {
                    var data = new MachineData { 
                        MachineName = $"{m.machine_type}-{m.machine_area}.{m.machine_number}" 
                    };
                    string type = m.machine_type.ToString().ToUpper();

                    // AC90
                    if (type.Contains("AC90"))
                    {
                        string pathProd = @"C:\AC90HMI\prg\INI\HmiProcess.ini";
                        string pathEff = @"C:\AC90HMI\prg\INI\HmiProcess2.ini";
                        
                        if (File.Exists(pathProd)) {
                            data.ProducedLots = ParseLineValue(pathProd, 2);
                            data.ProducedPieces = ParseLineValue(pathProd, 3);
                        }
                        if (File.Exists(pathEff)) {
                            data.AutoTime = ParseIniValue(pathEff, "AutoTime");
                            data.MonitorTime = ParseIniValue(pathEff, "MonitorTime");
                        }
                    }
                    // AC95
                    else if (type.Contains("AC95"))
                    {
                        string path = @"D:\AC95\Product\Information.ini";
                        if (File.Exists(path)) {
                            data.ProducedLots = (long)ParseIniValue(path, "ProducedLots");
                            data.ProducedPieces = (long)ParseIniValue(path, "ProducedPieces");
                            data.AutoTime = ParseIniValue(path, "AutoTime");
                            data.MonitorTime = ParseIniValue(path, "MonitorTime");
                        }
                    }
                    // AC80/81
                    else if (type.Contains("AC80") || type.Contains("AC81"))
                    {
                        string folder = type.Contains("81") ? "AC81" : "AC80";
                        string path = $@"C:\{folder}HMI\{folder}\{folder}";
                        
                        if (!File.Exists(path) && File.Exists(path + ".ini")) path += ".ini";
                        
                        if (File.Exists(path)) {
                            var vals = FindNumericValues(path, 2);
                            if (vals.Count >= 1) data.ProducedLots = vals[0];
                            if (vals.Count >= 2) data.ProducedPieces = vals[1];
                        }
                    }

                    // Add to list (even if 0, to show machine exists, or filter out 0s)
                    // Let's filter out completely dead machines to save chart space
                    if (data.ProducedLots > 0 || data.ProducedPieces > 0 || data.AutoTime > 0)
                    {
                        machineList.Add(data);
                    }
                }

                UpdateChart(machineList, selectedMetric);
                _lblStatus.Text = $"Updated: {DateTime.Now:HH:mm:ss} | Active: {machineList.Count}";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private void UpdateChart(List<MachineData> data, string mode)
        {
            _chart.Series.Clear();
            
            if (mode.Contains("Produksi"))
            {
                // Series 1: Lots (Primary Axis)
                var sLots = new Series("Lots (Kiri)") { 
                    ChartType = SeriesChartType.Column, 
                    Color = AppColors.Primary 
                };
                
                // Series 2: Pieces (Secondary Axis - Right)
                var sPcs = new Series("Pieces (Kanan)") { 
                    ChartType = SeriesChartType.Column, 
                    Color = AppColors.Success, 
                    YAxisType = AxisType.Secondary 
                };

                foreach (var item in data)
                {
                    sLots.Points.AddXY(item.MachineName, item.ProducedLots);
                    sPcs.Points.AddXY(item.MachineName, item.ProducedPieces);
                }
                _chart.Series.Add(sLots);
                _chart.Series.Add(sPcs);
            }
            else // Efisiensi
            {
                // Series 1: Auto Time
                var sAuto = new Series("Auto Time") { 
                    ChartType = SeriesChartType.Column, 
                    Color = AppColors.Warning 
                };
                
                // Series 2: Monitor Time
                var sMon = new Series("Monitor Time") { 
                    ChartType = SeriesChartType.Column, 
                    Color = AppColors.Danger 
                };

                foreach (var item in data)
                {
                    sAuto.Points.AddXY(item.MachineName, item.AutoTime);
                    sMon.Points.AddXY(item.MachineName, item.MonitorTime);
                }
                _chart.Series.Add(sAuto);
                _chart.Series.Add(sMon);
            }
        }

        // --- Helpers ---

        private double ParseIniValue(string path, string key)
        {
            try
            {
                var lines = File.ReadAllLines(path, Encoding.Default);
                foreach (var line in lines)
                {
                    if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split('=');
                        if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out double val)) return val;
                    }
                }
            }
            catch { }
            return 0;
        }

        private long ParseLineValue(string path, int lineIndex)
        {
            try
            {
                var lines = File.ReadAllLines(path, Encoding.Default);
                if (lineIndex < lines.Length)
                {
                    string line = lines[lineIndex];
                    string valuePart = line.Contains("=") ? line.Split('=')[1].Trim() : line.Trim();
                    if (long.TryParse(valuePart, out long val)) return val;
                }
            }
            catch { }
            return 0;
        }

        private List<long> FindNumericValues(string path, int count)
        {
            var results = new List<long>();
            try
            {
                var lines = File.ReadAllLines(path, Encoding.Default);
                foreach (var line in lines)
                {
                    if (line.Contains("="))
                    {
                        string valPart = line.Split('=')[1].Trim();
                        if (long.TryParse(valPart, out long val) && val > 0) 
                        {
                            results.Add(val);
                            if (results.Count >= count) break;
                        }
                    }
                }
            }
            catch { }
            return results;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null) components.Dispose();
                if (_timer != null) { _timer.Stop(); _timer.Dispose(); }
            }
            base.Dispose(disposing);
        }
    }
}