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
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));

            var flowLeft = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _lblStatus = new Label { Text = "Memuat data...", Font = AppFonts.BodySmall, ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
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

        private void SetupTimer()
        {
            _timer = new Timer();
            _timer.Interval = REFRESH_RATE_MS;
            _timer.Tick += async (s, e) => await LoadData();
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

                // --- SQL: UNION ALL with FIRST/LAST/MAX per hour (handles counter resets) ---
                // Part 1: Machine list (no log table touch, instant)
                // Part 2: Aggregated logs with WHERE filter (uses index on machine_id + created_at)
                // NOTE: GROUP_CONCAT is needed for FIRST/LAST to detect counter resets correctly.
                //       MIN/MAX cannot detect resets (e.g. 12000→0→500 gives MAX-MIN=12000, wrong!)
                string sql = @"
                SELECT m.machine_id,
                        m.type_id,
                        m.area_id,
                        COALESCE(t.type_name, 'UNK') AS type_name,
                        COALESCE(a.area_name, 'UNK') AS area_name,
                        m.machine_number,
                        CAST(NULL AS SIGNED) AS hour_index,
                        0 AS max_pieces, 0 AS first_pieces, 0 AS last_pieces,
                        0 AS curr_auto, 0 AS curr_mon
                FROM machines m
                LEFT JOIN machine_types t ON m.type_id = t.type_id
                LEFT JOIN machine_areas a ON m.area_id = a.area_id
                WHERE (@Area = 'Semua Area' OR a.area_name = @Area)

                UNION ALL

                SELECT m.machine_id,
                        m.type_id,
                        m.area_id,
                        COALESCE(t.type_name, 'UNK') AS type_name,
                        COALESCE(a.area_name, 'UNK') AS area_name,
                        m.machine_number,
                        TIMESTAMPDIFF(HOUR, @ShiftStart, p.created_at) AS hour_index,
                        MAX(p.produced_pieces) AS max_pieces,
                        CAST(SUBSTRING_INDEX(GROUP_CONCAT(p.produced_pieces ORDER BY p.created_at ASC SEPARATOR ','), ',', 1) AS SIGNED) AS first_pieces,
                        CAST(SUBSTRING_INDEX(GROUP_CONCAT(p.produced_pieces ORDER BY p.created_at DESC SEPARATOR ','), ',', 1) AS SIGNED) AS last_pieces,
                        MAX(p.auto_time) AS curr_auto,
                        MAX(p.monitor_time) AS curr_mon
                FROM machines m
                LEFT JOIN machine_types t ON m.type_id = t.type_id
                LEFT JOIN machine_areas a ON m.area_id = a.area_id
                JOIN machine_process_logs p ON m.machine_id = p.machine_id
                WHERE (@Area = 'Semua Area' OR a.area_name = @Area)
                    AND p.created_at >= @ShiftStart
                    AND p.created_at < @ShiftEnd
                    AND p.produced_pieces > 0
                GROUP BY m.machine_id, t.type_name, a.area_name, m.machine_number, hour_index
                ORDER BY machine_id, hour_index;";

                IEnumerable<dynamic> rows;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    rows = await conn.QueryAsync(sql, new { Area = selectedArea, ShiftStart = shiftStart, ShiftEnd = shiftEnd }, commandTimeout: 60);
                }

                // --- C# ALGORITHM: FIRST/LAST/MAX per hour (handles counter resets) ---
                var hourFirst = new Dictionary<int, long[]>();
                var hourLast = new Dictionary<int, long[]>();
                var hourMax = new Dictionary<int, long[]>();
                var machines = new Dictionary<int, MachineData>();

                foreach (var row in rows)
                {
                    int mId = (int)row.machine_id;
                    if (!machines.ContainsKey(mId))
                    {
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

                    // hour_index is NULL for machine-list rows (Part 1 of UNION ALL)
                    if (row.hour_index == null) continue;

                    int hIndex = (int)row.hour_index;
                    if (hIndex >= 0 && hIndex < maxShiftHours)
                    {
                        hourFirst[mId][hIndex] = (long)(row.first_pieces ?? 0);
                        hourLast[mId][hIndex] = (long)(row.last_pieces ?? 0);
                        hourMax[mId][hIndex] = (long)(row.max_pieces ?? 0);

                        machines[mId].AutoTime = Math.Max(machines[mId].AutoTime, (double)(row.curr_auto ?? 0));
                        machines[mId].MonitorTime = Math.Max(machines[mId].MonitorTime, (double)(row.curr_mon ?? 0));
                    }
                }

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

                // --- Load targets from DB and map to each machine ---
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        var targets = await conn.QueryAsync(
                            "SELECT type_id, area_id, machine_number, target_per_hour FROM machine_output_targets");

                        var targetMap = new Dictionary<string, int>();
                        foreach (var t in targets)
                        {
                            string key = $"{(int)t.type_id}_{(int)t.area_id}_{t.machine_number}";
                            targetMap[key] = (int)t.target_per_hour;
                        }

                        foreach (var machine in machines.Values)
                        {
                            string key = $"{machine.TypeId}_{machine.AreaId}_{machine.MachineNum}";
                            if (targetMap.TryGetValue(key, out int target))
                                machine.TargetPerHour = target;
                        }
                    }
                }
                catch { /* Target table might not exist yet — silently ignore */ }

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

                UpdateChart(machineList, selectedMetric, currentHourCount);

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

        private void UpdateChart(List<MachineData> data, string mode, int currentHourCount)
        {
            _chart.Series.Clear();
            var area = _chart.ChartAreas[0];

            area.AxisY.Maximum = Double.NaN;
            area.AxisY.Minimum = 0;
            area.AxisY.Title = "";

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

            if (mode == "Output Per Jam")
            {
                // --- MODE 1: Single bar (avg/hour) + target dashed line ---
                area.AxisY.Title = "Output Per Jam (Pcs)";

                var sBar = new Series("Avg Output/Jam")
                {
                    ChartType = SeriesChartType.Column,
                    Color = Color.FromArgb(0, 229, 255), // Cyan
                    IsValueShownAsLabel = true,
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
                        sBar.Points[idx].Label = $"{item.AveragePerHour:N0}";

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
                area.AxisY.Title = "Total Output (Pcs)";

                Color[] hourColors = new Color[] {
                Color.FromArgb(255, 64, 129), // Pink
                Color.FromArgb(255, 145, 0),  // Orange
                Color.FromArgb(255, 234, 0),  // Yellow
                Color.FromArgb(0, 230, 118),  // Green
                Color.FromArgb(0, 229, 255),  // Cyan
                Color.FromArgb(213, 0, 249),  // Purple
                Color.FromArgb(245, 0, 87),   // Rose
                Color.FromArgb(118, 255, 3),  // Lime
                Color.FromArgb(255, 61, 0),   // Deep Orange
                Color.FromArgb(29, 233, 182), // Teal
                Color.FromArgb(224, 64, 251), // Light Purple
                Color.FromArgb(198, 255, 0),  // Yellow Green
                Color.FromArgb(255, 82, 82),  // Light Red
                Color.FromArgb(68, 138, 255)   // Blue
            };

                for (int i = 0; i < currentHourCount; i++)
                {
                    var s = new Series($"Jam {i + 1}")
                    {
                        ChartType = SeriesChartType.StackedColumn,
                        Color = hourColors[i],
                        IsValueShownAsLabel = false
                    };
                    s["PointWidth"] = "0.7";
                    _chart.Series.Add(s);
                }

                // Total label on top (no avg)
                var sTotLabel = new Series("Total")
                {
                    ChartType = SeriesChartType.Point,
                    MarkerStyle = MarkerStyle.None,
                    Color = Color.Transparent,
                    IsValueShownAsLabel = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                sTotLabel["LabelStyle"] = "Top";
                _chart.Series.Add(sTotLabel);

                foreach (var item in data)
                {
                    for (int i = 0; i < currentHourCount; i++)
                    {
                        int pIdx = _chart.Series[i].Points.AddXY(item.MachineName, item.HourlyPieces[i]);
                        if (item.HourlyPieces[i] > 0)
                            _chart.Series[i].Points[pIdx].Label = item.HourlyPieces[i].ToString("N0");
                    }

                    double labelYPos = item.TotalPieces > 0 ? item.TotalPieces + (item.TotalPieces * 0.05) : 0;
                    int pTot = sTotLabel.Points.AddXY(item.MachineName, labelYPos);
                    sTotLabel.Points[pTot].Label = item.TotalPieces > 0 ? $"Tot: {item.TotalPieces:N0}" : " ";
                }
            }
            else // Efisiensi Mesin
            {
                area.AxisY.Title = "Waktu (Menit)";

                var sAuto = new Series("Auto Time") { ChartType = SeriesChartType.StackedColumn, Color = AppColors.Success, IsValueShownAsLabel = true };
                sAuto["PointWidth"] = "0.7";

                var sLoss = new Series("Loss Time") { ChartType = SeriesChartType.StackedColumn, Color = Color.FromArgb(230, 126, 34), IsValueShownAsLabel = true };
                sLoss["PointWidth"] = "0.7";

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