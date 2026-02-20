using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
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
        private Panel _pnlChartContainer;
        private ComboBox _comboMetric;
        private ComboBox _comboArea;
        private Label _lblStatus;
        
        private System.ComponentModel.IContainer components = null;

        private class MachineData
        {
            public string MachineName { get; set; }
            public long ProducedPieces { get; set; } 
            
            // [BARU] Dictionary untuk menyimpan produksi per jam (Index 0-11)
            public Dictionary<int, long> HourlyProduction { get; set; } = new Dictionary<int, long>();
            
            // [HAPUS] legacy MinuteAverage
            // public long MinuteAverage { get; set; } 
            
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
            catch { }
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.CardBackground;
            this.Padding = new Padding(AppDimens.MarginLarge);

            var pnlHeader = BuildHeaderPanel();
            this.Controls.Add(pnlHeader);

            _pnlChartContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            
            _chart = new Chart();
            _chart.Dock = DockStyle.Left;  
            _chart.BackColor = Color.White;
            _chart.Height = _pnlChartContainer.Height - 20; 
            
            var chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Angle = -45;
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            
            _chart.ChartAreas.Add(chartArea);

            var legend = new Legend("MainLegend");
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Center;
            _chart.Legends.Add(legend);

            _pnlChartContainer.Controls.Add(_chart);
            this.Controls.Add(_pnlChartContainer);

            var lblTitle = new Label { Text = "Monitoring Mesin Real-time", Font = AppFonts.PageTitle, ForeColor = AppColors.TextPrimary, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top, Height = 40 };
            this.Controls.Add(lblTitle);
        }

        private Panel BuildHeaderPanel()
        {
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = AppDimens.RowHeight };
            var headerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            
            var flowLeft = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _lblStatus = new Label { Text = "Memuat data...", Font = AppFonts.BodySmall, ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            flowLeft.Controls.Add(_lblStatus);
            headerLayout.Controls.Add(flowLeft, 0, 0);

            var flowRight = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, BackColor = Color.Transparent };
            
            _comboMetric = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 0, 0) };
            
            // [UPDATE LABEL] Menjadi "Rata-rata 1 Menit"
            _comboMetric.Items.AddRange(new object[] { "Rata-rata 1 Menit (Pcs)", "Efisiensi Mesin" });
            _comboMetric.SelectedIndex = 0;
            _comboMetric.SelectedIndexChanged += async (s, e) => await LoadData();

            var lblMetric = new Label { Text = "Metrik:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, AppDimens.MarginSmall, 0) };

            _comboArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, AppDimens.MarginLarge, 0) };
            _comboArea.SelectedIndexChanged += async (s, e) => await LoadData();
            var lblArea = new Label { Text = "Area:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, AppDimens.MarginSmall, 0) };

            flowRight.Controls.AddRange(new Control[] { _comboMetric, lblMetric, _comboArea, lblArea });
            headerLayout.Controls.Add(flowRight, 1, 0);
            pnlHeader.Controls.Add(headerLayout);
            return pnlHeader;
        }

        private void SetupTimer()
        {
            _timer = new Timer();
            _timer.Interval = REFRESH_RATE_MS;
            _timer.Tick += async (s, e) => await LoadData();
        }

        public void StartMonitoring() { _ = LoadData(); _timer.Start(); }
        public void StopMonitoring() { _timer.Stop(); }

        public void SetMetric(int index) { if (index >= 0 && index < _comboMetric.Items.Count) _comboMetric.SelectedIndex = index; }

        private async Task LoadData()
        {
            try
            {
                string selectedArea = _comboArea.SelectedItem?.ToString();
                
                // [BARU] Tentukan Awal Shift
                DateTime shiftStart = GetCurrentShiftStart();
                DateTime nextShiftStart = shiftStart.AddHours(shiftStart.Hour == 7 ? 12 : 9); // Estimasi durasi shift

                string sql = @"
                    SELECT m.machine_id, 
                           COALESCE(t.type_name, 'UNK') AS type_name, 
                           COALESCE(a.area_name, 'UNK') AS area_name, 
                           m.machine_number,
                           l.produced_pieces,
                           l.auto_time,
                           l.monitor_time,
                           l.created_at
                    FROM machines m
                    LEFT JOIN machine_types t ON m.type_id = t.type_id
                    LEFT JOIN machine_areas a ON m.area_id = a.area_id
                    -- [FIX] Gunakan LEFT JOIN agar mesin tetap muncul meski belum ada log shift ini
                    LEFT JOIN machine_process_logs l ON m.machine_id = l.machine_id AND l.created_at >= @ShiftStart
                    ORDER BY m.machine_id, l.created_at ASC";

                IEnumerable<dynamic> logs;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    if (!string.IsNullOrEmpty(selectedArea) && selectedArea != "Semua Area")
                    {
                        sql += " AND a.area_name = @Area";
                        logs = await conn.QueryAsync(sql, new { ShiftStart = shiftStart, Area = selectedArea });
                    }
                    else
                    {
                        logs = await conn.QueryAsync(sql, new { ShiftStart = shiftStart });
                    }
                }

                // [BARU] Proses Data untuk Hourly Stacking
                var machineList = ProcessHourlyData(logs, shiftStart);

                // Sorting
                string selectedMetric = _comboMetric.SelectedItem?.ToString();
                if (selectedMetric.Contains("Efisiensi"))
                {
                    machineList = machineList.OrderByDescending(x => x.Efficiency).ToList();
                }
                else
                {
                    machineList = machineList.OrderByDescending(x => x.ProducedPieces).ToList();
                }

                UpdateChart(machineList, selectedMetric, shiftStart);
                _lblStatus.Text = $"Shift: {shiftStart:HH:mm} | Update: {DateTime.Now:HH:mm:ss} | Aktif: {machineList.Count}";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private DateTime GetCurrentShiftStart()
        {
            DateTime now = DateTime.Now;
            // Shift 1: 07:00 - 18:59 -> Start Today 07:00
            // Shift 2: 19:00 - 06:59 -> Start Today 19:00 OR Yesterday 19:00
            
            if (now.Hour >= 7 && now.Hour < 19)
            {
                return now.Date.AddHours(7);
            }
            else
            {
                // Jika jam 00:00 - 06:59, berarti shift mulai kemarin jam 19:00
                if (now.Hour < 7) return now.Date.AddDays(-1).AddHours(19);
                // Jika jam 19:00 - 23:59, berarti shift mulai hari ini jam 19:00
                return now.Date.AddHours(19);
            }
        }

        private List<MachineData> ProcessHourlyData(IEnumerable<dynamic> logs, DateTime shiftStart)
        {
            var result = new List<MachineData>();
            
            // Group logs per Machine
            var machineGroups = logs.GroupBy(l => (int)l.machine_id);

            foreach (var grp in machineGroups)
            {
                var firstRow = grp.First(); // Info mesin (nama, tipe) selalu ada
                
                // Ambil log yang valid saja (dimana produced_pieces TIDAK NULL)
                // Karena LEFT JOIN, jika tidak ada log, produced_pieces akan null
                var validLogs = grp.Where(x => x.produced_pieces != null)
                                   .OrderBy(x => x.created_at)
                                   .ToList();

                var md = new MachineData
                {
                    MachineName = $"{firstRow.type_name}.{firstRow.area_name}-{firstRow.machine_number}",
                    HourlyProduction = new Dictionary<int, long>() 
                };

                // Jika tidak ada log valid, berarti mesin ini belum ada data di shift ini
                if (!validLogs.Any()) 
                {
                    md.ProducedPieces = 0;
                    md.AutoTime = 0;
                    md.MonitorTime = 0;
                    result.Add(md);
                    continue; 
                }

                // --- LOGIKA STACKING PER JAM ---
                
                long lastCounter = 0;
                // Inisialisasi counter awal
                lastCounter = (long)validLogs.First().produced_pieces;

                // Ambil log terakhir untuk Total Saat Ini
                var lastLog = validLogs.Last();
                md.ProducedPieces = (long)lastLog.produced_pieces;
                
                // Handle null untuk auto_time/monitor_time (jaga-jaga)
                md.AutoTime = lastLog.auto_time != null ? (double)lastLog.auto_time : 0;
                md.MonitorTime = lastLog.monitor_time != null ? (double)lastLog.monitor_time : 0;

                // Iterasi setiap jam dalam shift (Max 12 jam)
                for (int i = 0; i < 12; i++)
                {
                    DateTime hourStart = shiftStart.AddHours(i);
                    DateTime hourEnd = hourStart.AddHours(1);
                    
                    if (hourStart > DateTime.Now) break; 

                    // Cari log Paling Akhir di jam ini
                    var logAtEnd = validLogs
                        .Where(l => l.created_at < hourEnd)
                        .LastOrDefault();

                    if (logAtEnd == null) continue;

                    long currentCounter = (long)logAtEnd.produced_pieces;
                    long delta = 0;

                    if (currentCounter >= lastCounter)
                    {
                        delta = currentCounter - lastCounter;
                    }
                    else
                    {
                        // Reset logic: counter reset ke 0
                        delta = currentCounter; 
                    }

                    if (delta > 0) md.HourlyProduction[i] = delta;

                    lastCounter = currentCounter;
                }
                
                result.Add(md);
            }

            return result;
        }

        private void UpdateChart(List<MachineData> data, string mode, DateTime shiftStart)
        {
            int requiredWidth = Math.Max(_pnlChartContainer.Width, data.Count * 120); 
            _chart.Width = requiredWidth;

            _chart.Series.Clear();
            var area = _chart.ChartAreas[0];

            area.AxisY.Maximum = Double.NaN;
            area.AxisY.Minimum = 0;
            area.AxisX.LabelStyle.Angle = 0; // [FIX] Horizontal Labels
            area.RecalculateAxesScale();

            if (mode.Contains("Rata-rata"))
            {
                area.AxisY.Title = "Output (Pcs / Jam)";
                
                // 1. Stacked Bar (Produksi per Jam)
                for (int i = 0; i < 12; i++)
                {
                    DateTime h = shiftStart.AddHours(i);
                    string seriesName = h.ToString("HH:00"); 

                    var series = new Series(seriesName)
                    {
                        ChartType = SeriesChartType.StackedColumn,
                        IsValueShownAsLabel = false, // [FIX] Matikan default label biar '0' tidak muncul
                        Color = GetHourColor(i),
                        Font = new Font("Segoe UI", 8, FontStyle.Bold) 
                    };
                    series["PointWidth"] = "0.7";
                    
                    foreach (var m in data)
                    {
                        long val = m.HourlyProduction.ContainsKey(i) ? m.HourlyProduction[i] : 0;
                        int idx = series.Points.AddXY(m.MachineName, val);
                        
                        // [FIX] Manual Label hanya jika > 0
                        if (val > 0) 
                        {
                            series.Points[idx].Label = $"{val}";
                            series.Points[idx].LabelAngle = 0; 
                        }
                    }
                    _chart.Series.Add(series);
                }

                // 2. Average Line (Total / Jam Berjalan)
                var sAvg = new Series("Avg / Hour") 
                { 
                    ChartType = SeriesChartType.Line, 
                    Color = Color.Red, 
                    BorderWidth = 2,
                    IsValueShownAsLabel = true,
                    LabelForeColor = Color.Red
                };

                // Hitung jam berjalan saat ini (1-12)
                int hoursPassed = (int)(DateTime.Now - shiftStart).TotalHours;
                if (hoursPassed < 1) hoursPassed = 1; 

                foreach (var m in data)
                {
                    // Total Produksi / Jam Berjalan
                    long total = m.ProducedPieces; 
                    double avg = (double)total / hoursPassed;

                    int idx = sAvg.Points.AddXY(m.MachineName, avg);
                    // Tampilkan label di titik warning
                    sAvg.Points[idx].Label = $"Avg: {avg:N0}";
                    sAvg.Points[idx].LabelAngle = 0; 
                    sAvg.Points[idx].MarkerStyle = MarkerStyle.Circle;
                }
                _chart.Series.Add(sAvg);
            }
            else 
            {
                // Mode Efisiensi
                area.AxisY.Title = "Waktu (Menit)";
                var sAuto = new Series("Auto") { ChartType = SeriesChartType.StackedColumn, Color = AppColors.Success, IsValueShownAsLabel = true };
                var sLoss = new Series("Loss") { ChartType = SeriesChartType.StackedColumn, Color = AppColors.Danger, IsValueShownAsLabel = true };

                foreach (var item in data)
                {
                    double autoM = item.AutoTime / 60.0;
                    double monM = item.MonitorTime / 60.0;
                    double lossM = Math.Max(0, monM - autoM);

                    int p1 = sAuto.Points.AddXY(item.MachineName, autoM);
                    sAuto.Points[p1].Label = autoM > 0 ? $"{autoM:N0}" : "";
                    
                    int p2 = sLoss.Points.AddXY(item.MachineName, lossM);
                    sLoss.Points[p2].Label = lossM > 0 ? $"{lossM:N0}" : "";
                }
                _chart.Series.Add(sAuto);
                _chart.Series.Add(sLoss);
            }
        }
        
        private Color GetHourColor(int index)
        {
            // Palet warna gradasi atau distinct
            Color[] colors = {
                Color.FromArgb(65, 140, 240), // Biru
                Color.FromArgb(252, 180, 65), // Kuning
                Color.FromArgb(224, 64, 10),  // Merah
                Color.FromArgb(5, 100, 146),  // Biru Tua
                Color.FromArgb(191, 191, 191),// Abu
                Color.FromArgb(26, 59, 105),  // Navy
                Color.FromArgb(255, 128, 0),  // Orange
                Color.FromArgb(100, 200, 100),// Hijau
                Color.Purple,
                Color.Teal,
                Color.Magenta,
                Color.Brown
            };
            return colors[index % colors.Length];
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