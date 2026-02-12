using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; 
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
        private Panel _pnlChartContainer; // Container for scrolling
        private ComboBox _comboMetric;
        private ComboBox _comboArea;      // Filter Area
        private Label _lblStatus;
        
        private System.ComponentModel.IContainer components = null;

        // Data Structure updated to hold Speed
        private class MachineData
        {
            public string MachineName { get; set; }
            public long ProducedLots { get; set; }
            public long ProducedPieces { get; set; }
            public double SpeedPerHour { get; set; } // [NEW] Property for Speed
            public double AutoTime { get; set; }
            public double MonitorTime { get; set; }
            public double Efficiency => MonitorTime > 0 ? (AutoTime / MonitorTime) * 100 : 0;
        }

        public MachineMonitorControl()
        {
            InitializeComponent();
            SetupTimer();
            LoadAreas(); 
        }

        private async void LoadAreas()
        {
            try
            {
                _comboArea.Items.Add("Semua Area");
                _comboArea.SelectedIndex = 0;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var areas = await conn.QueryAsync<string>("SELECT area_name FROM machine_areas ORDER BY area_name");
                    foreach (var area in areas) _comboArea.Items.Add(area);
                }
            }
            catch { /* Ignore */ }
        }

        // ========================================================
        // UI Construction
        // ========================================================
        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.CardBackground;
            this.Padding = new Padding(AppDimens.MarginLarge);

            // 1. Header
            var pnlHeader = BuildHeaderPanel();
            this.Controls.Add(pnlHeader);

            // 2. Chart Container
            _pnlChartContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            // 3. Chart
            _chart = new Chart();
            _chart.Dock = DockStyle.Left;  
            _chart.BackColor = Color.White;
            _chart.Height = _pnlChartContainer.Height - 20; 
            
            var chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Angle = -45;
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            
            // Primary Y Axis
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            
            // Secondary Y Axis (Disabled)
            chartArea.AxisY2.Enabled = AxisEnabled.False;
            
            _chart.ChartAreas.Add(chartArea);

            var legend = new Legend("MainLegend");
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Center;
            _chart.Legends.Add(legend);

            _pnlChartContainer.Controls.Add(_chart);
            this.Controls.Add(_pnlChartContainer);
        }

        private Panel BuildHeaderPanel()
        {
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = AppDimens.RowHeight };

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Left: Title + Status
            var flowLeft = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label 
            { 
                Text = "Monitoring Mesin Real-time", 
                Font = AppFonts.MetricSmall,
                AutoSize = true,
                Margin = new Padding(0, 5, AppDimens.MarginLarge, 0)
            };

            _lblStatus = new Label
            {
                Text = "Memuat data...",
                Font = AppFonts.BodySmall,
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };

            flowLeft.Controls.AddRange(new Control[] { lblTitle, _lblStatus });
            headerLayout.Controls.Add(flowLeft, 0, 0);

            // Right: Filters
            var flowRight = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            _comboMetric = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180, // Dilebarkan sedikit
                Font = AppFonts.BodySmall,
                Margin = new Padding(0, 10, 0, 0)
            };
            // [UPDATE] Opsi diganti ke Speed
            _comboMetric.Items.AddRange(new object[] { "Speed Produksi (Pcs/Jam)", "Efisiensi (Waktu)" });
            _comboMetric.SelectedIndex = 0;
            _comboMetric.SelectedIndexChanged += async (s, e) => await LoadData();

            var lblMetric = new Label 
            { 
                Text = "Metrik:", 
                AutoSize = true, 
                Font = AppFonts.BodySmall,
                Margin = new Padding(0, 13, AppDimens.MarginSmall, 0)
            };

            _comboArea = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Font = AppFonts.BodySmall,
                Margin = new Padding(0, 10, AppDimens.MarginLarge, 0)
            };
            _comboArea.SelectedIndexChanged += async (s, e) => await LoadData();

            var lblArea = new Label 
            { 
                Text = "Area:", 
                AutoSize = true, 
                Font = AppFonts.BodySmall,
                Margin = new Padding(0, 13, AppDimens.MarginSmall, 0)
            };

            flowRight.Controls.AddRange(new Control[] { _comboMetric, lblMetric, _comboArea, lblArea });
            headerLayout.Controls.Add(flowRight, 1, 0);

            pnlHeader.Controls.Add(headerLayout);
            return pnlHeader;
        }

        // ========================================================
        // Timer & Monitoring
        // ========================================================
        private void SetupTimer()
        {
            _timer = new Timer();
            _timer.Interval = REFRESH_RATE_MS;
            _timer.Tick += async (s, e) => await LoadData();
        }

        public void StartMonitoring() { _ = LoadData(); _timer.Start(); }
        public void StopMonitoring() { _timer.Stop(); }

        public void SetMetric(int index)
        {
            if (index >= 0 && index < _comboMetric.Items.Count)
            {
                _comboMetric.SelectedIndex = index;
            }
        }

        private async Task LoadData()
        {
            try
            {
                string selectedArea = _comboArea.SelectedItem?.ToString();
                
                // [UPDATE] Query Database: Join dengan Log Terakhir untuk referensi Speed
                string sql = @"
                    SELECT m.machine_id, 
                           COALESCE(t.type_name, 'UNK') AS type_name, 
                           COALESCE(a.area_name, 'UNK') AS area_name, 
                           m.machine_number,
                           l.produced_pieces AS last_log_pieces,
                           l.created_at AS last_log_time
                    FROM machines m
                    LEFT JOIN machine_types t ON m.type_id = t.type_id
                    LEFT JOIN machine_areas a ON m.area_id = a.area_id
                    LEFT JOIN (
                        SELECT machine_id, produced_pieces, created_at
                        FROM machine_process_logs
                        WHERE log_id IN (
                            SELECT MAX(log_id) FROM machine_process_logs GROUP BY machine_id
                        )
                    ) l ON m.machine_id = l.machine_id";
                
                IEnumerable<dynamic> machines;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    if (!string.IsNullOrEmpty(selectedArea) && selectedArea != "Semua Area")
                    {
                        sql += " WHERE a.area_name = @Area";
                        machines = await conn.QueryAsync(sql, new { Area = selectedArea });
                    }
                    else
                    {
                        machines = await conn.QueryAsync(sql + " ORDER BY t.type_name, a.area_name, m.machine_number");
                    }
                }

                // 2. Process each machine
                var machineList = new List<MachineData>();
                string selectedMetric = _comboMetric.SelectedItem?.ToString() ?? "Speed Produksi (Pcs/Jam)";

                foreach (var m in machines)
                {
                    var data = new MachineData { 
                        MachineName = $"{m.type_name}.{m.area_name}-{m.machine_number}" 
                    };
                    string type = m.type_name.ToString().ToUpper();

                    // --- READ REAL-TIME DATA (INI FILES) ---
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
                    if (data.ProducedPieces == 0 && data.MonitorTime == 0) continue;

                    // --- CALCULATE SPEED (Pcs/Hour) based on DB Log ---
                    if (m.last_log_time != null)
                    {
                        DateTime lastTime = m.last_log_time;
                        long lastPieces = m.last_log_pieces ?? 0;
                        double hoursElapsed = (DateTime.Now - lastTime).TotalHours;

                        // Hanya hitung jika log cukup baru (< 4 jam) untuk menghindari data kemarin
                        if (hoursElapsed > 0.001 && hoursElapsed < 4.0) 
                        {
                            long diffPieces = data.ProducedPieces - lastPieces;
                            
                            // Jika negatif, berarti mesin baru di-reset counter-nya.
                            // Kita asumsikan mulai dari 0, jadi diff = data.ProducedPieces
                            if (diffPieces < 0) diffPieces = data.ProducedPieces;

                            data.SpeedPerHour = diffPieces / hoursElapsed;
                        }
                    }
                    else
                    {
                        // Jika tidak ada log DB sama sekali, speed 0 (tunggu log pertama dibuat logger)
                        data.SpeedPerHour = 0; 
                    }

                    machineList.Add(data);
                }

                // 3. Sorting (Desc)
                if (selectedMetric.Contains("Speed"))
                {
                    machineList = machineList.OrderByDescending(x => x.SpeedPerHour).ToList();
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
            int requiredWidth = Math.Max(_pnlChartContainer.Width, data.Count * 100);
            _chart.Width = requiredWidth;

            _chart.Series.Clear();
            var area = _chart.ChartAreas[0];

            area.AxisY.Maximum = Double.NaN;
            area.AxisY.Minimum = 0; 
            area.AxisY.Title = "";
            area.RecalculateAxesScale();
            
            if (mode.Contains("Speed"))
            {
                // --- MODE SPEED (PCS/JAM) ---
                area.AxisY.Title = "Speed (Pcs/Jam)";

                var sSpeed = new Series("Speed") { 
                    ChartType = SeriesChartType.Column, 
                    Color = AppColors.Primary, // Biru
                    IsValueShownAsLabel = true 
                };
                sSpeed["PointWidth"] = "0.8";

                foreach (var item in data)
                {
                    // Tampilkan Speed
                    int idx = sSpeed.Points.AddXY(item.MachineName, (int)item.SpeedPerHour);
                    
                    // Format Label: "2,500"
                    sSpeed.Points[idx].Label = $"{item.SpeedPerHour:N0}"; 
                }
                _chart.Series.Add(sSpeed);
            }
            else 
            {
                // --- MODE EFISIENSI ---
                area.AxisY.Title = "Waktu (Menit)";

                var sAuto = new Series("Auto Time") { 
                    ChartType = SeriesChartType.StackedColumn, 
                    Color = AppColors.Success,
                    IsValueShownAsLabel = true 
                };
                sAuto["PointWidth"] = "0.8"; 

                var sLoss = new Series("Loss Time") { 
                    ChartType = SeriesChartType.StackedColumn, 
                    Color = AppColors.Danger,
                    IsValueShownAsLabel = true 
                };
                sLoss["PointWidth"] = "0.8";

                var sEffLabel = new Series("Eff Label") {
                    ChartType = SeriesChartType.Point, 
                    Color = Color.Transparent,
                    IsValueShownAsLabel = true
                };
                sEffLabel["LabelStyle"] = "Top"; 

                foreach (var item in data)
                {
                    double autoMin = item.AutoTime / 60.0;
                    double monMin = item.MonitorTime / 60.0;
                    double lossMin = (monMin > autoMin) ? (monMin - autoMin) : 0;
                    double eff = (item.MonitorTime > 0) ? (item.AutoTime / item.MonitorTime) * 100.0 : 0;

                    int pIndexAuto = sAuto.Points.AddXY(item.MachineName, autoMin);
                    sAuto.Points[pIndexAuto].Label = $"{autoMin:F0}m";

                    int pIndexLoss = sLoss.Points.AddXY(item.MachineName, lossMin);
                    sLoss.Points[pIndexLoss].Label = $"{monMin:F0}m"; 

                    double labelY = monMin + (monMin * 0.05); 
                    if (labelY == 0) labelY = 1; 

                    int pIndexLabel = sEffLabel.Points.AddXY(item.MachineName, labelY); 
                    sEffLabel.Points[pIndexLabel].Label = $"{eff:F1}%";
                    sEffLabel.Points[pIndexLabel].MarkerStyle = MarkerStyle.None; 
                }
                _chart.Series.Add(sAuto);
                _chart.Series.Add(sLoss);
                _chart.Series.Add(sEffLabel); 
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