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

            // === Header Panel (Row 0) ===
            headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };
            headerPanel.Paint += (s, e) =>
            {
                // Bottom border
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230)), 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            // Title
            lblTitle = new Label
            {
                Text = "Leaderboard Teknisi",
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                Location = new Point(20, 15),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            // Stats Control (Shop-wide totals)
            // NOTE: This shows aggregate stats for ALL technicians, not the logged-in user
            statsControl = new TechnicianStatsControl
            {
                Location = new Point(20, 45),
                Size = new Size(940, 100), // Must be at least 900x100 for 3 cards
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(statsControl);

            // Filter Controls Row
            var filterRow = new Panel
            {
                Location = new Point(20, 180),  // Moved down to accommodate taller stats
                Size = new Size(650, 40),
                BackColor = Color.Transparent
            };

            var lblMetric = new Label
            {
                Text = "Metrik:",
                Font = AppFonts.Title,
                Location = new Point(0, 7),
                AutoSize = true
            };
            filterRow.Controls.Add(lblMetric);

            cmbMetric = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = AppFonts.Title,
                Location = new Point(70, 3),
                Size = new Size(200, 28)
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
            filterRow.Controls.Add(cmbMetric);

            btnSort = new Button
            {
                Text = "↓ Tertinggi",
                Font = AppFonts.Body,
                Location = new Point(300, 2),
                Size = new Size(120, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = AppColors.TextPrimary
            };
            btnSort.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnSort.Click += (s, e) =>
            {
                _sortAscending = !_sortAscending;
                btnSort.Text = _sortAscending ? "↑ Terendah" : "↓ Tertinggi";
                SortAndRenderChart();
            };
            filterRow.Controls.Add(btnSort);

            headerPanel.Controls.Add(filterRow);
            headerPanel.Height = 230; // Increased height for header

            mainLayout.Controls.Add(headerPanel, 0, 0);

            // === Chart Panel (Row 1) ===
            chartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            chartPanel.Paint += ChartPanel_Paint;

            lblNoData = new Label
            {
                Text = "Tidak ada data leaderboard.",
                Font = AppFonts.Body,
                ForeColor = AppColors.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            chartPanel.Controls.Add(lblNoData);

            mainLayout.Controls.Add(chartPanel, 0, 1);

            this.Controls.Add(mainLayout);
        }

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
            g.Clear(Color.White);

            int padding = 20;
            int labelWidth = 120; // Space for technician names
            int chartLeft = labelWidth + padding;
            int chartTop = padding;
            int chartWidth = chartPanel.Width - chartLeft - padding - 60; // Space for value labels
            int chartHeight = chartPanel.Height - padding * 2;

            if (chartWidth < 100 || chartHeight < 50) return;

            // Get max value for current metric
            double maxValue = GetMaxValue();
            if (maxValue == 0) maxValue = 1;

            int barCount = Math.Min(_leaderboardData.Count, 10); // Top 10
            int barHeight = Math.Min(35, (chartHeight - 10) / barCount);
            int gap = 8;

            // Draw bars
            int y = chartTop;
            for (int i = 0; i < barCount; i++)
            {
                var item = _leaderboardData[i];
                double value = GetMetricValue(item);

                // Rank number
                using (var font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
                using (var brush = new SolidBrush(AppColors.TextSecondary))
                {
                    g.DrawString($"#{i + 1}", font, brush, 10, y + (barHeight - gap) / 2 - 8);
                }

                // Technician Name
                string name = item.TechnicianName ?? "Unknown";
                if (name.Length > 15) name = name.Substring(0, 12) + "...";
                
                using (var font = new Font("Segoe UI", 9F))
                using (var brush = new SolidBrush(AppColors.TextPrimary))
                {
                    g.DrawString(name, font, brush, 40, y + (barHeight - gap) / 2 - 7);
                }

                // Bar
                int barWidth = (int)((value / maxValue) * chartWidth);
                if (barWidth < 5) barWidth = 5;

                Color barColor = GetBarColor(i);
                using (var brush = new SolidBrush(barColor))
                {
                    var barRect = new Rectangle(chartLeft, y, barWidth, barHeight - gap);
                    using (var path = GetRoundedRect(barRect, 4))
                    {
                        g.FillPath(brush, path);
                    }
                }

                // Value Label
                string valueText = GetFormattedValue(value);
                using (var font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold))
                using (var brush = new SolidBrush(AppColors.TextPrimary))
                {
                    g.DrawString(valueText, font, brush, chartLeft + barWidth + 8, y + (barHeight - gap) / 2 - 7);
                }

                y += barHeight;
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
