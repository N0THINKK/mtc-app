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
        // Refresh otomatis setiap 30 Menit (30 * 60 * 1000 ms = 1800000)
        private const int REFRESH_RATE_MS = 1800000;

        private Button _btnRefresh;
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
        private bool _isLoading = false;

        // Array mapping actual active hours (index) to effective hours (value)
        // Index 0 is 1.0 to prevent division by zero for the 0th hour.
        // Index 1-9: Regular shift (Total max = 8.00). 8.0 spread across 9 hours.
        // Index 10-12: Overtime (Total max = 8.00 + 1.75 = 9.75). 1.75 effective hours spread across 3 overtime hours.
        private readonly double[] _effectiveHours = new double[]
        {
            1.00, // Index 0 (No full running hours yet)
            8.0 / 9.0 * 1, // Hour 1
            8.0 / 9.0 * 2, // Hour 2
            8.0 / 9.0 * 3, // Hour 3
            8.0 / 9.0 * 4, // Hour 4
            8.0 / 9.0 * 5, // Hour 5
            8.0 / 9.0 * 6, // Hour 6
            8.0 / 9.0 * 7, // Hour 7
            8.0 / 9.0 * 8, // Hour 8
            8.00, // Hour 9  (End of regular shift)
            8.00 + (1.75 / 3.0 * 1), // Hour 10 (Overtime 1)
            8.00 + (1.75 / 3.0 * 2), // Hour 11 (Overtime 2)
            9.75  // Hour 12 (Overtime 3)
        };

        private System.ComponentModel.IContainer components = null;

        private class MachineData
        {
            public string MachineName { get; set; }
            public string MachineNum { get; set; }
            public int TypeId { get; set; }
            public int AreaId { get; set; }
            public long[] HourlyPieces { get; set; } = new long[14];
            public long TotalPieces { get; set; }
            public double AveragePerHour { get; set; }
            public int TargetPerHour { get; set; }

            public double AutoTime { get; set; }
            public double MonitorTime { get; set; }
            public double PlannedStopMinutes { get; set; }
            public double SuddenStopMinutes { get; set; }
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
                using (var conn = DatabaseHelper.GetConnection())
                {
                    string sql = @"
                        SELECT DISTINCT a.area_name 
                        FROM machine_areas a 
                        JOIN machines m ON a.area_id = m.area_id 
                        ORDER BY a.area_name";
                    var areas = await conn.QueryAsync<string>(sql);
                    foreach (var area in areas) _comboArea.Items.Add(area);
                }

                if (_comboArea.Items.Count > 1)
                {
                    _comboArea.SelectedIndex = 1; // Default to first alphabet
                }
                else if (_comboArea.Items.Count > 0)
                {
                    _comboArea.SelectedIndex = 0;
                }
            }
            catch { }
        }

        public void ResetAutoSwitch()
        {
            if (_comboMetric.Items.Count > 0) _comboMetric.SelectedIndex = 0;
            if (_comboArea.Items.Count > 1) _comboArea.SelectedIndex = 1;
            else if (_comboArea.Items.Count > 0) _comboArea.SelectedIndex = 0;
        }

        public bool AdvanceAutoSwitch()
        {
            int areaCount = _comboArea.Items.Count;
            int metricCount = _comboMetric.Items.Count;

            if (areaCount <= 1 || metricCount <= 0) return true; 

            int currentAreaIndex = _comboArea.SelectedIndex;
            int currentMetricIndex = _comboMetric.SelectedIndex;

            currentAreaIndex++;

            if (currentAreaIndex >= areaCount)
            {
                // Reached end of areas, go to next metric and start from first area again
                currentAreaIndex = 1; 
                currentMetricIndex++;

                if (currentMetricIndex >= metricCount)
                {
                    // Reached end of all metrics
                    return true; // Output tab is done, parent should switch to next tab
                }
            }

            _comboMetric.SelectedIndex = currentMetricIndex;
            _comboArea.SelectedIndex = currentAreaIndex;

            return false;
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.CardBackground;
            this.Padding = new Padding(AppDimens.MarginLarge);

            var pnlHeader = BuildHeaderPanel();

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.Transparent };
            _btnRefresh = new Button 
            { 
                Text = "Refresh", 
                Font = AppFonts.BodySmall, 
                Width = 100, 
                Height = 35, 
                Margin = new Padding(0), 
                BackColor = Color.White, 
                ForeColor = AppColors.TextPrimary, 
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnRefresh.FlatAppearance.BorderColor = Color.LightGray;
            _btnRefresh.Click += async (s, e) => await HandleManualRefresh();
            pnlBottom.Controls.Add(_btnRefresh);

            _pnlChartContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, MinimumSize = new Size(10, 10) };

            _chart = new Chart();
            
            // Fix MS Chart crash on Height = 0 during layout
            _chart.MinimumSize = new Size(10, 10);

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
            _chart.PostPaint += Chart_PostPaint;

            var legend = new Legend("MainLegend");
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Center;
            _chart.Legends.Add(legend);

            _pnlChartContainer.Controls.Add(_chart);
            this.Controls.Add(_pnlChartContainer);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);

            var lblTitle = new Label { Text = "Monitoring Output Efisiensi", Font = AppFonts.PageTitle, ForeColor = AppColors.TextPrimary, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top, Height = 40 };
            this.Controls.Add(lblTitle);
        }

        private Panel BuildHeaderPanel()
        {
            var pnlHeader = new Panel { Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 60) };
            var headerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            var flowLeft = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill, BackColor = Color.Transparent };
            
            _lblStatus = new Label { Text = "Memuat data...", Font = AppFonts.BodySmall, ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 13, 0, 0) };
            
            flowLeft.Controls.Add(_lblStatus);
            headerLayout.Controls.Add(flowLeft, 0, 0);

            var flowRight = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, WrapContents = true, Dock = DockStyle.Fill, BackColor = Color.Transparent };

            // 1. Sort
            _comboSort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppFonts.BodySmall, Width = 110, Margin = new Padding(0, 10, 10, 0) };
            _comboSort.Items.AddRange(new object[] { "↓ Tertinggi", "↑ Terendah", "Nomor Mesin" });
            _comboSort.SelectedIndex = 2; // Default to "Nomor Mesin" for randomized look
            _comboSort.SelectedIndexChanged += async (s, e) => { await LoadData(); };
            var lblSort = new Label { Text = "Urutkan:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            // 2. Metric
            _comboMetric = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _comboMetric.Items.AddRange(new object[] { "Output Per Jam", "Total", "Efisiensi Mesin" });
            _comboMetric.SelectedIndex = 0;
            _comboMetric.SelectedIndexChanged += async (s, e) => await LoadData();
            var lblMetric = new Label { Text = "Metrik:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            // 3. Area
            _comboArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _comboArea.SelectedIndexChanged += async (s, e) => await LoadData();
            var lblArea = new Label { Text = "Area:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            // 4. Shift 
            _comboShift = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _comboShift.Items.AddRange(new object[] { "Waktu Aktual", "Shift Pagi", "Shift Malam" });
            _comboShift.SelectedIndex = 0;
            _comboShift.SelectedIndexChanged += async (s, e) =>
            {
                _dtpDateFilter.Enabled = _comboShift.SelectedIndex != 0;
                await LoadData();
            };
            var lblShift = new Label { Text = "Shift:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            // 5. Date Picker
            _dtpDateFilter = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _dtpDateFilter.Enabled = false;
            _dtpDateFilter.ValueChanged += async (s, e) => await LoadData();
            var lblDate = new Label { Text = "Tanggal:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

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

        private void Chart_PostPaint(object sender, ChartPaintEventArgs e)
        {
            if (sender is Chart chart && e.ChartElement is ChartArea area && area.Name == "MainArea")
            {
                var cg = e.ChartGraphics;
                var g = cg.Graphics;
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };

                foreach (var series in chart.Series)
                {
                    if (series.Name == "Avg Output/Jam" || series.Name == "Total" || series.Name == "Eff %")
                    {
                        for (int i = 0; i < series.Points.Count; i++)
                        {
                            var p = series.Points[i];
                            string labelText = p.Tag as string;
                            if (string.IsNullOrWhiteSpace(labelText)) continue;

                            double xVal = p.XValue != 0 ? p.XValue : i + 1;
                            
                            if (!double.IsNaN(area.AxisX.ScaleView.ViewMinimum) && !double.IsNaN(area.AxisX.ScaleView.ViewMaximum))
                            {
                                if (xVal < area.AxisX.ScaleView.ViewMinimum || xVal > area.AxisX.ScaleView.ViewMaximum)
                                    continue;
                            }

                            double yVal = p.YValues[0];
                            try 
                            {
                                double xPosRel = area.AxisX.ValueToPosition(xVal);
                                double yPosRel = area.AxisY.ValueToPosition(yVal);
                                PointF pixelPos = cg.GetAbsolutePoint(new PointF((float)xPosRel, (float)yPosRel));

                                var state = g.Save();
                                g.TranslateTransform(pixelPos.X, pixelPos.Y - 5);
                                g.RotateTransform(-90);
                                
                                using (var brush = new SolidBrush(Color.FromArgb(64, 64, 64)))
                                {
                                    g.DrawString(labelText, series.Font, brush, new PointF(0, 0), sf);
                                }
                                g.Restore(state);
                            } 
                            catch { }
                        }
                    }
                }
            }
        }

        private void SetupTimer()
        {
            _timer = new Timer();
            _timer.Interval = REFRESH_RATE_MS;
            _timer.Tick += async (s, e) => await LoadData();
        }

        private async Task HandleManualRefresh()
        {
            if (_isLoading) return;

            // Reset timer ototmatis agar jeda 30 menit menghitung dari SEKARANG
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Start();
            }

            _btnRefresh.Enabled = false;
            
            await LoadData();

            // Proteksi Anti-Spam (Cooldown 2 menit)
            int cooldownSeconds = 120;
            while (cooldownSeconds > 0 && !this.IsDisposed)
            {
                _btnRefresh.Text = $"Tunggu ({cooldownSeconds}s)";
                await Task.Delay(1000);
                cooldownSeconds--;
            }

            if (!this.IsDisposed)
            {
                _btnRefresh.Enabled = true;
                _btnRefresh.Text = "Refresh";
            }
        }

        public void StartMonitoring() { _ = LoadData(); _timer.Start(); }
        public void StopMonitoring() { _timer.Stop(); }
        public void SetMetric(int index) { if (index >= 0 && index < _comboMetric.Items.Count) _comboMetric.SelectedIndex = index; }

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

                isPastShift = now >= shiftStart.AddHours(12);
            }

            shiftEnd = shiftStart.AddHours(12);
            return shiftStart;
        }

        private async Task LoadData()
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                string selectedArea = _comboArea.SelectedItem?.ToString() ?? "Semua Area";
                int maxShiftHours = 12;

                DateTime shiftEnd;
                bool isPastShift;
                string namaShift;
                DateTime shiftStart = GetShiftTimeRange(out shiftEnd, out isPastShift, out namaShift);

                // ══════════════════════════════════════════════════════════
                // SINGLE CONNECTION: All queries share one connection
                // ══════════════════════════════════════════════════════════
                int maxBreakMinutes = 0;
                var machines = new Dictionary<int, MachineData>();
                var hourFirst = new Dictionary<int, long[]>();
                var hourLast = new Dictionary<int, long[]>();
                var hourMax = new Dictionary<int, long[]>();

                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // --- Query 1: Break minutes (tiny lookup) ---
                    try
                    {
                        string dbShift = namaShift == "Shift Pagi" ? "Shift 1" : "Shift 2";
                        int dayId = (int)shiftStart.DayOfWeek;
                        if (dayId == 0) dayId = 7;

                        var b = await conn.QueryFirstOrDefaultAsync(
                            "SELECT non_ot_minutes, ot_minutes FROM shift_breaks WHERE shift_name = @Shift AND day_id = @Day",
                            new { Shift = dbShift, Day = dayId });

                        if (b != null)
                        {
                            int currentHourCountTemp = isPastShift ? 12 : Math.Max(1, (int)(DateTime.Now - shiftStart).TotalHours + 1);
                            maxBreakMinutes = currentHourCountTemp > 9
                                ? ((int)b.non_ot_minutes + (int)b.ot_minutes)
                                : (int)b.non_ot_minutes;
                        }
                    }
                    catch { }

                    // --- Query 2: Machine list (lightweight) ---
                    string sqlMachines = @"
                        SELECT m.machine_id, m.type_id, m.area_id,
                               COALESCE(t.type_name, 'UNK') AS type_name,
                               COALESCE(a.area_name, 'UNK') AS area_name,
                               m.machine_number
                        FROM machines m
                        LEFT JOIN machine_types t ON m.type_id = t.type_id
                        LEFT JOIN machine_areas a ON m.area_id = a.area_id
                        WHERE (@Area = 'Semua Area' OR a.area_name = @Area)
                        ORDER BY m.machine_id";

                    var machineRows = await conn.QueryAsync(sqlMachines, new { Area = selectedArea });

                    foreach (var row in machineRows)
                    {
                        int mId = (int)row.machine_id;
                        machines[mId] = new MachineData
                        {
                            MachineName = $"{row.type_name}.{row.area_name}-{row.machine_number}",
                            MachineNum = row.machine_number,
                            TypeId = (int)(row.type_id ?? 0),
                            AreaId = (int)(row.area_id ?? 0)
                        };
                        hourFirst[mId] = new long[maxShiftHours];
                        hourLast[mId] = new long[maxShiftHours];
                        hourMax[mId] = new long[maxShiftHours];
                        for (int i = 0; i < maxShiftHours; i++)
                        {
                            hourFirst[mId][i] = -1;
                            hourLast[mId][i] = -1;
                            hourMax[mId][i] = -1;
                        }
                    }

                    // --- Query 3: FLAT raw logs — NO self-join, NO UNION ALL ---
                    // The DB only scans machine_process_logs ONCE with index on (created_at, machine_id).
                    // C# will compute first/last/max per (machine, hour) from ordered rows.
                    string sqlLogs = @"
                        SELECT machine_id,
                               TIMESTAMPDIFF(HOUR, @ShiftStart, created_at) AS hour_index,
                               produced_pieces,
                               auto_time,
                               monitor_time
                        FROM machine_process_logs
                        WHERE created_at >= @ShiftStart AND created_at < @ShiftEnd
                          AND produced_pieces > 0
                        ORDER BY machine_id, created_at";

                    var logRows = await conn.QueryAsync(sqlLogs, 
                        new { ShiftStart = shiftStart, ShiftEnd = shiftEnd }, 
                        commandTimeout: 30);

                    // --- C# ALGORITHM: Compute first/last/max per (machine, hour) ---
                    foreach (var row in logRows)
                    {
                        int mId = (int)row.machine_id;
                        if (!machines.ContainsKey(mId)) continue; // Skip if not in filtered area

                        int hIndex = (int)(row.hour_index ?? 0);
                        if (hIndex < 0 || hIndex >= maxShiftHours) continue;

                        long pieces = (long)(row.produced_pieces ?? 0);
                        double autoTime = (double)(row.auto_time ?? 0);
                        double monTime = (double)(row.monitor_time ?? 0);

                        // First piece for this (machine, hour) — set once
                        if (hourFirst[mId][hIndex] == -1)
                            hourFirst[mId][hIndex] = pieces;

                        // Last piece — always overwrite (rows are ordered by created_at)
                        hourLast[mId][hIndex] = pieces;

                        // Max piece — track running max
                        if (pieces > hourMax[mId][hIndex])
                            hourMax[mId][hIndex] = pieces;

                        // Auto/Monitor time — take the max across all rows
                        machines[mId].AutoTime = Math.Max(machines[mId].AutoTime, autoTime);
                        machines[mId].MonitorTime = Math.Max(machines[mId].MonitorTime, monTime);
                    }

                    // --- Query 4: Targets (tiny table) ---
                    try
                    {
                        var targets = await conn.QueryAsync(
                            "SELECT machine_id, target_per_hour FROM machine_output_targets");

                        foreach (var row in targets)
                        {
                            int targetMachineId = Convert.ToInt32(row.machine_id);
                            if (machines.TryGetValue(targetMachineId, out var targetMachine))
                            {
                                targetMachine.TargetPerHour = Convert.ToInt32(row.target_per_hour);
                            }
                        }
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine("Error mapping targets: " + ex.Message);
                    }

                    // --- Query 5: Downtime categories (Planned / Sudden) ---
                    try
                    {
                        var psData = await conn.QueryAsync(@"
                            SELECT moa.machine_id, 
                                   SUM(CASE WHEN it.category IN ('Planned Stop', 'Berhenti Terencana') THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS PlannedMin,
                                   SUM(CASE WHEN it.category IN ('Sudden Stop', 'Berhenti Tiba Tiba') THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS SuddenMin
                            FROM machine_operator_activities moa
                            LEFT JOIN activity_types it ON moa.activity_id = it.id
                            WHERE moa.start_time >= @ShiftStart AND moa.start_time < @ShiftEnd
                            GROUP BY moa.machine_id", new { ShiftStart = shiftStart, ShiftEnd = shiftEnd });

                        foreach (var row in psData)
                        {
                            int downtimeMachineId = Convert.ToInt32(row.machine_id);
                            if (machines.TryGetValue(downtimeMachineId, out var downtimeMachine))
                            {
                                downtimeMachine.PlannedStopMinutes = (double)(row.PlannedMin ?? 0);
                                downtimeMachine.SuddenStopMinutes = (double)(row.SuddenMin ?? 0);
                            }
                        }
                    }
                    catch { /* Quiet fallback */ }

                } // End single connection

                // ══════════════════════════════════════════════════════════
                // C# POST-PROCESSING (unchanged logic)
                // ══════════════════════════════════════════════════════════
                int currentHourCount;
                if (isPastShift)
                {
                    currentHourCount = maxShiftHours;
                }
                else
                {
                    currentHourCount = (int)(DateTime.Now - shiftStart).TotalHours + 1;
                    if (currentHourCount > maxShiftHours) currentHourCount = maxShiftHours;
                    if (currentHourCount < 1) currentHourCount = 1;
                }

                foreach (var kvp in machines)
                {
                    int mId = kvp.Key;
                    var machine = kvp.Value;

                    long totalPiecesShiftIni = 0;
                    int firstActiveHour = -1;
                    int lastActiveHour = -1;

                    for (int i = 0; i < maxShiftHours; i++)
                    {
                        if (hourFirst[mId][i] == -1 || i >= currentHourCount)
                        {
                            machine.HourlyPieces[i] = 0;
                            continue;
                        }

                        long first = hourFirst[mId][i];
                        long last = hourLast[mId][i];
                        long max = hourMax[mId][i];
                        long production = 0;

                        if (last >= first)
                        {
                            // Normal: no reset in this hour
                            production = last - first;
                        }
                        else
                        {
                            // Counter reset happened in this hour (last < first)
                            // Pre-reset production: max - first
                            // Post-reset production: last (counter restarted from 0)
                            production = (max - first) + last;
                        }

                        machine.HourlyPieces[i] = production;
                        totalPiecesShiftIni += production;

                        if (production > 0)
                        {
                            if (firstActiveHour == -1) firstActiveHour = i + 1; // 1-based
                            lastActiveHour = i + 1;
                        }
                    }

                    machine.TotalPieces = totalPiecesShiftIni;

                    // SMART OVERTIME DETECTION:
                    // If the machine produced output in Hour 10 or later, they are validated as working overtime.
                    bool isOvertime = lastActiveHour >= 10;

                    // If overtime is validated, the divisor grows up to the current running hour.
                    // If not overtime, the divisor is strictly capped at hour 9 (end of regular shift).
                    int activeEndHour = isOvertime ? currentHourCount : Math.Min(currentHourCount, 9);

                    int divisorIndex = 1;
                    if (firstActiveHour != -1)
                    {
                        // Calculate active hours based on when they started producing
                        divisorIndex = activeEndHour - firstActiveHour + 1;
                    }
                    else
                    {
                        divisorIndex = activeEndHour;
                    }

                    // Clamp the divisor index between 0 and 12 to safely access the array
                    if (divisorIndex < 0) divisorIndex = 0;
                    if (divisorIndex > 12) divisorIndex = 12;

                    double effectiveDivisor = _effectiveHours[divisorIndex];
                    machine.AveragePerHour = (double)totalPiecesShiftIni / effectiveDivisor;
                }

                string selectedMetric = _comboMetric.SelectedItem?.ToString();
                string selectedSort = _comboSort.SelectedItem?.ToString();
                var machineList = machines.Values.ToList();

                if (selectedSort == "Nomor Mesin")
                {
                    machineList = machineList.OrderBy(x => x.MachineName).ToList();
                }
                else
                {
                    bool sortAscending = selectedSort == "↑ Terendah";

                    if (selectedMetric.Contains("Efisiensi"))
                    {
                        if (sortAscending)
                            machineList = machineList.OrderBy(x => x.Efficiency).ToList();
                        else
                            machineList = machineList.OrderByDescending(x => x.Efficiency).ToList();
                    }
                    else
                    {
                        if (sortAscending)
                            machineList = machineList.OrderBy(x => x.TotalPieces).ToList();
                        else
                            machineList = machineList.OrderByDescending(x => x.TotalPieces).ToList();
                    }
                }

                UpdateChart(machineList, selectedMetric, currentHourCount, maxBreakMinutes);

                string stateText = isPastShift ? "Selesai" : $"Berjalan: Jam ke-{currentHourCount}";
                _lblStatus.Text = $"Update: {DateTime.Now:HH:mm:ss} | {namaShift} ({shiftStart:dd MMM yyyy}) | {stateText}";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Error: {ex.Message}";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateChart(List<MachineData> data, string mode, int currentHourCount, int maxBreakMinutes)
        {
            _chart.Series.Clear();
            var area = _chart.ChartAreas[0];

            area.AxisY.Minimum = 0;
            area.AxisY.Title = "";
            area.AxisY.LabelStyle.Format = "N0";

            int maxVisible = 40; // Fit more bars in one screen for slimmer look
            if (data.Count > maxVisible)
            {
                area.AxisX.ScaleView.Zoomable = true;
                area.AxisX.ScaleView.Size = maxVisible;
            }
            else
            {
                area.AxisX.ScaleView.ZoomReset();
            }

            area.AxisX.LabelStyle.Angle = -90;
            area.AxisX.IsLabelAutoFit = false;

            area.RecalculateAxesScale();

            _chart.Legends.Clear();

            if (mode == "Output Per Jam")
            {
                // --- MODE 1: Single bar (avg/hour) + target dashed line ---
                double maxVal = data.Count > 0 ? data.Max(x => x.AveragePerHour) : 0;
                double maxTarget = data.Count > 0 ? data.Max(x => x.TargetPerHour) : 0;
                area.AxisY.Maximum = Math.Max(maxVal, maxTarget) > 0 ? Math.Ceiling(Math.Max(maxVal, maxTarget) * 1.2) : 10;

                area.AxisY.Title = "Output Per Jam (Pcs)";

                var sBar = new Series("Avg Output/Jam")
                {
                    ChartType = SeriesChartType.Column,
                    Color = Color.FromArgb(174, 214, 241), // Pastel Blue
                    IsValueShownAsLabel = false,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                sBar["PointWidth"] = "0.7";

                var sTarget = new Series("Target")
                {
                    ChartType = SeriesChartType.Line,
                    Color = Color.Red,
                    BorderWidth = 2,
                    BorderDashStyle = ChartDashStyle.Dash,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 6,
                    IsValueShownAsLabel = false
                };

                bool hasAnyTarget = data.Any(d => d.TargetPerHour > 0);

                foreach (var item in data)
                {
                    int idx = sBar.Points.AddXY(item.MachineName, item.AveragePerHour);
                    if (item.AveragePerHour > 0)
                        sBar.Points[idx].Tag = $"{item.AveragePerHour:N0}";

                    if (hasAnyTarget)
                        sTarget.Points.AddXY(item.MachineName, item.TargetPerHour);
                }

                _chart.Series.Add(sBar);
                if (hasAnyTarget)
                    _chart.Series.Add(sTarget);
            }
            else if (mode == "Total")
            {
                // --- MODE 2: Stacked hourly bars (no avg/tot label) ---
                double maxVal = data.Count > 0 ? data.Max(x => x.TotalPieces) : 0;
                area.AxisY.Maximum = maxVal > 0 ? Math.Ceiling(maxVal * 1.2) : 10;

                area.AxisY.Title = "Total Output (Pcs)";

                Color[] hourColors = new Color[] {
                Color.FromArgb(255, 179, 186), // Pastel Pink
                Color.FromArgb(255, 223, 186), // Pastel Orange
                Color.FromArgb(255, 255, 186), // Pastel Yellow
                Color.FromArgb(186, 255, 201), // Pastel Green
                Color.FromArgb(186, 225, 255), // Pastel Cyan/Blue
                Color.FromArgb(226, 186, 255), // Pastel Purple
                Color.FromArgb(255, 186, 210), // Pastel Rose
                Color.FromArgb(219, 255, 186), // Pastel Lime
                Color.FromArgb(255, 201, 186), // Pastel Deep Orange
                Color.FromArgb(186, 255, 240), // Pastel Teal
                Color.FromArgb(240, 186, 255), // Pastel Light Purple
                Color.FromArgb(237, 255, 186), // Pastel Yellow Green
                Color.FromArgb(255, 186, 186), // Pastel Light Red
                Color.FromArgb(174, 214, 241)  // Pastel Blue
            };

                for (int i = 0; i < currentHourCount; i++)
                {
                    var s = new Series($"Jam {i + 1}")
                    {
                        ChartType = SeriesChartType.StackedColumn,
                        Color = hourColors[i % hourColors.Length], // Cegah IndexOutOfRange just in case
                        IsValueShownAsLabel = false,
                        Font = new Font("Segoe UI", 8F, FontStyle.Regular)
                    };
                    s["PointWidth"] = "0.7";
                    _chart.Series.Add(s);
                }

                // Total label on top (no avg) using Tag for PostPaint drawing
                var sTotLabel = new Series("Total")
                {
                    ChartType = SeriesChartType.Point,
                    MarkerStyle = MarkerStyle.None,
                    Color = Color.Transparent,
                    IsValueShownAsLabel = false,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                _chart.Series.Add(sTotLabel);

                // --- Calculate Smart Label Threshold ---
                // Minimal butuh sekitar 16 pixel secara vertikal agar text font 8pt tidak bertumpuk.
                int chartDrawHeight = Math.Max(200, _chart.Height - 80); 
                double labelYThreshold = (area.AxisY.Maximum / chartDrawHeight) * 16.0;

                foreach (var item in data)
                {
                    for (int i = 0; i < currentHourCount; i++)
                    {
                        int pIdx = _chart.Series[i].Points.AddXY(item.MachineName, item.HourlyPieces[i]);
                        
                        // Smart Clipping: Sembunyikan label dalam bar jika ukuran fisiknya kurang dari 16px di layar
                        if (item.HourlyPieces[i] > 0 && item.HourlyPieces[i] >= labelYThreshold)
                        {
                            _chart.Series[i].Points[pIdx].Label = item.HourlyPieces[i].ToString("N0");
                        }
                    }

                    // MaxVal is used so that the label offsets consistently across the chart
                    double maxValRef = data.Count > 0 ? data.Max(x => x.TotalPieces) : 0;
                    double labelYPos = item.TotalPieces > 0 ? item.TotalPieces + (maxValRef * 0.02) : 0;
                    int pTot = sTotLabel.Points.AddXY(item.MachineName, labelYPos);
                    if (item.TotalPieces > 0)
                        sTotLabel.Points[pTot].Tag = $"{item.TotalPieces:N0}";
                }
            }
            else // Efisiensi Mesin
            {
                double maxVal = data.Count > 0 ? data.Max(x => x.MonitorTime / 60.0) : 0;
                area.AxisY.Maximum = maxVal > 0 ? Math.Ceiling(maxVal * 1.2) : 10;

                area.AxisY.Title = "Waktu (Menit)";

                var legend = new Legend("LegendEfisiensi")
                {
                    Docking = Docking.Top,
                    Alignment = StringAlignment.Center,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                _chart.Legends.Add(legend);

                var sAuto = new Series("Run") { ChartType = SeriesChartType.StackedColumn, Color = Color.FromArgb(171, 235, 198), IsValueShownAsLabel = true }; // Pastel Green
                sAuto["PointWidth"] = "0.7";

                var sBreak = new Series("Break") { ChartType = SeriesChartType.StackedColumn, Color = Color.FromArgb(224, 224, 224), IsValueShownAsLabel = true }; // Pastel Gray
                sBreak["PointWidth"] = "0.7";

                var sPlanned = new Series("Downtime") { ChartType = SeriesChartType.StackedColumn, Color = Color.FromArgb(174, 214, 241), IsValueShownAsLabel = true }; // Pastel Blue
                sPlanned["PointWidth"] = "0.7";

                var sSudden = new Series("Excluding Time") { ChartType = SeriesChartType.StackedColumn, Color = Color.FromArgb(255, 186, 186), IsValueShownAsLabel = true }; // Pastel Red
                sSudden["PointWidth"] = "0.7";

                var sIdle = new Series("Idle/Unaccounted") { ChartType = SeriesChartType.StackedColumn, Color = Color.FromArgb(250, 215, 161), IsValueShownAsLabel = true }; // Pastel Orange
                sIdle["PointWidth"] = "0.7";

                var sEffLabel = new Series("Eff %") { ChartType = SeriesChartType.Point, Color = Color.Transparent, IsValueShownAsLabel = false, IsVisibleInLegend = false };
                sEffLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);

                foreach (var item in data)
                {
                    double autoValMinutes = item.AutoTime / 60.0;
                    double monValMinutes = item.MonitorTime / 60.0;
                    double totalLossValMinutes = (monValMinutes > autoValMinutes) ? (monValMinutes - autoValMinutes) : 0;

                    double breakValMinutes = Math.Min(totalLossValMinutes, maxBreakMinutes);
                    double remainingLoss = totalLossValMinutes - breakValMinutes;

                    double plannedMin = item.PlannedStopMinutes;
                    double suddenMin = item.SuddenStopMinutes;
                    double idleMin = 0;

                    if (plannedMin + suddenMin > remainingLoss && remainingLoss > 0)
                    {
                        // Cap manual inputs if they exceed PLC MonitorTime to preserve the aesthetic 100% stack layout
                        double scale = remainingLoss / (plannedMin + suddenMin);
                        plannedMin *= scale;
                        suddenMin *= scale;
                        idleMin = 0;
                    }
                    else if (remainingLoss > 0)
                    {
                        idleMin = remainingLoss - plannedMin - suddenMin;
                        if (idleMin < 0) idleMin = 0;
                    }
                    else {
                        plannedMin = 0;
                        suddenMin = 0;
                        idleMin = 0;
                    }

                    int p1 = sAuto.Points.AddXY(item.MachineName, autoValMinutes);
                    sAuto.Points[p1].Label = autoValMinutes > 0 ? $"{autoValMinutes:N0}" : " ";

                    int pBreak = sBreak.Points.AddXY(item.MachineName, breakValMinutes);
                    sBreak.Points[pBreak].Label = breakValMinutes > 0 ? $"{breakValMinutes:N0}" : " ";

                    int pPlan = sPlanned.Points.AddXY(item.MachineName, plannedMin);
                    sPlanned.Points[pPlan].Label = plannedMin > 0 ? $"{plannedMin:N0}" : " ";

                    int pSudden = sSudden.Points.AddXY(item.MachineName, suddenMin);
                    sSudden.Points[pSudden].Label = suddenMin > 0 ? $"{suddenMin:N0}" : " ";

                    int pIdle = sIdle.Points.AddXY(item.MachineName, idleMin);
                    sIdle.Points[pIdle].Label = idleMin > 0 ? $"{idleMin:N0}" : " ";

                    double maxValRef = data.Count > 0 ? data.Max(x => x.MonitorTime / 60.0) : 0;
                    double labelY = monValMinutes > 0 ? monValMinutes + (maxValRef * 0.02) : 0;
                    int p3 = sEffLabel.Points.AddXY(item.MachineName, labelY);

                    if (monValMinutes > 0)
                    {
                        sEffLabel.Points[p3].Tag = $"{item.Efficiency:F1}%";
                    }

                    sEffLabel.Points[p3].MarkerStyle = MarkerStyle.None;
                }
                _chart.Series.Add(sAuto);
                _chart.Series.Add(sBreak);
                _chart.Series.Add(sPlanned);
                _chart.Series.Add(sSudden);
                _chart.Series.Add(sIdle);
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