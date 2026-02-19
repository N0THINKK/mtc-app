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
            public long ProducedPieces { get; set; } // Total saat ini
            
            // [UBAH] Menjadi Rata-rata Per Menit
            public long MinuteAverage { get; set; } 
            
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
                
                // --- LOGIKA QUERY 1 MENIT ---
                string sql = @"
                    SELECT m.machine_id, 
                           COALESCE(t.type_name, 'UNK') AS type_name, 
                           COALESCE(a.area_name, 'UNK') AS area_name, 
                           m.machine_number,
                           
                           -- Ambil data TERAKHIR (Current)
                           l_curr.produced_pieces AS curr_pieces,
                           l_curr.auto_time AS curr_auto,
                           l_curr.monitor_time AS curr_mon,
                           
                           -- Ambil data 1 MENIT LALU
                           (
                               SELECT produced_pieces 
                               FROM machine_process_logs old 
                               WHERE old.machine_id = m.machine_id 
                                 AND old.created_at <= DATE_SUB(NOW(), INTERVAL 1 MINUTE)
                               ORDER BY old.created_at DESC 
                               LIMIT 1
                           ) AS old_pieces_1m

                    FROM machines m
                    LEFT JOIN machine_types t ON m.type_id = t.type_id
                    LEFT JOIN machine_areas a ON m.area_id = a.area_id
                    
                    LEFT JOIN (
                        SELECT * FROM machine_process_logs p1
                        WHERE log_id = (SELECT MAX(log_id) FROM machine_process_logs p2 WHERE p2.machine_id = p1.machine_id)
                    ) l_curr ON m.machine_id = l_curr.machine_id";
                
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

                var machineList = new List<MachineData>();
                string selectedMetric = _comboMetric.SelectedItem?.ToString();

                foreach (var m in machines)
                {
                    var data = new MachineData { 
                        MachineName = $"{m.type_name}.{m.area_name}-{m.machine_number}" 
                    };
                    
                    long current = (m.curr_pieces != null) ? (long)m.curr_pieces : 0;
                    long old_1m = (m.old_pieces_1m != null) ? (long)m.old_pieces_1m : 0;
                    
                    data.ProducedPieces = current;
                    data.AutoTime = (m.curr_auto != null) ? (double)m.curr_auto : 0;
                    data.MonitorTime = (m.curr_mon != null) ? (double)m.curr_mon : 0;

                    // --- HITUNG RATA-RATA 1 MENIT ---
                    // Jika old_1m = 0 (berarti belum ada data 1 menit lalu), 
                    // maka selisihnya akan sama dengan total produksi (kurang akurat di awal, tapi membaik seiring waktu).
                    
                    // Supaya grafik tidak "kaget" di menit pertama, jika old_1m = 0, kita anggap rata-rata 0 dulu
                    long diff = 0;
                    if (old_1m > 0)
                    {
                         diff = current - old_1m;
                    }
                    else
                    {
                         // Opsi: Tampilkan 0 atau tampilkan current (pilih salah satu)
                         // Saya pilih 0 agar tidak spike di awal
                         diff = 0; 
                    }

                    if (diff < 0) diff = 0; // Reset counter
                    
                    data.MinuteAverage = diff;

                    if (current == 0 && data.MonitorTime == 0) continue;

                    machineList.Add(data);
                }

                // Sorting
                if (selectedMetric.Contains("Efisiensi"))
                {
                    machineList = machineList.OrderByDescending(x => x.Efficiency).ToList();
                }
                else
                {
                    machineList = machineList.OrderByDescending(x => x.MinuteAverage).ToList();
                }

                UpdateChart(machineList, selectedMetric);
                _lblStatus.Text = $"Update: {DateTime.Now:HH:mm:ss} | Aktif: {machineList.Count}";
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

            if (mode.Contains("Rata-rata"))
            {
                // --- MODE 1: RATA-RATA 1 MENIT ---
                area.AxisY.Title = "Output (Pcs / Menit)";
                var sAvg = new Series("1M Avg") { ChartType = SeriesChartType.Column, Color = AppColors.Primary, IsValueShownAsLabel = true };
                sAvg["PointWidth"] = "0.8";

                foreach (var item in data)
                {
                    int idx = sAvg.Points.AddXY(item.MachineName, item.MinuteAverage);
                    sAvg.Points[idx].Label = $"{item.MinuteAverage:N0}"; 
                }
                _chart.Series.Add(sAvg);
            }
            else 
            {
                // --- MODE 2: EFISIENSI & WAKTU (Input Detik -> Tampil Menit) ---
                area.AxisY.Title = "Waktu (Menit)";

                var sAuto = new Series("Auto Time") { ChartType = SeriesChartType.StackedColumn, Color = AppColors.Success, IsValueShownAsLabel = true };
                var sLoss = new Series("Loss Time") { ChartType = SeriesChartType.StackedColumn, Color = AppColors.Danger, IsValueShownAsLabel = true };
                var sEffLabel = new Series("Eff %") { ChartType = SeriesChartType.Point, Color = Color.Transparent, IsValueShownAsLabel = true };
                sEffLabel["LabelStyle"] = "Top"; 

                foreach (var item in data)
                {
                    // [PERBAIKAN KONVERSI DETIK KE MENIT]
                    // Karena data dari Logger adalah DETIK, kita bagi 60.0 agar jadi MENIT
                    double autoValMinutes = item.AutoTime / 60.0; 
                    double monValMinutes = item.MonitorTime / 60.0;
                    
                    double lossValMinutes = (monValMinutes > autoValMinutes) ? (monValMinutes - autoValMinutes) : 0;
                    
                    // Tambahkan ke grafik (Nilai Menit)
                    int p1 = sAuto.Points.AddXY(item.MachineName, autoValMinutes);
                    // Kita pakai N0 (bulat) atau N1 (1 koma) biar enak dilihat
                    sAuto.Points[p1].Label = $"{autoValMinutes:N0}m";

                    int p2 = sLoss.Points.AddXY(item.MachineName, lossValMinutes);
                    // Label loss time
                    sLoss.Points[p2].Label = $"{lossValMinutes:N0}m"; 

                    // Efisiensi tetap sama rumusnya (Detik/Detik sama saja dengan Menit/Menit)
                    double labelY = monValMinutes + (monValMinutes * 0.05); if (labelY==0) labelY=1;
                    int p3 = sEffLabel.Points.AddXY(item.MachineName, labelY);
                    sEffLabel.Points[p3].Label = $"{item.Efficiency:F1}%";
                    sEffLabel.Points[p3].MarkerStyle = MarkerStyle.None;
                }
                _chart.Series.Add(sAuto);
                _chart.Series.Add(sLoss);
                _chart.Series.Add(sEffLabel);
            }
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