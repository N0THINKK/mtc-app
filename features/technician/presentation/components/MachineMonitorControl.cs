using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using mtc_app.shared.presentation.styles;
using mtc_app.features.technician.presentation.controllers;
using mtc_app.features.technician.data.repositories;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.presentation.components
{
    public class MachineMonitorControl : UserControl, IMachineMonitorView
    {
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
        
        private readonly MachineMonitorController _controller;

        // Background cache state
        private Dictionary<int, List<MachineProcessLogAggregateDto>> _bgCache = null;
        private Dictionary<int, List<MachineDowntimeDto>> _bgDowntimeCache = null;
        private bool _bgCacheReady = false;
        private DateTime _bgCacheShiftStart;
        private DateTime _bgCacheShiftEnd;

        private System.ComponentModel.IContainer components = null;

        public MachineMonitorControl()
        {
            InitializeComponent();
            _controller = new MachineMonitorController(this, new MachineMonitorRepository());
            SetupTimer();
            LoadAreasAsync();
        }

        // --- IMachineMonitorView Implementation ---
        public string SelectedArea => _comboArea.SelectedItem?.ToString() ?? "Semua Area";
        public string SelectedMetric => _comboMetric.SelectedItem?.ToString();
        public string SelectedSort => _comboSort.SelectedItem?.ToString();
        public int SelectedShiftIndex => _comboShift.SelectedIndex;
        public DateTime SelectedDate => _dtpDateFilter.Value;

        public void UpdateStatus(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => _lblStatus.Text = text));
            }
            else
            {
                _lblStatus.Text = text;
            }
        }

        public void SetLoadingState(bool isLoading)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetLoadingState(isLoading)));
                return;
            }
            
            // Just disable controls slightly while loading
            _btnRefresh.Enabled = !isLoading;
        }

        public void UpdateChart(List<MachineMonitorDto> data, string metric, int currentHourCount, int maxBreakMinutes)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateChart(data, metric, currentHourCount, maxBreakMinutes)));
                return;
            }

            _chart.Series.Clear();
            _chart.Annotations.Clear();
            var area = _chart.ChartAreas[0];

            area.AxisY.Minimum = 0;
            area.AxisY.Title = "";
            area.AxisY.LabelStyle.Format = "N0";

            int maxVisible = 40;
            if (data.Count > maxVisible)
            {
                area.AxisX.ScaleView.Zoomable = true;
                area.AxisX.ScaleView.Size = maxVisible;
            }
            else
            {
                area.AxisX.ScaleView.Zoomable = false;
                area.AxisX.ScaleView.Size = double.NaN;
            }

            if (metric == "Output Per Jam")
            {
                var sAvg = new Series("Avg Output/Jam")
                {
                    ChartType = SeriesChartType.Column,
                    Color = AppColors.Primary,
                    IsValueShownAsLabel = false,
                    Font = AppFonts.BodySmall
                };

                var sTarget = new Series("Target")
                {
                    ChartType = SeriesChartType.Line,
                    Color = AppColors.Danger,
                    BorderWidth = 3,
                    BorderDashStyle = ChartDashStyle.Solid
                };

                foreach (var m in data)
                {
                    int ptIdx = sAvg.Points.AddXY(m.MachineNum, m.AveragePerHour);
                    var p = sAvg.Points[ptIdx];
                    p.ToolTip = $"Mesin: {m.MachineName}\nAvg: {m.AveragePerHour:F1}/jam";
                    p.Tag = m.AveragePerHour.ToString("F1");

                    if (m.AveragePerHour < m.TargetPerHour * 0.9)
                        p.Color = AppColors.Warning;
                    else if (m.AveragePerHour >= m.TargetPerHour)
                        p.Color = AppColors.Success;

                    sTarget.Points.AddXY(m.MachineNum, m.TargetPerHour);
                }

                _chart.Series.Add(sAvg);
                _chart.Series.Add(sTarget);
                area.AxisY.Title = "Pieces / Jam";
            }
            else if (metric == "Total")
            {
                var sTotal = new Series("Total")
                {
                    ChartType = SeriesChartType.Column,
                    Color = AppColors.Info,
                    IsValueShownAsLabel = false,
                    Font = AppFonts.BodySmall
                };

                var sTargetTotal = new Series("Target Total")
                {
                    ChartType = SeriesChartType.Line,
                    Color = AppColors.Danger,
                    BorderWidth = 3
                };

                foreach (var m in data)
                {
                    int ptIdx = sTotal.Points.AddXY(m.MachineNum, m.TotalPieces);
                    var p = sTotal.Points[ptIdx];
                    p.ToolTip = $"Mesin: {m.MachineName}\nTotal: {m.TotalPieces}";
                    p.Tag = m.TotalPieces.ToString("N0");

                    double currentTarget = m.TargetPerHour * (currentHourCount - (maxBreakMinutes / 60.0));
                    sTargetTotal.Points.AddXY(m.MachineNum, currentTarget);

                    if (m.TotalPieces < currentTarget * 0.9) p.Color = AppColors.Warning;
                    else if (m.TotalPieces >= currentTarget) p.Color = AppColors.Success;
                }

                _chart.Series.Add(sTotal);
                _chart.Series.Add(sTargetTotal);
                area.AxisY.Title = "Pieces";
            }
            else if (metric != null && metric.Contains("Efisiensi"))
            {
                var sEff = new Series("Efisiensi")
                {
                    ChartType = SeriesChartType.StackedColumn,
                    Color = AppColors.Success
                };
                var sPlan = new Series("Planned Stop")
                {
                    ChartType = SeriesChartType.StackedColumn,
                    Color = AppColors.Warning
                };
                var sSudden = new Series("Sudden Stop")
                {
                    ChartType = SeriesChartType.StackedColumn,
                    Color = AppColors.Danger
                };
                var sUnk = new Series("Unknown Loss")
                {
                    ChartType = SeriesChartType.StackedColumn,
                    Color = Color.LightGray
                };

                var sEffLabel = new Series("Eff %")
                {
                    ChartType = SeriesChartType.Line,
                    Color = Color.Transparent,
                    IsValueShownAsLabel = false,
                    Font = AppFonts.BodySmall
                };

                area.AxisY.Maximum = 100;

                foreach (var m in data)
                {
                    double totalTime = m.MonitorTime;
                    if (totalTime <= 0)
                    {
                        sEff.Points.AddXY(m.MachineNum, 0);
                        sPlan.Points.AddXY(m.MachineNum, 0);
                        sSudden.Points.AddXY(m.MachineNum, 0);
                        sUnk.Points.AddXY(m.MachineNum, 0);

                        int lblIdx = sEffLabel.Points.AddXY(m.MachineNum, 100);
                        sEffLabel.Points[lblIdx].Tag = "0%";
                        continue;
                    }

                    double pAuto = (m.AutoTime / totalTime) * 100;
                    double pPlan = (m.PlannedStopMinutes / totalTime) * 100;
                    double pSudden = (m.SuddenStopMinutes / totalTime) * 100;

                    double pUnk = 100 - (pAuto + pPlan + pSudden);
                    if (pUnk < 0) pUnk = 0;
                    if (pAuto > 100) pAuto = 100;

                    int idx1 = sEff.Points.AddXY(m.MachineNum, pAuto);
                    sEff.Points[idx1].ToolTip = $"Efisiensi: {pAuto:F1}%";

                    int idx2 = sPlan.Points.AddXY(m.MachineNum, pPlan);
                    sPlan.Points[idx2].ToolTip = $"Planned Stop: {m.PlannedStopMinutes} min ({pPlan:F1}%)";

                    int idx3 = sSudden.Points.AddXY(m.MachineNum, pSudden);
                    sSudden.Points[idx3].ToolTip = $"Sudden Stop: {m.SuddenStopMinutes} min ({pSudden:F1}%)";

                    int idx4 = sUnk.Points.AddXY(m.MachineNum, pUnk);
                    sUnk.Points[idx4].ToolTip = $"Loss: {pUnk:F1}%";

                    int lblIdx2 = sEffLabel.Points.AddXY(m.MachineNum, 100);
                    sEffLabel.Points[lblIdx2].Tag = $"{pAuto:F1}%";
                }

                _chart.Series.Add(sEff);
                _chart.Series.Add(sPlan);
                _chart.Series.Add(sSudden);
                _chart.Series.Add(sUnk);
                _chart.Series.Add(sEffLabel);
            }
        }

        public void PreloadAllAreasBackground(DateTime shiftStart, DateTime shiftEnd)
        {
            _ = Task.Run(async () => await _controller.ExecuteBackgroundPreloadAsync(shiftStart, shiftEnd));
        }

        public void NotifyCacheReady(DateTime shiftStart, DateTime shiftEnd, Dictionary<int, List<MachineProcessLogAggregateDto>> cache, Dictionary<int, List<MachineDowntimeDto>> downtimeCache)
        {
            _bgCache = cache;
            _bgDowntimeCache = downtimeCache;
            _bgCacheShiftStart = shiftStart;
            _bgCacheShiftEnd = shiftEnd;
            _bgCacheReady = true;
            System.Diagnostics.Debug.WriteLine($"[BgCache] Pre-loaded {cache.Count} machines");
        }

        public bool IsBackgroundCacheReady(DateTime shiftStart, DateTime shiftEnd)
        {
            return _bgCacheReady && _bgCacheShiftStart == shiftStart && _bgCacheShiftEnd == shiftEnd;
        }

        public Dictionary<int, List<MachineProcessLogAggregateDto>> GetBackgroundCache() => _bgCache;
        public Dictionary<int, List<MachineDowntimeDto>> GetBackgroundDowntimeCache() => _bgDowntimeCache;

        // --- View Logic & Initialization ---

        private async void LoadAreasAsync()
        {
            try
            {
                _comboArea.Items.Add("Semua Area");
                var areas = await _controller.GetAreasAsync();
                foreach (var area in areas) _comboArea.Items.Add(area);

                if (_comboArea.Items.Count > 1) _comboArea.SelectedIndex = 1;
                else if (_comboArea.Items.Count > 0) _comboArea.SelectedIndex = 0;
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
                currentAreaIndex = 1; 
                currentMetricIndex++;

                if (currentMetricIndex >= metricCount) return true;
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
            _chart.MinimumSize = new Size(10, 10);
            _chart.Dock = DockStyle.Fill;
            _chart.BackColor = Color.White;

            var chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Angle = -45;
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
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

            var lblTitle = new Label { Text = "Monitoring Output Cutting ", Font = AppFonts.PageTitle, ForeColor = AppColors.TextPrimary, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top, Height = 40 };
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

            _comboSort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppFonts.BodySmall, Width = 110, Margin = new Padding(0, 10, 10, 0) };
            _comboSort.Items.AddRange(new object[] { "↓ Tertinggi", "↑ Terendah", "Nomor Mesin" });
            _comboSort.SelectedIndex = 2; 
            _comboSort.SelectedIndexChanged += async (s, e) => await _controller.LoadDataAsync();
            var lblSort = new Label { Text = "Urutkan:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            _comboMetric = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _comboMetric.Items.AddRange(new object[] { "Output Per Jam", "Total", "Efisiensi Mesin" });
            _comboMetric.SelectedIndex = 0;
            _comboMetric.SelectedIndexChanged += async (s, e) => await _controller.LoadDataAsync();
            var lblMetric = new Label { Text = "Metrik:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            _comboArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _comboArea.SelectedIndexChanged += async (s, e) => await _controller.LoadDataAsync();
            var lblArea = new Label { Text = "Area:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            _comboShift = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _comboShift.Items.AddRange(new object[] { "Waktu Aktual", "Shift Pagi", "Shift Malam" });
            _comboShift.SelectedIndex = 0;
            _comboShift.SelectedIndexChanged += async (s, e) =>
            {
                _dtpDateFilter.Enabled = _comboShift.SelectedIndex != 0;
                _bgCacheReady = false; 
                await _controller.LoadDataAsync();
            };
            var lblShift = new Label { Text = "Shift:", AutoSize = true, Font = AppFonts.BodySmall, Margin = new Padding(0, 13, 5, 0) };

            _dtpDateFilter = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Font = AppFonts.BodySmall, Margin = new Padding(0, 10, 10, 0) };
            _dtpDateFilter.Enabled = false;
            _dtpDateFilter.ValueChanged += async (s, e) => { _bgCacheReady = false; await _controller.LoadDataAsync(); };
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
            _timer.Tick += async (s, e) => await _controller.LoadDataAsync();
        }

        private async Task HandleManualRefresh()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Start();
            }

            _btnRefresh.Enabled = false;
            await _controller.LoadDataAsync();

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

        public void StartMonitoring() { _ = _controller.LoadDataAsync(); _timer.Start(); }
        public void StopMonitoring() { _timer.Stop(); }
        public void SetMetric(int index) { if (index >= 0 && index < _comboMetric.Items.Count) _comboMetric.SelectedIndex = index; }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (components != null) components.Dispose(); if (_timer != null) { _timer.Stop(); _timer.Dispose(); } }
            base.Dispose(disposing);
        }
    }
}