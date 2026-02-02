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
                    var areas = await conn.QueryAsync<string>("SELECT DISTINCT machine_area FROM machines ORDER BY machine_area");
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
                // Use combo box selection if no override provided (or if called from Dashboard refresh)
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

            // === Header ===
            headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 80, 
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            
            lblTitle = new Label
            {
                Text = "Analisis Downtime Mesin",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            // Area Filter
            var lblArea = new Label 
            { 
                Text = "Filter Area:", 
                Font = AppFonts.BodySmall, 
                Location = new Point(400, 20), 
                AutoSize = true 
            };
            headerPanel.Controls.Add(lblArea);

            cmbArea = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = AppFonts.BodySmall,
                Location = new Point(480, 16),
                Width = 120
            };
            cmbArea.SelectedIndexChanged += async (s, e) => await LoadDataAsync(_lastStart, _lastEnd);
            headerPanel.Controls.Add(cmbArea);

            // Legend
            DrawLegend(headerPanel, 20, 50);

            mainLayout.Controls.Add(headerPanel, 0, 0);

            // === Chart ===
            chartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20),
                AutoScroll = true 
            };
            chartPanel.Paint += ChartPanel_Paint;

            lblNoData = new Label
            {
                Text = "Belum ada data downtime.",
                Font = AppFonts.Title,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            chartPanel.Controls.Add(lblNoData);

            mainLayout.Controls.Add(chartPanel, 0, 1);
            this.Controls.Add(mainLayout);
        }

        private void DrawLegend(Panel panel, int x, int y)
        {
            // Simple legend labels
            CreateLegendItem(panel, "Respon", AppColors.Danger, x, y);
            CreateLegendItem(panel, "Perbaikan", AppColors.Warning, x + 100, y);
            CreateLegendItem(panel, "Tunggu Part", AppColors.Success, x + 200, y);
            CreateLegendItem(panel, "Tunggu Op", AppColors.Primary, x + 320, y);
        }

        private void CreateLegendItem(Panel panel, string text, Color color, int x, int y)
        {
            var pnlColor = new Panel { BackColor = color, Size = new Size(15, 15), Location = new Point(x, y + 3) };
            var lblText = new Label { Text = text, Location = new Point(x + 20, y), AutoSize = true, Font = AppFonts.Caption };
            panel.Controls.Add(pnlColor);
            panel.Controls.Add(lblText);
        }

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

            // Dimensions
            int padding = 20;
            int labelWidth = 150; // Machine Name width
            int chartLeft = labelWidth + padding;
            int chartWidth = chartPanel.Width - chartLeft - padding - 120; // Increased right padding for Total Text
            int rowHeight = 50;

            // Calculate Max Total Downtime for scaling
            double maxDowntime = _data.Max(m => m.TotalDowntimeSeconds);
            if (maxDowntime == 0) maxDowntime = 1;

            int y = padding;

            // Helper to draw text inside bar
            void DrawBarLabel(float x, float width, double seconds)
            {
                if (width < 30) return; 
                TimeSpan t = TimeSpan.FromSeconds(seconds);
                string txt = "";
                if (t.TotalHours >= 1) txt = $"{(int)t.TotalHours}h";
                else if (t.TotalMinutes >= 1) txt = $"{(int)t.TotalMinutes}m";
                
                if (string.IsNullOrEmpty(txt)) return;

                using (var font = new Font("Segoe UI", 8F, FontStyle.Regular))
                using (var brush = new SolidBrush(Color.White))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(txt, font, brush, new RectangleF(x, y, width, 30), format);
                }
            }

            foreach (var item in _data)
            {
                // Machine Name
                g.DrawString(item.MachineName, new Font("Segoe UI", 10F), Brushes.Black, padding, y + 10);

                // Calculate Widths
                float scale = (float)(chartWidth / maxDowntime);
                
                float wResponse = (float)(item.ResponseDurationSeconds * scale);
                float wRepair = (float)(item.RepairDurationSeconds * scale);
                float wPart = (float)(item.PartWaitDurationSeconds * scale);
                float wOp = (float)(item.OperatorWaitDurationSeconds * scale);

                float currentX = chartLeft;

                // Draw Stacked Bars with Labels
                if (wResponse > 0)
                {
                    g.FillRectangle(new SolidBrush(AppColors.Danger), currentX, y, wResponse, 30);
                    DrawBarLabel(currentX, wResponse, item.ResponseDurationSeconds);
                    currentX += wResponse;
                }

                if (wRepair > 0)
                {
                    g.FillRectangle(new SolidBrush(AppColors.Warning), currentX, y, wRepair, 30);
                    DrawBarLabel(currentX, wRepair, item.RepairDurationSeconds);
                    currentX += wRepair;
                }

                if (wPart > 0)
                {
                    g.FillRectangle(new SolidBrush(AppColors.Success), currentX, y, wPart, 30);
                    DrawBarLabel(currentX, wPart, item.PartWaitDurationSeconds);
                    currentX += wPart;
                }

                if (wOp > 0)
                {
                    g.FillRectangle(new SolidBrush(AppColors.Primary), currentX, y, wOp, 30);
                    DrawBarLabel(currentX, wOp, item.OperatorWaitDurationSeconds);
                    currentX += wOp;
                }

                // Draw Total Text
                TimeSpan totalTime = TimeSpan.FromSeconds(item.TotalDowntimeSeconds);
                string totalStr = $"{(int)totalTime.TotalHours}h {totalTime.Minutes}m";
                g.DrawString(totalStr, new Font("Segoe UI", 9F, FontStyle.Bold), Brushes.Black, currentX + 10, y + 8);

                y += rowHeight;
            }
            
            // Adjust panel height if scrolling needed (for AutoScroll)
            chartPanel.AutoScrollMinSize = new Size(0, y + padding);
        }
    }
}