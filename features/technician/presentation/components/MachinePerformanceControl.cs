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
                Height = AppDimens.HeaderHeight, 
                BackColor = AppColors.CardBackground,
                Padding = new Padding(AppDimens.MarginLarge)
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
                BackColor = AppColors.CardBackground,
                Padding = new Padding(AppDimens.MarginLarge),
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

            // === 1. DEFINISI LAYOUT ===
            int padding = 20;            // Margin Kiri/Atas
            int paddingRight = 2;        // [MAXIMIZE] Margin Kanan hampir 0
            int labelMachineWidth = 100;  // [OPTIMIZE] Dipersempit agar chart maju ke kiri
            int textTotalWidth = 75;     // [MAXIMIZE] Lebar area text super ketat
            int gap = 5;                 // [MAXIMIZE] Gap minimal

            // Titik mulai Chart (kiri)
            // [OPTIMIZE] Rapatkan chart ke nama mesin (jarak 5px)
            int chartStartX = padding + labelMachineWidth + 5; 

            // Titik mulai Text Total (kanan)
            // Kita kunci ini di kanan panel agar rapi seperti kolom tabel
            int textStartX = chartPanel.Width - paddingRight - textTotalWidth;

            // Lebar Maksimal yang boleh dipakai oleh Bar
            // Rumus: (Posisi Text) - (Gap) - (Posisi Awal Chart)
            int maxAvailableBarWidth = textStartX - gap - chartStartX;

            // Safety check jika window terlalu kecil
            if (maxAvailableBarWidth < 10) maxAvailableBarWidth = 10;

            int rowHeight = AppDimens.RowHeight;
            int minBarWidth = 4; // Lebar minimum visual

            // Calculate Max Total Downtime for scaling
            double maxDowntime = _data.Max(m => m.TotalDowntimeSeconds);
            if (maxDowntime == 0) maxDowntime = 1;

            // Hitung Skala: 1 detik = berapa pixel?
            float scaleFactor = (float)(maxAvailableBarWidth / maxDowntime);

            int y = padding;

            // Helper: Draw text inside bar
            void DrawBarLabel(float x, float width, double seconds)
            {
                if (width < 30) return; // Hanya gambar jika bar cukup lebar
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
                // 1. Gambar Nama Mesin (Rata Kiri sesuai Legend)
                string machineName = item.MachineName.Length > 18 ? item.MachineName.Substring(0, 15) + "..." : item.MachineName;
                
                RectangleF nameRect = new RectangleF(padding, y + 5, labelMachineWidth, 30);
                using (var formatName = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near }) // Rata Kiri
                {
                    g.DrawString(machineName, new Font("Segoe UI", 10F), Brushes.Black, nameRect, formatName);
                }

                // 2. Hitung Lebar Bar (Visual Widths)
                float wResponse = (float)(item.ResponseDurationSeconds * scaleFactor);
                float wRepair = (float)(item.RepairDurationSeconds * scaleFactor);
                float wPart = (float)(item.PartWaitDurationSeconds * scaleFactor);
                float wOp = (float)(item.OperatorWaitDurationSeconds * scaleFactor);

                // Terapkan MinBarWidth (hanya jika ada nilainya)
                if (item.ResponseDurationSeconds > 0 && wResponse < minBarWidth) wResponse = minBarWidth;
                if (item.RepairDurationSeconds > 0 && wRepair < minBarWidth) wRepair = minBarWidth;
                if (item.PartWaitDurationSeconds > 0 && wPart < minBarWidth) wPart = minBarWidth;
                if (item.OperatorWaitDurationSeconds > 0 && wOp < minBarWidth) wOp = minBarWidth;

                // Total lebar visual yang akan digambar
                float totalVisualWidth = wResponse + wRepair + wPart + wOp;

                // 3. Scaling Down jika MinBarWidth membuat total melebihi batas area
                // Ini mencegah bar menabrak teks di kanan
                if (totalVisualWidth > maxAvailableBarWidth)
                {
                    float reductionRatio = maxAvailableBarWidth / totalVisualWidth;
                    wResponse *= reductionRatio;
                    wRepair *= reductionRatio;
                    wPart *= reductionRatio;
                    wOp *= reductionRatio;
                }

                float currentX = chartStartX;

                // 4. Gambar Bar Segments
                if (wResponse > 0)
                {
                    using (var brush = new SolidBrush(AppColors.Danger)) g.FillRectangle(brush, currentX, y, wResponse, 25);
                    DrawBarLabel(currentX, wResponse, item.ResponseDurationSeconds);
                    currentX += wResponse;
                }

                if (wRepair > 0)
                {
                    using (var brush = new SolidBrush(AppColors.Warning)) g.FillRectangle(brush, currentX, y, wRepair, 25);
                    DrawBarLabel(currentX, wRepair, item.RepairDurationSeconds);
                    currentX += wRepair;
                }

                if (wPart > 0)
                {
                    using (var brush = new SolidBrush(AppColors.Success)) g.FillRectangle(brush, currentX, y, wPart, 25);
                    DrawBarLabel(currentX, wPart, item.PartWaitDurationSeconds);
                    currentX += wPart;
                }

                if (wOp > 0)
                {
                    using (var brush = new SolidBrush(AppColors.Primary)) g.FillRectangle(brush, currentX, y, wOp, 25);
                    DrawBarLabel(currentX, wOp, item.OperatorWaitDurationSeconds);
                    currentX += wOp;
                }

                // 5. Gambar Teks Total Downtime (Di kolom kanan yang aman)
                TimeSpan totalTime = TimeSpan.FromSeconds(item.TotalDowntimeSeconds);
                string totalStr = $"{(int)totalTime.TotalHours}h {totalTime.Minutes}m";

                // Menggunakan textStartX yang sudah kita kunci di awal
                RectangleF textRect = new RectangleF(textStartX, y, textTotalWidth, 25);

                using (var brush = new SolidBrush(Color.Black))
                using (var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(totalStr, new Font("Segoe UI", 9F, FontStyle.Bold), brush, textRect, format);
                }

                y += rowHeight;
            }

            // Adjust panel height if scrolling needed
            chartPanel.AutoScrollMinSize = new Size(0, y + padding);
        }
    }
}