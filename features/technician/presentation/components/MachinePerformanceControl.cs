using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class MachinePerformanceControl : UserControl
    {
        private readonly ITechnicianRepository _repository;
        private List<MachinePerformanceDto> _data = new List<MachinePerformanceDto>();
        private DateTime _lastStart = DateTime.Now.AddDays(-7);
        private DateTime _lastEnd = DateTime.Now;
        
        // Layout
        private TableLayoutPanel mainLayout;
        private Panel headerPanel;
        private Panel chartPanel;
        private Label lblTitle;
        private Label lblNoData;
        private ComboBox cmbArea;

        public MachinePerformanceControl(ITechnicianRepository repository)
        {
            _repository = repository;
            InitializeComponent();
            LoadAreas();
        }

        private async void LoadAreas()
        {
            try
            {
                cmbArea.Items.Add("All Areas");
                cmbArea.SelectedIndex = 0;

                using (var conn = DatabaseHelper.GetConnection())
                {
                    var areas = await conn.QueryAsync<string>("SELECT area_name FROM machine_areas ORDER BY area_name");
                    foreach (var area in areas) cmbArea.Items.Add(area);
                }
            }
            catch { /* Ignore */ }
        }

        public async Task LoadDataAsync(DateTime start, DateTime end, string areaOverride = null)
        {
            _lastStart = start;
            _lastEnd = end;
            
            try
            {
                string area = areaOverride;
                if (string.IsNullOrEmpty(area) && cmbArea != null && cmbArea.SelectedItem != null)
                {
                    area = cmbArea.SelectedItem.ToString();
                    if (area == "All Areas") area = null;
                }

                var result = await _repository.GetMachinePerformanceAsync(start, end, area);
                _data = result?.ToList() ?? new List<MachinePerformanceDto>();
                
                chartPanel.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data mesin: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========================================================
        // UI Construction
        // ========================================================
        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(248, 250, 252);

            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); 
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); 

            headerPanel = BuildHeaderPanel();
            mainLayout.Controls.Add(headerPanel, 0, 0);

            chartPanel = BuildChartPanel();
            mainLayout.Controls.Add(chartPanel, 0, 1);

            this.Controls.Add(mainLayout);
        }

        private Panel BuildHeaderPanel()
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                Height = AppDimens.HeaderHeight,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(AppDimens.MarginLarge)
            };

            var flowVertical = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };

            var flowTitleRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, AppDimens.MarginSmall)
            };

            lblTitle = new Label
            {
                Text = "Analisis Downtime Mesin",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 3, AppDimens.SpacingXL, 0)
            };
            flowTitleRow.Controls.Add(lblTitle);

            var lblArea = new Label 
            { 
                Text = "Filter Area:", 
                Font = AppFonts.BodySmall, 
                AutoSize = true,
                Margin = new Padding(0, 7, AppDimens.MarginSmall, 0)
            };
            flowTitleRow.Controls.Add(lblArea);

            cmbArea = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = AppFonts.BodySmall, 
                Width = 120,
                Margin = new Padding(0, 3, 0, 0)
            };
            cmbArea.SelectedIndexChanged += async (s, e) => await LoadDataAsync(_lastStart, _lastEnd);
            flowTitleRow.Controls.Add(cmbArea);

            flowVertical.Controls.Add(flowTitleRow);

            var flowLegend = BuildLegendFlow();
            flowVertical.Controls.Add(flowLegend);

            header.Controls.Add(flowVertical);

            return header;
        }

        private FlowLayoutPanel BuildLegendFlow()
        {
            var flowLegend = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            AddLegendItem(flowLegend, "Respon", AppColors.Danger);
            AddLegendItem(flowLegend, "Perbaikan", AppColors.Warning);
            AddLegendItem(flowLegend, "Tunggu Part", AppColors.Success);
            AddLegendItem(flowLegend, "Tunggu Op", AppColors.Primary);

            return flowLegend;
        }

        private void AddLegendItem(FlowLayoutPanel parent, string text, Color color)
        {
            var pnlColor = new Panel 
            { 
                BackColor = color, 
                Size = new Size(15, 15), 
                Margin = new Padding(0, 3, AppDimens.MarginXS, 0) 
            };
            var lblText = new Label 
            { 
                Text = text, 
                AutoSize = true, 
                Font = AppFonts.Caption,
                Margin = new Padding(0, 0, AppDimens.MarginLarge, 0)
            };
            parent.Controls.Add(pnlColor);
            parent.Controls.Add(lblText);
        }

        private Panel BuildChartPanel()
        {
            var chart = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(AppDimens.MarginLarge),
                AutoScroll = true 
            };
            chart.Paint += ChartPanel_Paint;

            lblNoData = new Label
            {
                Text = "Belum ada data downtime.",
                Font = AppFonts.Title,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            chart.Controls.Add(lblNoData);

            return chart;
        }

        // ========================================================
        // Chart Rendering (BIG SIZE VERSION)
        // ========================================================
        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            if (_data.Count == 0)
            {
                lblNoData.Visible = true;
                return;
            }
            lblNoData.Visible = false;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.TranslateTransform(chartPanel.AutoScrollPosition.X, chartPanel.AutoScrollPosition.Y);

            // --- Layout Parameters (BIGGER) ---
            int padding = 20;
            int bottomLabelHeight = 90; // Lebih tinggi sedikit untuk nama mesin panjang
            int topValueHeight = 35;    // Lebih tinggi untuk font besar
            
            // UKURAN BAR & GAP (Diperbesar)
            int barWidth = 85;          // Naik dari 45
            int gap = 35;               // Naik dari 20
            
            int availableHeight = chartPanel.Height - padding - bottomLabelHeight - topValueHeight;
            if (availableHeight < 150) availableHeight = 150; // Min height area gambar

            int totalContentWidth = padding + (_data.Count * (barWidth + gap)) + padding;
            chartPanel.AutoScrollMinSize = new Size(totalContentWidth, 0);

            double maxDowntime = _data.Max(m => m.TotalDowntimeSeconds);
            if (maxDowntime == 0) maxDowntime = 1;

            int chartBottomY = padding + topValueHeight + availableHeight;
            int currentX = padding;

            // --- SETTING: Min Height = 12 (Agar font 9pt muat) ---
            int minVisualHeight = 12; 

            // Helper function untuk gambar teks di dalam bar (FONT BESAR)
            void DrawSegmentLabel(double seconds, int x, int y, int h, bool isDarkBackground = true)
            {
                // Jangan gambar teks jika tinggi bar terlalu pendek (kurang dari 14px)
                // Biarkan warnanya saja yang terlihat agar rapi
                if (h < 14) return; 

                TimeSpan t = TimeSpan.FromSeconds(seconds);
                string txt = "";
                if (t.TotalHours >= 1) txt = $"{(int)t.TotalHours}h";
                else if (t.TotalMinutes >= 1) txt = $"{(int)t.TotalMinutes}m";
                else txt = $"{t.Seconds}s";

                Color textColor = isDarkBackground ? Color.White : Color.Black;

                // FONT: 9pt Bold (Naik dari 6pt)
                using (var font = new Font("Segoe UI", 9F, FontStyle.Bold)) 
                using (var brush = new SolidBrush(textColor))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(txt, font, brush, new RectangleF(x, y, barWidth, h), format);
                }
            }

            foreach (var item in _data)
            {
                // 1. Calculate Raw Heights
                int hResponse = (int)((item.ResponseDurationSeconds / maxDowntime) * availableHeight);
                int hRepair = (int)((item.RepairDurationSeconds / maxDowntime) * availableHeight);
                int hPart = (int)((item.PartWaitDurationSeconds / maxDowntime) * availableHeight);
                int hOp = (int)((item.OperatorWaitDurationSeconds / maxDowntime) * availableHeight);

                // 2. Apply Minimum Height Logic
                if (item.ResponseDurationSeconds > 0 && hResponse < minVisualHeight) hResponse = minVisualHeight;
                if (item.RepairDurationSeconds > 0 && hRepair < minVisualHeight) hRepair = minVisualHeight;
                if (item.PartWaitDurationSeconds > 0 && hPart < minVisualHeight) hPart = minVisualHeight;
                if (item.OperatorWaitDurationSeconds > 0 && hOp < minVisualHeight) hOp = minVisualHeight;

                // 3. Draw Stacked Bars & Inner Labels
                int currentY = chartBottomY;

                // Layer 1: Response (Red)
                if (hResponse > 0)
                {
                    currentY -= hResponse;
                    using (var b = new SolidBrush(AppColors.Danger)) 
                        g.FillRectangle(b, currentX, currentY, barWidth, hResponse);
                    DrawSegmentLabel(item.ResponseDurationSeconds, currentX, currentY, hResponse, true);
                }

                // Layer 2: Repair (Yellow)
                if (hRepair > 0)
                {
                    currentY -= hRepair;
                    using (var b = new SolidBrush(AppColors.Warning)) 
                        g.FillRectangle(b, currentX, currentY, barWidth, hRepair);
                    DrawSegmentLabel(item.RepairDurationSeconds, currentX, currentY, hRepair, false);
                }

                // Layer 3: Wait Part (Green)
                if (hPart > 0)
                {
                    currentY -= hPart;
                    using (var b = new SolidBrush(AppColors.Success)) 
                        g.FillRectangle(b, currentX, currentY, barWidth, hPart);
                    DrawSegmentLabel(item.PartWaitDurationSeconds, currentX, currentY, hPart, true);
                }

                // Layer 4: Wait Op (Blue)
                if (hOp > 0)
                {
                    currentY -= hOp;
                    using (var b = new SolidBrush(AppColors.Primary)) 
                        g.FillRectangle(b, currentX, currentY, barWidth, hOp);
                    DrawSegmentLabel(item.OperatorWaitDurationSeconds, currentX, currentY, hOp, true);
                }

                // 4. Draw Total Time Label (Top) - FONT 10pt Bold
                TimeSpan totalTime = TimeSpan.FromSeconds(item.TotalDowntimeSeconds);
                string totalStr = totalTime.TotalHours >= 1 
                    ? $"{(int)totalTime.TotalHours}h" 
                    : $"{totalTime.Minutes}m";

                using (var font = new Font("Segoe UI", 10F, FontStyle.Bold)) // Font Besar
                using (var brush = new SolidBrush(Color.Black))
                using (var format = new StringFormat { Alignment = StringAlignment.Center })
                {
                    g.DrawString(totalStr, font, brush, currentX + (barWidth / 2), currentY - 20, format);
                }

                // 5. Draw Machine Name (Bottom) - FONT 9pt
                string machineName = item.MachineName ?? "-";
                RectangleF textRect = new RectangleF(currentX - 5, chartBottomY + 8, barWidth + 10, bottomLabelHeight);
                using (var font = new Font("Segoe UI", 9F)) // Font Standar
                using (var brush = new SolidBrush(AppColors.TextSecondary))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
                {
                    g.DrawString(machineName, font, brush, textRect, format);
                }

                currentX += (barWidth + gap);
            }
        }
    }
}