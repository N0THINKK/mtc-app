using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // [NEW] For Regex
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
        private Panel _pnlChartContainer; // [NEW] Container for scrolling
        private ComboBox _comboMetric;
        private ComboBox _comboArea;      // [NEW] Filter Area
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
            public double Efficiency => MonitorTime > 0 ? (AutoTime / MonitorTime) * 100 : 0;
        }

        public MachineMonitorControl()
        {
            InitializeComponent();
            SetupTimer();
            LoadAreas(); // [NEW] Populate Area Filter
        }

        private async void LoadAreas()
        {
            try
            {
                _comboArea.Items.Add("Semua Area");
                _comboArea.SelectedIndex = 0;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var areas = await conn.QueryAsync<string>("SELECT DISTINCT machine_area FROM machines ORDER BY machine_area");
                    foreach (var area in areas) _comboArea.Items.Add(area);
                }
            }
            catch { /* Ignore */ }
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.CardBackground;
            this.Padding = new Padding(AppDimens.MarginLarge);

            // 1. Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = AppDimens.RowHeight }; // [FIX] Standard height
            
            var lblTitle = new Label 
            { 
                Text = "Monitoring Output Produksi", 
                Font = AppFonts.MetricSmall,
                AutoSize = true,
                Location = new Point(0, 5)
            };

            _lblStatus = new Label
            {
                Text = "Memuat data...",
                Font = AppFonts.BodySmall,
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(300, 12) // [FIX] Beside Title
            };

            // Area Filter (Right Aligned)
            var lblArea = new Label { Text = "Area:", AutoSize = true, Location = new Point(pnlHeader.Width - 420, 15), Font = AppFonts.BodySmall, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _comboArea = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Location = new Point(pnlHeader.Width - 380, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = AppFonts.BodySmall
            };
            _comboArea.SelectedIndexChanged += async (s, e) => await LoadData();

            // Metric Filter (Right Aligned)
            var lblMetric = new Label { Text = "Metrik:", AutoSize = true, Location = new Point(pnlHeader.Width - 260, 15), Font = AppFonts.BodySmall, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _comboMetric = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Location = new Point(pnlHeader.Width - 210, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = AppFonts.BodySmall
            };
            // [FIX] Restore Items
            _comboMetric.Items.AddRange(new object[] { "Produksi (Output)", "Efisiensi (Waktu)" });
            _comboMetric.SelectedIndex = 0;
            _comboMetric.SelectedIndexChanged += async (s, e) => await LoadData();

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, _lblStatus, lblArea, _comboArea, lblMetric, _comboMetric });
            this.Controls.Add(pnlHeader);

            // 2. Chart Container (Scrollable)
            _pnlChartContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            // 3. Chart
            _chart = new Chart();
            // _chart.Dock = DockStyle.Fill; // [CHANGE] Don't fill, we manage width manually
            _chart.Dock = DockStyle.Left;  // Align left to allow scrolling
            _chart.BackColor = Color.White;
            _chart.Height = _pnlChartContainer.Height - 20; // Fit container height
            
            var chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Angle = -45;
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            
            // Primary Y Axis
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            
            // Secondary Y Axis
            chartArea.AxisY2.Enabled = AxisEnabled.Auto;
            chartArea.AxisY2.MajorGrid.Enabled = false;
            
            _chart.ChartAreas.Add(chartArea);

            var legend = new Legend("MainLegend");
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Center;
            _chart.Legends.Add(legend);

            _pnlChartContainer.Controls.Add(_chart);
            this.Controls.Add(_pnlChartContainer);
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
                // 1. Get List of Machines from DB (With Area Filter)
                IEnumerable<dynamic> machines;
                string selectedArea = _comboArea.SelectedItem?.ToString();
                string sql = "SELECT machine_id, machine_type, machine_area, machine_number FROM machines";
                
                using (var conn = DatabaseHelper.GetConnection())
                {
                    if (!string.IsNullOrEmpty(selectedArea) && selectedArea != "Semua Area")
                    {
                        sql += " WHERE machine_area = @Area";
                        machines = await conn.QueryAsync(sql, new { Area = selectedArea });
                    }
                    else
                    {
                        machines = await conn.QueryAsync(sql + " ORDER BY machine_type, machine_area, machine_number");
                    }
                    
                    // History Query (Keep simplified for now or add Area filter there too if optimized)
                    // ... (Skipping history for Raw Data mode)
                }

                // 2. Process each machine (Fetch ALL data)
                var machineList = new List<MachineData>();
                string selectedMetric = _comboMetric.SelectedItem?.ToString() ?? "Produksi (Output)";

                foreach (var m in machines)
                {
                    // ... (Parsing logic remains same) ...
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

                    // [FIX] Hide inactive machines
                    if (data.ProducedLots == 0 && data.ProducedPieces == 0 && data.MonitorTime == 0) continue;

                    machineList.Add(data);
                }

                // 3. Sorting (Desc)
                if (selectedMetric.Contains("Produksi"))
                {
                    machineList = machineList.OrderByDescending(x => x.ProducedPieces).ToList();
                }
                else
                {
                    machineList = machineList.OrderByDescending(x => x.Efficiency).ToList();
                }

                UpdateChart(machineList, selectedMetric);
                _lblStatus.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss} | Aktif: {machineList.Count}";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private void UpdateChart(List<MachineData> data, string mode)
        {
            // [FIX] Dynamic Width for Scrolling (Min 100px per bar)
            int requiredWidth = Math.Max(_pnlChartContainer.Width, data.Count * 100);
            _chart.Width = requiredWidth;

            _chart.Series.Clear();
            var area = _chart.ChartAreas[0];

            // Reset Axis Scaling
            area.AxisY.Maximum = Double.NaN;
            area.AxisY.Minimum = 0; // Always start from 0
            area.AxisY.Title = "";
            area.AxisY2.Maximum = Double.NaN;
            area.AxisY2.Minimum = 0;
            area.AxisY2.Title = "";
            area.RecalculateAxesScale();
            
            if (mode.Contains("Produksi"))
            {
                area.AxisY.Title = "Lots (Ikat)";
                area.AxisY2.Title = "Pieces (Pcs)";
                area.AxisY2.Enabled = AxisEnabled.True;

                // Series 1: Lots (Primary Axis)
                var sLots = new Series("Lots (Kiri)") { 
                    ChartType = SeriesChartType.Column, 
                    Color = AppColors.Primary 
                };
                sLots["PointWidth"] = "0.8"; // [FIX] Wider Bars
                
                // Series 2: Pieces (Secondary Axis - Right)
                var sPcs = new Series("Pieces (Kanan)") { 
                    ChartType = SeriesChartType.Column, 
                    Color = AppColors.Success, 
                    YAxisType = AxisType.Secondary 
                };
                sPcs["PointWidth"] = "0.8";

                foreach (var item in data)
                {
                    sLots.Points.AddXY(item.MachineName, item.ProducedLots);
                    sPcs.Points.AddXY(item.MachineName, item.ProducedPieces);
                }
                _chart.Series.Add(sLots);
                _chart.Series.Add(sPcs);
            }
            else // Efisiensi (Stacked: Auto vs Loss)
            {
                area.AxisY.Title = "Waktu (Menit)";
                area.AxisY.Maximum = Double.NaN; // Auto scale for time
                area.AxisY2.Enabled = AxisEnabled.False;

                // Series 1: Auto Time (Green - Bottom)
                var sAuto = new Series("Auto Time") { 
                    ChartType = SeriesChartType.StackedColumn, 
                    Color = AppColors.Success,
                    IsValueShownAsLabel = true 
                };
                sAuto["PointWidth"] = "0.8"; // [FIX] Wider Bars

                // Series 2: Loss Time (Red - Top)
                var sLoss = new Series("Loss Time") { 
                    ChartType = SeriesChartType.StackedColumn, 
                    Color = AppColors.Danger,
                    IsValueShownAsLabel = true 
                };
                sLoss["PointWidth"] = "0.8";

                // Series 3: Eff Label (Point Series - Floating Label)
                var sEffLabel = new Series("Eff Label") {
                    ChartType = SeriesChartType.Point, // [FIX] Use Point to float
                    Color = Color.Transparent,
                    IsValueShownAsLabel = true
                };
                sEffLabel["LabelStyle"] = "Top"; // Force Top

                foreach (var item in data)
                {
                    double autoMin = item.AutoTime / 60.0;
                    double monMin = item.MonitorTime / 60.0;
                    double lossMin = (monMin > autoMin) ? (monMin - autoMin) : 0;
                    
                    double eff = (item.MonitorTime > 0) ? (item.AutoTime / item.MonitorTime) * 100.0 : 0;

                    // 1. Auto Point (Green)
                    int pIndexAuto = sAuto.Points.AddXY(item.MachineName, autoMin);
                    sAuto.Points[pIndexAuto].Label = $"{autoMin:F0}m";

                    // 2. Loss Point (Red)
                    int pIndexLoss = sLoss.Points.AddXY(item.MachineName, lossMin);
                    sLoss.Points[pIndexLoss].Label = $"{monMin:F0}m"; 

                    // 3. Label Point (Floating) -> Label: "85%"
                    // Position: Top of Stack (monMin) + Buffer
                    double labelY = monMin + (monMin * 0.05); // 5% above
                    if (labelY == 0) labelY = 1; // Safety for 0 height

                    int pIndexLabel = sEffLabel.Points.AddXY(item.MachineName, labelY); 
                    sEffLabel.Points[pIndexLabel].Label = $"{eff:F1}%";
                    sEffLabel.Points[pIndexLabel].MarkerStyle = MarkerStyle.None; // Hide dot
                }
                _chart.Series.Add(sAuto);
                _chart.Series.Add(sLoss);
                _chart.Series.Add(sEffLabel); // Added Last = Top of Stack
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
                    // [FIX] Use Regex to extract digits, ignoring Japanese chars or separators
                    var match = Regex.Match(line, @"\d+");
                    if (match.Success && long.TryParse(match.Value, out long val))
                    {
                        return val;
                    }
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