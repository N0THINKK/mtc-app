using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class TechnicianPerformanceControl : UserControl
    {
        private readonly ITechnicianRepository _repository;
        private List<TechnicianPerformanceDto> _leaderboardData = new List<TechnicianPerformanceDto>();
        private string _currentMetric = "repairs"; // repairs, rating, stars
        private bool _sortAscending = false;

        // Layout
        private TableLayoutPanel mainLayout;
        
        // Header Controls
        private Panel headerPanel;
        private TechnicianStatsControl statsControl;
        private ComboBox cmbMetric;
        private ComboBox cmbSort;
        private Label lblTitle;

        // Chart Area
        private Panel chartPanel;
        private Label lblNoData;

        public TechnicianPerformanceControl(ITechnicianRepository repository)
        {
            _repository = repository;
            InitializeComponent();
        }

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            try
            {
                var data = await _repository.GetLeaderboardAsync(start, end);
                _leaderboardData = data?.ToList() ?? new List<TechnicianPerformanceDto>();
                
                // Update shop-wide stats (sum of all technicians)
                if (_leaderboardData.Count > 0)
                {
                    int totalRepairs = _leaderboardData.Sum(t => t.TotalRepairs);
                    double avgRating = _leaderboardData.Average(t => t.AverageRating);
                    int totalStars = _leaderboardData.Sum(t => t.TotalStars);
                    statsControl.UpdateStats(totalRepairs, (decimal)avgRating);
                }

                SortAndRenderChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data leaderboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========================================================
        // UI Construction
        // ========================================================
        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(248, 250, 252);

            // Main Layout: 2 rows (Header AutoSize, Chart Fill)
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Chart

            headerPanel = BuildHeaderPanel();
            mainLayout.Controls.Add(headerPanel, 0, 0);

            chartPanel = BuildChartPanel();
            mainLayout.Controls.Add(chartPanel, 0, 1);

            this.Controls.Add(mainLayout);

            // Title — added LAST so it docks at the very top (WinForms dock order)
            lblTitle = new Label
            {
                Text = "Leaderboard Teknisi",
                Font = AppFonts.PageTitle,
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 40
            };
            this.Controls.Add(lblTitle);
        }

        private Panel BuildHeaderPanel()
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(AppDimens.MarginLarge, AppDimens.PaddingStandard, AppDimens.MarginLarge, AppDimens.PaddingStandard)
            };
            header.Paint += (s, e) =>
            {
                // Bottom border
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230)), 0, header.Height - 1, header.Width, header.Height - 1);
            };

            // Use a vertical FlowLayoutPanel to stack title, stats, and filter row
            var flowVertical = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // Stats Control (Shop-wide totals)
            statsControl = new TechnicianStatsControl
            {
                Size = new Size(940, 100),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, AppDimens.MarginLarge)
            };
            flowVertical.Controls.Add(statsControl);

            // Filter Controls Row
            var flowFilterRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            var lblMetric = new Label
            {
                Text = "Metrik:",
                Font = AppFonts.Title,
                AutoSize = true,
                Margin = new Padding(0, 7, AppDimens.MarginSmall, 0)
            };
            flowFilterRow.Controls.Add(lblMetric);

            cmbMetric = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = AppFonts.Title,
                Size = new Size(200, 28),
                Margin = new Padding(0, 3, AppDimens.MarginLarge, 0)
            };
            cmbMetric.Items.AddRange(new object[] { "Jumlah Perbaikan", "Rata-rata Rating", "Total Bintang" });
            cmbMetric.SelectedIndex = 0;
            cmbMetric.SelectedIndexChanged += (s, e) =>
            {
                switch (cmbMetric.SelectedIndex)
                {
                    case 1: _currentMetric = "rating"; break;
                    case 2: _currentMetric = "stars"; break;
                    default: _currentMetric = "repairs"; break;
                }
                SortAndRenderChart();
            };
            flowFilterRow.Controls.Add(cmbMetric);

            var lblSort = new Label
            {
                Text = "Urutkan:",
                Font = AppFonts.Title,
                AutoSize = true,
                Margin = new Padding(AppDimens.MarginLarge, 7, AppDimens.MarginSmall, 0)
            };
            flowFilterRow.Controls.Add(lblSort);

            cmbSort = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = AppFonts.Title,
                Size = new Size(130, 28),
                Margin = new Padding(0, 3, 0, 0)
            };
            cmbSort.Items.AddRange(new object[] { "↓ Tertinggi", "↑ Terendah" });
            cmbSort.SelectedIndex = 0;
            cmbSort.SelectedIndexChanged += (s, e) =>
            {
                _sortAscending = cmbSort.SelectedIndex == 1;
                SortAndRenderChart();
            };
            flowFilterRow.Controls.Add(cmbSort);

            flowVertical.Controls.Add(flowFilterRow);

            header.Controls.Add(flowVertical);

            return header;
        }

        private Panel BuildChartPanel()
        {
            var chart = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(AppDimens.MarginLarge),
                AutoScroll = true // MENCEGAH TEKS TABRAKAN SAAT RESIZE
            };
            chart.Paint += ChartPanel_Paint;

            lblNoData = new Label
            {
                Text = "Tidak ada data leaderboard.",
                Font = AppFonts.Body,
                ForeColor = AppColors.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            chart.Controls.Add(lblNoData);

            return chart;
        }

        // ========================================================
        // Chart Rendering
        // ========================================================
        private void SortAndRenderChart()
        {
            if (_leaderboardData.Count == 0)
            {
                lblNoData.Visible = true;
                chartPanel.Invalidate();
                return;
            }

            lblNoData.Visible = false;

            // Sort based on current metric
            switch (_currentMetric)
            {
                case "rating":
                    _leaderboardData = _sortAscending
                        ? _leaderboardData.OrderBy(t => t.AverageRating).ToList()
                        : _leaderboardData.OrderByDescending(t => t.AverageRating).ToList();
                    break;
                case "stars":
                    _leaderboardData = _sortAscending
                        ? _leaderboardData.OrderBy(t => t.TotalStars).ToList()
                        : _leaderboardData.OrderByDescending(t => t.TotalStars).ToList();
                    break;
                default: // repairs
                    _leaderboardData = _sortAscending
                        ? _leaderboardData.OrderBy(t => t.TotalRepairs).ToList()
                        : _leaderboardData.OrderByDescending(t => t.TotalRepairs).ToList();
                    break;
            }

            chartPanel.Invalidate();
        }

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            if (_leaderboardData.Count == 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(AppColors.CardBackground);
            
            // Translasi untuk AutoScroll
            g.TranslateTransform(chartPanel.AutoScrollPosition.X, chartPanel.AutoScrollPosition.Y);

            // Layout Parameters for Vertical Chart
            int padding = 20;
            int bottomLabelHeight = 70; // Adjusted space for vertical names at bottom
            int topValueHeight = 40;    

            int chartBottom = chartPanel.Height - padding - bottomLabelHeight;
            int chartHeight = chartBottom - (padding + topValueHeight);

            if (chartHeight < 50) return;

            // Get max value for current metric
            double maxValue = GetMaxValue();
            if (maxValue == 0) maxValue = 1;

            int barCount = Math.Min(_leaderboardData.Count, 10); // Top 10
            
            // LEBAR DAN JARAK BAR DIBUAT TETAP
            int gap = 45; 
            int barWidth = 65;

            int totalBarsWidth = (barWidth * barCount) + (gap * (barCount - 1));
            chartPanel.AutoScrollMinSize = new Size(totalBarsWidth + (padding * 2), 0);
            
            // Tengahkan posisi X jika ruang layar masih lebih lebar dari konten
            int startX = padding;
            if (chartPanel.ClientSize.Width > totalBarsWidth + (padding * 2))
            {
                startX = (chartPanel.ClientSize.Width - totalBarsWidth) / 2;
            }

            // Draw bars
            for (int i = 0; i < barCount; i++)
            {
                var item = _leaderboardData[i];
                double value = GetMetricValue(item);
                
                int x = startX + i * (barWidth + gap);
                
                // Calculate Height relative to max value
                int barHeight = (int)((value / maxValue) * chartHeight);
                if (barHeight < 5) barHeight = 5; // Minimum visual height for 0 or low values
                
                int y = chartBottom - barHeight;

                // 1. Draw Bar
                Color barColor = GetBarColor(i);
                using (var brush = new SolidBrush(barColor))
                {
                    var barRect = new Rectangle(x, y, barWidth, barHeight);
                    using (var path = GetRoundedRect(barRect, 4))
                    {
                        g.FillPath(brush, path);
                    }
                }

                // Prepare string format for centering text
                using (var centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
                {
                    float centerX = x + barWidth / 2f;

                    // 2. Draw Value Label (Above Bar) - FONT DIPERBESAR (12F)
                    string valueText = GetFormattedValue(value);
                    using (var font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold))
                    using (var brush = new SolidBrush(AppColors.TextPrimary))
                    {
                        // Posisi ditarik ke atas sedikit agar tidak nabrak (y - 25)
                        g.DrawString(valueText, font, brush, centerX, y - 25, centerFormat);
                    }

                    // 3. Draw Rank (Below Bar)
                    using (var font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
                    using (var brush = new SolidBrush(AppColors.TextSecondary))
                    {
                        g.DrawString($"#{i + 1}", font, brush, centerX, chartBottom + 8, centerFormat);
                    }

                    // 4. Draw Initials (Below Rank, Vertical)
                    string initials = !string.IsNullOrWhiteSpace(item.Nik) ? item.Nik : GetInitials(item.TechnicianName);
                    
                    using (var font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
                    using (var brush = new SolidBrush(AppColors.TextPrimary))
                    {
                        var state = g.Save();
                        g.TranslateTransform(centerX, chartBottom + 28);
                        g.RotateTransform(-90);
                        using (var verticalFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString(initials, font, brush, 0, 0, verticalFormat);
                        }
                        g.Restore(state);
                    }
                }
            }
        }

        private double GetMaxValue()
        {
            return _currentMetric switch
            {
                "rating" => _leaderboardData.Max(t => t.AverageRating),
                "stars" => _leaderboardData.Max(t => t.TotalStars),
                _ => _leaderboardData.Max(t => t.TotalRepairs)
            };
        }

        private double GetMetricValue(TechnicianPerformanceDto item)
        {
            return _currentMetric switch
            {
                "rating" => item.AverageRating,
                "stars" => item.TotalStars,
                _ => item.TotalRepairs
            };
        }

        private string GetFormattedValue(double value)
        {
            return _currentMetric switch
            {
                "rating" => $"{value:F1} ⭐",
                "stars" => $"{value:F0} ⭐",
                _ => $"{value:F0}"
            };
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var words = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(words.Select(w => char.ToUpper(w[0])));
        }

        private Color GetBarColor(int rank)
        {
            // Performa disamakan jadi warna kuning emas semua
            return Color.FromArgb(255, 193, 7);
        }

        private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            chartPanel?.Invalidate();
        }
    }
}