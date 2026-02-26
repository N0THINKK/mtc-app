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
        private const int REFRESH_RATE_MS = 30000; 

        private Timer _timer;
        private Chart _chart;
        private Panel _pnlChartContainer;
        private ComboBox _comboMetric;
        private ComboBox _comboArea;
        private ComboBox _comboSort;
        private ComboBox _comboShift;
        private DateTimePicker _dtpDateFilter;
        private Label _lblStatus;
        private bool _sortAscending = false;
        
        private System.ComponentModel.IContainer components = null;

        private class MachineData
        {
            public string MachineName { get; set; }
            public long[] HourlyPieces { get; set; } = new long[14]; 
            public long TotalPieces { get; set; } 
            public double AveragePerHour { get; set; } 
            
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
            
            // ════ FIX BUG SCROLL WINFORMS ════
            _chart.Dock = DockStyle.Fill;  
            _chart.BackColor = Color.White;

            var chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Angle = -45;
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            
            // Konfigurasi Scrollbar Native Chart
            chartArea.AxisX.ScrollBar.Enabled = true;
            chartArea.AxisX.ScrollBar.IsPositionedInside = false; 
            chartArea.AxisX.ScrollBar.Size = 14;

            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            
            _chart.ChartAreas.Add(chartArea);

            var legend = new Legend("MainLegend");
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Center;
            _chart.Legends.Add(legend);

            _pnlChartContainer.Controls.Add(_chart);
            this.Controls.Add(_pnlChartContainer);

            var lblTitle = new Label { Text = "Monitoring Output & Efisiensi", Font = AppFonts.PageTitle, ForeColor = AppColors.TextPrimary, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top, Height = 40 };
            this.Controls.Add(lblTitle);
        }

        private Panel BuildHeaderPanel()
        {
            var pnlHeader = new Panel { Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 60) };
            var headerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F)); // Kolom kanan diperlebar untuk filter
            
            var flowLeft = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _lblStatus = new Label { Text = "Memuat data...", Font = AppFonts.BodySmall, ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            flowLeft.Controls.Add(_lblStatus);
            headerLayout.Controls.Add(flowLeft, 0, 0);

            var flowRight = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, WrapContents = true, Dock = DockStyle.Fill, BackColor = Color.Transparent };
            
            // 1. Sort
            _comboSort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppFonts.BodySmall, Width = 100, Margin = new Padding(0, 10, 10, 0) };
            _comboSort.Items.AddRange(new object[] { "↓ Tertinggi", "↑ Terendah" });
            _comboSort.SelectedIndex = 0;
            _comboSort.SelectedIndexChanged += async (s, e) => { _sortAscending = _comboSort.SelectedIndex == 1; await LoadData(); };
            var lblSort = new Label { Text = "Urutkan:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            // 2. Metric
            _comboMetric = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _comboMetric.Items.AddRange(new object[] { "Output Per Jam", "Efisiensi Mesin" });
            _comboMetric.SelectedIndex = 0;
            _comboMetric.SelectedIndexChanged += async (s, e) => await LoadData();
            var lblMetric = new Label { Text = "Metrik:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            // 3. Area
            _comboArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _comboArea.SelectedIndexChanged += async (s, e) => await LoadData();
            var lblArea = new Label { Text = "Area:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            // 4. Shift (BARU)
            _comboShift = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _comboShift.Items.AddRange(new object[] { "Waktu Aktual", "Shift Pagi", "Shift Malam" });
            _comboShift.SelectedIndex = 0;
            _comboShift.SelectedIndexChanged += async (s, e) => 
            {
                // Jika Auto, kalender dinonaktifkan
                _dtpDateFilter.Enabled = _comboShift.SelectedIndex != 0;
                await LoadData();
            };
            var lblShift = new Label { Text = "Shift:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            // 5. Date Picker (BARU)
            _dtpDateFilter = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _dtpDateFilter.Enabled = false; // Default off karena mode Auto
            _dtpDateFilter.ValueChanged += async (s, e) => await LoadData();
            var lblDate = new Label { Text = "Tanggal:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            // Karena RightToLeft, tambahkan dari yang paling kanan ke kiri
            flowRight.Controls.Add(_comboSort);
            flowRight.Controls.Add(lblSort);
            flowRight.Controls.Add(_comboMetric);
            flowRight.Controls.Add(lblMetric);
            flowRight.Controls.Add(_comboArea);
            flowRight.Controls.Add(lblArea);
            flowRight.Controls.Add(_comboShift);
            flowRight.Controls.Add(lblShift);
            flowRight.Controls.Add(_dtpDateFilter);
            flowRight.Controls.Add(lblDate);
            
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

        // [MODIFIKASI] Logika Cerdas Pencarian Titik Start Shift
        private DateTime GetShiftTimeRange(out DateTime shiftEnd, out bool isPastShift, out string shiftName)
        {
            DateTime now = DateTime.Now;
            DateTime selectedDate = _dtpDateFilter.Value.Date;
            bool isAuto = _comboShift.SelectedIndex == 0;
            bool isPagi = _comboShift.SelectedIndex == 1;

            DateTime shiftStart;

            if (isAuto)
            {
                // MODE REAL-TIME
                isPastShift = false;
                if (now.Hour >= 7 && now.Hour < 19) 
                {
                    shiftName = "Shift Pagi";
                    shiftStart = now.Date.AddHours(7);
                }
                else if (now.Hour >= 19) 
                {
                    shiftName = "Shift Malam";
                    shiftStart = now.Date.AddHours(19);
                }
                else 
                {
                    shiftName = "Shift Malam";
                    shiftStart = now.Date.AddDays(-1).AddHours(19); 
                }
            }
            else
            {
                // MODE MANUAL (Menjelajah Waktu)
                if (isPagi)
                {
                    shiftName = "Shift Pagi";
                    shiftStart = selectedDate.AddHours(7);
                }
                else
                {
                    shiftName = "Shift Malam";
                    shiftStart = selectedDate.AddHours(19);
                }
                
                // Cek apakah shift ini sudah selesai di masa lalu
                isPastShift = now >= shiftStart.AddHours(12);
            }

            // Durasi Shift selalu 12 Jam
            shiftEnd = shiftStart.AddHours(12);
            return shiftStart;
        }

        private async Task LoadData()
        {
            try
            {
                string selectedArea = _comboArea.SelectedItem?.ToString() ?? "Semua Area";
                int maxShiftHours = 12;

                // Hitung rentang waktu berdasarkan filter UI
                DateTime shiftEnd;
                bool isPastShift;
                string namaShift;
                DateTime shiftStart = GetShiftTimeRange(out shiftEnd, out isPastShift, out namaShift);

                string sql = @"
                    SELECT m.machine_id, 
                           COALESCE(t.type_name, 'UNK') AS type_name, 
                           COALESCE(a.area_name, 'UNK') AS area_name, 
                           m.machine_number,
                           -1 AS hour_index,
                           COALESCE((SELECT produced_pieces FROM machine_process_logs WHERE machine_id = m.machine_id AND created_at < @ShiftStart ORDER BY created_at DESC LIMIT 1), 0) AS max_pieces,
                           0 AS curr_auto, 0 AS curr_mon
                    FROM machines m
                    LEFT JOIN machine_types t ON m.type_id = t.type_id
                    LEFT JOIN machine_areas a ON m.area_id = a.area_id
                    WHERE (@Area = 'Semua Area' OR a.area_name = @Area)
                    
                    UNION ALL
                    
                    SELECT m.machine_id, 
                           COALESCE(t.type_name, 'UNK') AS type_name, 
                           COALESCE(a.area_name, 'UNK') AS area_name, 
                           m.machine_number,
                           TIMESTAMPDIFF(HOUR, @ShiftStart, p.created_at) AS hour_index,
                           MAX(p.produced_pieces) AS max_pieces,
                           MAX(p.auto_time) AS curr_auto,
                           MAX(p.monitor_time) AS curr_mon
                    FROM machines m
                    LEFT JOIN machine_types t ON m.type_id = t.type_id
                    LEFT JOIN machine_areas a ON m.area_id = a.area_id
                    JOIN machine_process_logs p ON m.machine_id = p.machine_id
                    WHERE (@Area = 'Semua Area' OR a.area_name = @Area)
                      AND p.created_at >= @ShiftStart 
                      AND p.created_at < @ShiftEnd  -- MENCEGAH DATA BOCOR DARI SHIFT BERIKUTNYA
                    GROUP BY m.machine_id, type_name, area_name, m.machine_number, hour_index
                    ORDER BY machine_id, hour_index;";
                
                IEnumerable<dynamic> rows;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    rows = await conn.QueryAsync(sql, new { Area = selectedArea, ShiftStart = shiftStart, ShiftEnd = shiftEnd });
                }

                var rawData = new Dictionary<int, long[]>();
                var machines = new Dictionary<int, MachineData>();

                foreach (var row in rows)
                {
                    int mId = (int)row.machine_id;
                    if (!machines.ContainsKey(mId))
                    {
                        machines[mId] = new MachineData { MachineName = $"{row.type_name}.{row.area_name}-{row.machine_number}" };
                        
                        rawData[mId] = new long[maxShiftHours + 1]; 
                        for(int i=0; i <= maxShiftHours; i++) rawData[mId][i] = -1; 
                    }

                    int hIndex = (int)row.hour_index;
                    if (hIndex >= -1 && hIndex < maxShiftHours) 
                    {
                        rawData[mId][hIndex + 1] = (long)row.max_pieces;
                        
                        if (hIndex >= 0) 
                        {
                            machines[mId].AutoTime = Math.Max(machines[mId].AutoTime, (double)(row.curr_auto ?? 0));
                            machines[mId].MonitorTime = Math.Max(machines[mId].MonitorTime, (double)(row.curr_mon ?? 0));
                        }
                    }
                }

                int currentHourCount;
                if (isPastShift)
                {
                    // Jika data hari kemarin, kunci grafik full 12 Jam
                    currentHourCount = maxShiftHours;
                }
                else
                {
                    // Jika real-time, grafik berjalan sesuai umur shift
                    currentHourCount = (int)(DateTime.Now - shiftStart).TotalHours + 1;
                    if (currentHourCount > maxShiftHours) currentHourCount = maxShiftHours;
                    if (currentHourCount < 1) currentHourCount = 1;
                }

                foreach (var kvp in machines)
                {
                    int mId = kvp.Key;
                    var machine = kvp.Value;
                    var maxes = rawData[mId];

                    long lastKnown = maxes[0] != -1 ? maxes[0] : 0;
                    for (int i = 0; i <= maxShiftHours; i++)
                    {
                        if (maxes[i] == -1) maxes[i] = lastKnown;
                        else lastKnown = maxes[i];
                    }

                    long totalPiecesShiftIni = 0;
                    int firstActiveHour = -1; 

                    for (int i = 1; i <= maxShiftHours; i++)
                    {
                        long diff = maxes[i] - maxes[i - 1];
                        if (diff < 0) diff = maxes[i]; 
                        
                        if (i <= currentHourCount)
                        {
                            machine.HourlyPieces[i - 1] = diff;
                            totalPiecesShiftIni += diff; 
                            if (diff > 0 && firstActiveHour == -1) 
                            {
                                firstActiveHour = i;
                            }
                        }
                        else
                        {
                            machine.HourlyPieces[i - 1] = 0; 
                        }
                    }

                    machine.TotalPieces = totalPiecesShiftIni;
                    
                    int pembagi = 1;
                    if (firstActiveHour != -1)
                    {
                        pembagi = currentHourCount - firstActiveHour + 1;
                    }
                    else
                    {
                        pembagi = currentHourCount; 
                    }
                    
                    machine.AveragePerHour = (double)totalPiecesShiftIni / pembagi;
                }

                string selectedMetric = _comboMetric.SelectedItem?.ToString();
                var machineList = machines.Values.ToList();

                if (selectedMetric.Contains("Efisiensi"))
                {
                    if (_sortAscending)
                        machineList = machineList.OrderBy(x => x.Efficiency).ToList();
                    else
                        machineList = machineList.OrderByDescending(x => x.Efficiency).ToList();
                }
                else
                {
                    if (_sortAscending)
                        machineList = machineList.OrderBy(x => x.TotalPieces).ToList();
                    else
                        machineList = machineList.OrderByDescending(x => x.TotalPieces).ToList();
                }

                UpdateChart(machineList, selectedMetric, currentHourCount);
                
                string stateText = isPastShift ? "Selesai" : $"Berjalan: Jam ke-{currentHourCount}";
                _lblStatus.Text = $"Update: {DateTime.Now:HH:mm:ss} | {namaShift} ({shiftStart:dd MMM yyyy}) | {stateText}";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private void UpdateChart(List<MachineData> data, string mode, int currentHourCount)
        {
            _chart.Series.Clear();
            var area = _chart.ChartAreas[0];

            area.AxisY.Maximum = Double.NaN;
            area.AxisY.Minimum = 0;
            area.AxisY.Title = "";
            
            if (data.Count > 10)
            {
                area.AxisX.ScaleView.Zoomable = true;
                area.AxisX.ScaleView.Size = 10;
            }
            else
            {
                area.AxisX.ScaleView.ZoomReset();
            }

            area.RecalculateAxesScale();

            if (mode.Contains("Output"))
            {
                area.AxisY.Title = "Output (Pcs)";

                Color[] hourColors = new Color[] {
                    Color.FromArgb(52, 152, 219), Color.FromArgb(41, 128, 185), // Jam 1-2
                    Color.FromArgb(46, 204, 113), Color.FromArgb(39, 174, 96),  // Jam 3-4
                    Color.FromArgb(241, 196, 15), Color.FromArgb(230, 126, 34), // Jam 5-6
                    Color.FromArgb(231, 76, 60),  Color.FromArgb(192, 57, 43),  // Jam 7-8
                    Color.FromArgb(155, 89, 182), Color.FromArgb(142, 68, 173), // Jam 9-10
                    Color.FromArgb(26, 188, 156), Color.FromArgb(22, 160, 133), // Jam 11-12
                    Color.FromArgb(52, 73, 94),   Color.FromArgb(44, 62, 80)    // Jam 13-14
                };

                for (int i = 0; i < currentHourCount; i++)
                {
                    var s = new Series($"Jam {i + 1}") 
                    { 
                        ChartType = SeriesChartType.StackedColumn, 
                        Color = hourColors[i],
                        IsValueShownAsLabel = false 
                    };
                    s["PixelPointWidth"] = "80"; 
                    _chart.Series.Add(s);
                }

                var sAvg = new Series("Rata-rata/Jam") 
                { 
                    ChartType = SeriesChartType.Point, 
                    MarkerStyle = MarkerStyle.None, 
                    Color = Color.Transparent,
                    IsValueShownAsLabel = true,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold)
                };
                sAvg["LabelStyle"] = "Top";
                _chart.Series.Add(sAvg);

                foreach (var item in data)
                {
                    for (int i = 0; i < currentHourCount; i++)
                    {
                        int pIdx = _chart.Series[i].Points.AddXY(item.MachineName, item.HourlyPieces[i]);
                        if (item.HourlyPieces[i] > 0)
                            _chart.Series[i].Points[pIdx].Label = item.HourlyPieces[i].ToString("N0");
                    }
                    
                    double labelYPos = item.TotalPieces > 0 ? item.TotalPieces + (item.TotalPieces * 0.05) : 0;
                    int pAvg = sAvg.Points.AddXY(item.MachineName, labelYPos);
                    
                    if (item.TotalPieces > 0)
                    {
                        sAvg.Points[pAvg].Label = $"Rata-rata: {item.AveragePerHour:N0} / jam\nTotal: {item.TotalPieces:N0}";
                    }
                    else
                    {
                        sAvg.Points[pAvg].Label = " "; 
                    }
                }
            }
            else 
            {
                area.AxisY.Title = "Waktu (Menit)";

                var sAuto = new Series("Auto Time") { ChartType = SeriesChartType.StackedColumn, Color = AppColors.Success, IsValueShownAsLabel = true };
                sAuto["PixelPointWidth"] = "80";
                
                var sLoss = new Series("Loss Time") { ChartType = SeriesChartType.StackedColumn, Color = Color.FromArgb(230, 126, 34), IsValueShownAsLabel = true };
                sLoss["PixelPointWidth"] = "80";
                
                var sEffLabel = new Series("Eff %") { ChartType = SeriesChartType.Point, Color = Color.Transparent, IsValueShownAsLabel = true };
                sEffLabel["LabelStyle"] = "Top"; 
                sEffLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold); 

                foreach (var item in data)
                {
                    double autoValMinutes = item.AutoTime / 60.0; 
                    double monValMinutes = item.MonitorTime / 60.0;
                    double lossValMinutes = (monValMinutes > autoValMinutes) ? (monValMinutes - autoValMinutes) : 0;
                    
                    int p1 = sAuto.Points.AddXY(item.MachineName, autoValMinutes);
                    sAuto.Points[p1].Label = autoValMinutes > 0 ? $"{autoValMinutes:N0}m" : " ";

                    int p2 = sLoss.Points.AddXY(item.MachineName, lossValMinutes);
                    sLoss.Points[p2].Label = lossValMinutes > 0 ? $"{lossValMinutes:N0}m" : " "; 

                    double labelY = monValMinutes > 0 ? monValMinutes + (monValMinutes * 0.05) : 0; 
                    int p3 = sEffLabel.Points.AddXY(item.MachineName, labelY);
                    
                    if (monValMinutes > 0)
                    {
                        sEffLabel.Points[p3].Label = $"{item.Efficiency:F1}%";
                    }
                    else
                    {
                        sEffLabel.Points[p3].Label = " ";
                    }
                    
                    sEffLabel.Points[p3].MarkerStyle = MarkerStyle.None;
                }
                _chart.Series.Add(sAuto);
                _chart.Series.Add(sLoss);
                _chart.Series.Add(sEffLabel);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (components != null) components.Dispose(); if (_timer != null) { _timer.Stop(); _timer.Dispose(); } }
            base.Dispose(disposing);
        }
    }
}