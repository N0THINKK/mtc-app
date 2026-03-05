using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.admin.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.features.admin.presentation.views
{
    public class MonitoringView : UserControl
    {
        private readonly IAdminRepository _repository;
        private DataGridView gridTickets;
        private Timer _timerRefresh;
        private TableLayoutPanel tlpStats;
        private MetricCard cardTotal;
        private MetricCard cardMachines;
        private MetricCard cardOpen;
        private MetricCard cardValidate;
        private AppLabel lblLastUpdate;

        public MonitoringView(IAdminRepository repository)
        {
            _repository = repository;
            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeComponent()
        {
            // 1. TAHAN RENDER UI SAMPAI SEMUA KOMPONEN SIAP
            this.SuspendLayout();
            
            this.Size = new Size(1100, 700);
            this.BackColor = AppColors.Surface; 
            this.Padding = new Padding(24); 

            // ==========================================
            // STATS CARDS SECTION (TOP)
            // ==========================================
            tlpStats = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 140,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 24) 
            };
            
            for (int i = 0; i < 4; i++)
                tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            cardTotal = CreateMetricCard("Total Users", AppColors.Primary);
            cardMachines = CreateMetricCard("Total Machines", AppColors.Info);
            cardOpen = CreateMetricCard("Open Tickets", AppColors.Danger);
            cardValidate = CreateMetricCard("Need Validation", AppColors.Warning);

            cardTotal.Margin = new Padding(0, 0, 16, 0);
            cardMachines.Margin = new Padding(0, 0, 16, 0);
            cardOpen.Margin = new Padding(0, 0, 16, 0);
            cardValidate.Margin = new Padding(0, 0, 0, 0); 

            tlpStats.Controls.Add(cardTotal, 0, 0);
            tlpStats.Controls.Add(cardMachines, 1, 0);
            tlpStats.Controls.Add(cardOpen, 2, 0);
            tlpStats.Controls.Add(cardValidate, 3, 0);

            // ==========================================
            // GRID CONTAINER (CARD WRAPPER)
            // ==========================================
            AppCard cardGridContainer = new AppCard
            {
                Dock = DockStyle.Fill,
                ShowShadow = true,
                CornerRadius = 16,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(20) 
            };

            Panel pnlGridHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.Transparent
            };

            AppLabel lblGridTitle = new AppLabel
            {
                Text = "Live Machine Monitoring",
                Font = AppFonts.Header3,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 5)
            };

            lblLastUpdate = new AppLabel
            {
                Type = AppLabel.LabelType.Caption,
                Text = "Data loaded: -",
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 10, 0, 0)
            };

            pnlGridHeader.Controls.Add(lblGridTitle);
            pnlGridHeader.Controls.Add(lblLastUpdate);

            // ==========================================
            // DATAGRIDVIEW
            // ==========================================
            gridTickets = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.CardBackground, 
                BorderStyle = BorderStyle.None, 
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, 
                GridColor = Color.FromArgb(238, 242, 246),
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false,
                Margin = new Padding(0, 16, 0, 0)
            };

            // Definisi Kolom
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", DataPropertyName = "Status Terkini", FillWeight = 90 });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mesin", HeaderText = "MESIN", DataPropertyName = "Nama Mesin", FillWeight = 110 });
            
            var colMasalah = new DataGridViewTextBoxColumn 
            { 
                Name = "Problem", HeaderText = "PROBLEM", DataPropertyName = "Deskripsi Detail", FillWeight = 220 
            };
            colMasalah.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            gridTickets.Columns.Add(colMasalah);

            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Teknisi", HeaderText = "TEKNISI", DataPropertyName = "Nama Teknisi" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total Downtime", HeaderText = "TOTAL DOWNTIME", DataPropertyName = "Total Downtime" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Durasi Respon", HeaderText = "RESPON", DataPropertyName = "Tunggu Teknisi" }); 
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Durasi Perbaikan", HeaderText = "PERBAIKAN", DataPropertyName = "Durasi Perbaikan" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Waktu Tunggu Part", HeaderText = "TUNGGU PART", DataPropertyName = "Tunggu Part" }); 
            
            var btnCol = new DataGridViewButtonColumn
            {
                Name = "Detail",
                HeaderText = "AKSI",
                Text = "Lihat",
                UseColumnTextForButtonValue = true,
                FillWeight = 70
            };
            gridTickets.Columns.Add(btnCol);

            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "No Tiket", DataPropertyName = "ID Tiket", Visible = false });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Operator", DataPropertyName = "Nama Operator", Visible = false });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Waktu Lapor", DataPropertyName = "Waktu Lapor", Visible = false });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Waktu Tunggu Operator", DataPropertyName = "Tunggu Operator", Visible = false }); 

            gridTickets.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            gridTickets.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139); 
            gridTickets.ColumnHeadersDefaultCellStyle.Font = new Font(AppFonts.FontFamily, 10.5F, FontStyle.Bold); 
            gridTickets.ColumnHeadersDefaultCellStyle.Padding = new Padding(16, 14, 16, 14);
            gridTickets.ColumnHeadersHeight = 50;

            // 1. Matikan efek kotak biru saat header diklik
            gridTickets.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);

            // 2. Hilangkan seleksi otomatis pada baris/sel pertama saat data baru dimuat
            gridTickets.DataBindingComplete += (s, e) => gridTickets.ClearSelection();

            gridTickets.DefaultCellStyle.BackColor = AppColors.CardBackground;
            gridTickets.DefaultCellStyle.ForeColor = AppColors.TextPrimary;

            // ----------------------------------------------------------------------------------
            // PERBAIKAN: Menghilangkan kotak biru saat baris dipilih
            // Set warna seleksi sama dengan warna background kartu (Putih)
            // ----------------------------------------------------------------------------------
            gridTickets.DefaultCellStyle.SelectionBackColor = AppColors.CardBackground; 
            gridTickets.DefaultCellStyle.SelectionForeColor = AppColors.TextPrimary;
            // ----------------------------------------------------------------------------------

            gridTickets.DefaultCellStyle.Font = AppFonts.BodySmall;
            gridTickets.DefaultCellStyle.Padding = new Padding(16, 12, 16, 12);
            gridTickets.RowTemplate.Height = 64; 
            gridTickets.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            
            gridTickets.CellFormatting += GridTickets_CellFormatting;
            gridTickets.CellContentClick += GridTickets_CellContentClick;
            gridTickets.CellPainting += GridTickets_CellPainting;

            cardGridContainer.Controls.Add(gridTickets);
            cardGridContainer.Controls.Add(pnlGridHeader);
            
            // 2. PERBAIKAN URUTAN (Z-ORDER DOCKING)
            this.Controls.Add(cardGridContainer);
            this.Controls.Add(tlpStats);
            
            // Pastikan panel grid membawa diri ke depan (tidak tertimpa tlpStats)
            cardGridContainer.BringToFront();

            // 3. SELESAI RENDER
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private MetricCard CreateMetricCard(string title, Color accent)
        {
            return new MetricCard
            {
                Title = title,
                Value = "-",
                AccentColor = accent,
                Dock = DockStyle.Fill 
            };
        }

        private void GridTickets_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = gridTickets.Columns[e.ColumnIndex].Name;

            if (colName == "Detail")
            {
                e.PaintBackground(e.CellBounds, true);

                int btnHeight = 36;
                int btnWidth = 70;
                int btnY = e.CellBounds.Y + (e.CellBounds.Height - btnHeight) / 2;
                int btnX = e.CellBounds.X + (e.CellBounds.Width - btnWidth) / 2; 

                Rectangle btnRect = new Rectangle(btnX, btnY, btnWidth, btnHeight);

                using (System.Drawing.Drawing2D.GraphicsPath path = GraphicsUtils.GetRoundedRectangle(btnRect, 8))
                {
                    using (Pen pen = new Pen(AppColors.Primary, 1.5f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }

                TextRenderer.DrawText(e.Graphics, "Lihat", new Font(AppFonts.BodySmall, FontStyle.Bold), btnRect, AppColors.Primary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
            
            else if (colName == "Status" && e.Value != null)
            {
                e.PaintBackground(e.CellBounds, true);

                string status = e.Value.ToString();
                
                Color bgColor = Color.FromArgb(241, 245, 249); 
                Color textColor = Color.FromArgb(100, 116, 139);

                if (status.Contains("Open") || status.Contains("Pending") || status.Contains("Belum")) 
                {
                    bgColor = Color.FromArgb(254, 226, 226); 
                    textColor = Color.FromArgb(185, 28, 28); 
                }
                else if (status.Contains("Proses") || status.Contains("Repair")) 
                {
                    bgColor = Color.FromArgb(254, 243, 199); 
                    textColor = Color.FromArgb(180, 83, 9); 
                }
                else if (status.Contains("Selesai") || status.Contains("Done")) 
                {
                    bgColor = Color.FromArgb(220, 252, 231); 
                    textColor = Color.FromArgb(21, 128, 61); 
                }

                int badgeHeight = 30;
                int paddingX = 14;

                SizeF textSize = e.Graphics.MeasureString(status, new Font(AppFonts.BodySmall, FontStyle.Bold));
                int badgeWidth = (int)textSize.Width + (paddingX * 2);

                if (badgeWidth > e.CellBounds.Width - 20) badgeWidth = e.CellBounds.Width - 20;

                int badgeY = e.CellBounds.Y + (e.CellBounds.Height - badgeHeight) / 2;
                int badgeX = e.CellBounds.X + 16; 

                Rectangle badgeRect = new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight);
                int radius = badgeHeight / 2; 

                using (System.Drawing.Drawing2D.GraphicsPath path = GraphicsUtils.GetRoundedRectangle(badgeRect, radius))
                {
                    using (SolidBrush brush = new SolidBrush(bgColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                TextRenderer.DrawText(
                    e.Graphics, 
                    status, 
                    new Font(AppFonts.BodySmall, FontStyle.Bold), 
                    badgeRect, 
                    textColor, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                );

                e.Handled = true;
            }
        }

        private void GridTickets_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = gridTickets.Columns[e.ColumnIndex].Name;

            if (colName == "Problem" && e.Value != null)
            {
                string raw = e.Value.ToString();
                string[] parts = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length > 0)
                {
                    for (int i = 0; i < parts.Length; i++)
                    {
                        parts[i] = $"{i + 1}. {parts[i].Trim()}";
                    }
                    e.Value = string.Join("\n", parts);
                }
                e.CellStyle.WrapMode = DataGridViewTriState.True;
            }
        }

        private void GridTickets_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridTickets.Columns[e.ColumnIndex].Name == "Detail")
            {
                var row = gridTickets.Rows[e.RowIndex];
                
                string waitPart = row.Cells["Waktu Tunggu Part"].Value?.ToString() ?? "-";
                string waitOp = row.Cells["Waktu Tunggu Operator"].Value?.ToString() ?? "-";

                string detailMsg = 
                    $"No Tiket: {row.Cells["No Tiket"].Value}\n" +
                    $"Status: {row.Cells["Status"].Value}\n\n" +
                    $"Mesin: {row.Cells["Mesin"].Value}\n" +
                    $"Problem: {row.Cells["Problem"].Value}\n" +
                    $"Teknisi: {row.Cells["Teknisi"].Value}\n" +
                    $"Operator: {row.Cells["Operator"].Value}\n\n" +
                    $"Waktu Lapor: {row.Cells["Waktu Lapor"].Value}\n" +
                    $"-----------------------------------\n" +
                    $"DURASI RESPON: {row.Cells["Durasi Respon"].Value}\n" +
                    $"DURASI PERBAIKAN: {row.Cells["Durasi Perbaikan"].Value}\n" +
                    $"WAKTU TUNGGU PART: {waitPart}\n" +
                    $"WAKTU TUNGGU OPERATOR: {waitOp}\n" +
                    $"TOTAL DOWNTIME: {row.Cells["Total Downtime"].Value}";

                MessageBox.Show(detailMsg, "Detail Tiket", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void InitializeTimer()
        {
            _timerRefresh = new Timer { Interval = 15000 }; 
            _timerRefresh.Tick += async (s, e) => await LoadDataAsync();
        }

        public async void OnViewLoad()
        {
            await LoadDataAsync();
            _timerRefresh.Start();
        }

        public void OnViewUnload()
        {
            _timerRefresh.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerRefresh?.Stop();
                _timerRefresh?.Dispose();
            }
            base.Dispose(disposing);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (gridTickets.Rows.Count == 0) this.Cursor = Cursors.WaitCursor;

                var stats = await _repository.GetSummaryStatsAsync();
                if (stats != null)
                {
                    cardTotal.Value = stats.TotalUsers.ToString();
                    cardMachines.Value = stats.TotalMachines.ToString();
                    cardOpen.Value = stats.OpenTickets.ToString();
                    cardValidate.Value = stats.NeedValidation.ToString();
                }

                var data = await _repository.GetMonitoringDataAsync();
                gridTickets.DataSource = data;
                
                lblLastUpdate.Text = $"Terakhir diupdate: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data Monitoring:\n\n{ex.Message}", "Kesalahan Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
    }
}