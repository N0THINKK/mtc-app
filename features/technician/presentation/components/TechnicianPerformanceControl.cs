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
        private Button btnSort;
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
                    statsControl.UpdateStats(totalRepairs, (decimal)avgRating, totalStars);
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

            // Title
            lblTitle = new Label
            {
                Text = "Leaderboard Teknisi",
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, AppDimens.MarginSmall)
            };
            flowVertical.Controls.Add(lblTitle);

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

            btnSort = new Button
            {
                Text = "↓ Tertinggi",
                Font = AppFonts.Body,
                Size = new Size(120, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = AppColors.TextPrimary,
                Margin = new Padding(0, 2, 0, 0)
            };
            btnSort.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnSort.Click += (s, e) =>
            {
                _sortAscending = !_sortAscending;
                btnSort.Text = _sortAscending ? "↑ Terendah" : "↓ Tertinggi";
                SortAndRenderChart();
            };
            flowFilterRow.Controls.Add(btnSort);

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
                Padding = new Padding(AppDimens.MarginLarge)
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
        // Chart Rendering (Logic Modified for Vertical Layout)
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

            // Layout Parameters for Vertical Chart
            int padding = 20;
            int bottomLabelHeight = 60; // Space for Rank and Name at bottom
            int topValueHeight = 25;    // Space for Value label at top
            
            int chartLeft = padding;
            int chartRight = chartPanel.Width - padding;
            int chartTop = padding + topValueHeight;
            int chartBottom = chartPanel.Height - padding - bottomLabelHeight;
            
            int chartWidth = chartRight - chartLeft;
            int chartHeight = chartBottom - chartTop;

            if (chartWidth < 100 || chartHeight < 50) return;

            // Get max value for current metric
            double maxValue = GetMaxValue();
            if (maxValue == 0) maxValue = 1;

            int barCount = Math.Min(_leaderboardData.Count, 10); // Top 10
            int gap = 20; // Gap between vertical bars
            
            // Calculate bar width
            int barWidth = (chartWidth - (gap * (barCount - 1))) / barCount;
            
            // Constrain bar width to look good
            if (barWidth < 15) barWidth = 15; // Minimum width
            if (barWidth > 80) barWidth = 80; // Maximum width cap
            
            // Re-calculate start X to center the chart group
            int totalContentWidth = (barWidth * barCount) + (gap * (barCount - 1));
            int startX = chartLeft + (chartWidth - totalContentWidth) / 2;

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

                    // 2. Draw Value Label (Above Bar)
                    string valueText = GetFormattedValue(value);
                    using (var font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold))
                    using (var brush = new SolidBrush(AppColors.TextPrimary))
                    {
                        // Position slightly above the bar top
                        g.DrawString(valueText, font, brush, centerX, y - 18, centerFormat);
                    }

                    // 3. Draw Rank (Below Bar)
                    using (var font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
                    using (var brush = new SolidBrush(AppColors.TextSecondary))
                    {
                        g.DrawString($"#{i + 1}", font, brush, centerX, chartBottom + 8, centerFormat);
                    }

                    // 4. Draw Name (Below Rank)
                    string name = item.TechnicianName ?? "Unknown";
                    // Simple truncate if name is too long for the bar width
                    int maxNameChars = Math.Max(5, barWidth / 7); // Approx char width
                    if (name.Length > maxNameChars) name = name.Substring(0, maxNameChars - 2) + "..";
                    
                    using (var font = new Font("Segoe UI", 9F))
                    using (var brush = new SolidBrush(AppColors.TextPrimary))
                    {
                        g.DrawString(name, font, brush, centerX, chartBottom + 28, centerFormat);
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

        private Color GetBarColor(int rank)
        {
            // Gold, Silver, Bronze for top 3, then primary color
            return rank switch
            {
                0 => Color.FromArgb(255, 193, 7),   // Gold
                1 => Color.FromArgb(158, 158, 158), // Silver
                2 => Color.FromArgb(205, 127, 50),  // Bronze
                _ => AppColors.Primary
            };
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