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
        private Panel pnlStats;
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
            this.Size = new Size(1100, 700);
            this.BackColor = AppColors.Surface;

            // 1. Header & Stats Section
            pnlStats = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                Padding = new Padding(20),
                BackColor = AppColors.CardBackground
            };
            
            // Create Cards
            cardTotal = CreateMetricCard("Total Users", AppColors.Primary);
            cardMachines = CreateMetricCard("Total Machines", AppColors.Info);
            cardOpen = CreateMetricCard("Open Tickets", AppColors.Danger);
            cardValidate = CreateMetricCard("Need Validation", AppColors.Warning);

            // Layout Cards 
            cardTotal.Location = new Point(20, 20);
            cardMachines.Location = new Point(260, 20);
            cardOpen.Location = new Point(500, 20);
            cardValidate.Location = new Point(740, 20);

            pnlStats.Controls.AddRange(new Control[] { cardTotal, cardMachines, cardOpen, cardValidate });

            // Last Update Label
            lblLastUpdate = new AppLabel
            {
                Type = AppLabel.LabelType.Caption,
                Text = "Data loaded: -",
                Location = new Point(980, 20),
                AutoSize = true
            };
            pnlStats.Controls.Add(lblLastUpdate);

            // 2. DataGridView Section
            gridTickets = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.CardBackground,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false 
            };

            // ════ PERBAIKAN: SESUAIKAN DENGAN NAMA KOLOM SQL VIEW YANG BARU ════
            
            // Ini biasanya didapat dari JOIN dengan tabel status (pastikan nama aliasnya benar di Repository Anda)
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status Terkini", FillWeight = 80 });
            
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mesin", HeaderText = "Mesin", DataPropertyName = "Nama Mesin" });
            
            // Kolom Problem sekarang memanggil "Deskripsi Detail" agar aplikator tidak ikut tercetak
            var colMasalah = new DataGridViewTextBoxColumn 
            { 
                Name = "Problem", 
                HeaderText = "Problem", 
                DataPropertyName = "Deskripsi Detail", // Telah diubah
                FillWeight = 200 
            };
            colMasalah.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            gridTickets.Columns.Add(colMasalah);

            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Teknisi", HeaderText = "Teknisi", DataPropertyName = "Nama Teknisi" });
            
            // KPI Waktu (Telah disesuaikan)
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total Downtime", HeaderText = "Total Downtime", DataPropertyName = "Total Downtime" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Durasi Respon", HeaderText = "Durasi Respon", DataPropertyName = "Tunggu Teknisi" }); // Diubah
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Durasi Perbaikan", HeaderText = "Durasi Perbaikan", DataPropertyName = "Durasi Perbaikan" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Waktu Tunggu Part", HeaderText = "Tunggu Part", DataPropertyName = "Tunggu Part" }); // Diubah
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Waktu Tunggu Operator", HeaderText = "Tunggu Operator", DataPropertyName = "Tunggu Operator" }); // Diubah
            
            // ══════════════════════════════════════════════════════════════════════

            // Action Button Column
            var btnCol = new DataGridViewButtonColumn
            {
                Name = "Detail",
                HeaderText = "Aksi",
                Text = "Lihat",
                UseColumnTextForButtonValue = true,
                FillWeight = 60
            };
            gridTickets.Columns.Add(btnCol);

            // Hidden Columns for Detail Popup 
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "No Tiket", DataPropertyName = "ID Tiket", Visible = false }); // Diubah
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Operator", DataPropertyName = "Nama Operator", Visible = false }); // Diubah
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Waktu Lapor", DataPropertyName = "Waktu Lapor", Visible = false });

            // Grid Styling
            gridTickets.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            gridTickets.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.TextSecondary;
            gridTickets.ColumnHeadersDefaultCellStyle.Font = AppFonts.BodySmall; 
            gridTickets.ColumnHeadersDefaultCellStyle.Padding = new Padding(10);
            gridTickets.ColumnHeadersHeight = 40;

            gridTickets.DefaultCellStyle.SelectionBackColor = AppColors.PrimaryLight;
            gridTickets.DefaultCellStyle.SelectionForeColor = AppColors.TextPrimary;
            gridTickets.DefaultCellStyle.Padding = new Padding(10);
            gridTickets.RowTemplate.Height = 50;
            gridTickets.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            
            gridTickets.CellFormatting += GridTickets_CellFormatting;
            gridTickets.CellContentClick += GridTickets_CellContentClick;
            gridTickets.CellPainting += GridTickets_CellPainting;

            this.Controls.Add(gridTickets);
            this.Controls.Add(pnlStats);
        }

        private void GridTickets_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && gridTickets.Columns[e.ColumnIndex].Name == "Detail")
            {
                e.PaintBackground(e.CellBounds, true);

                int btnHeight = 28;
                int paddingX = 10;
                
                int btnWidth = e.CellBounds.Width - (paddingX * 2);
                int btnY = e.CellBounds.Y + (e.CellBounds.Height - btnHeight) / 2;
                int btnX = e.CellBounds.X + paddingX;

                Rectangle btnRect = new Rectangle(btnX, btnY, btnWidth, btnHeight);

                ButtonRenderer.DrawButton(e.Graphics, btnRect, System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                TextRenderer.DrawText(e.Graphics, "Lihat", e.CellStyle.Font, btnRect, SystemColors.ControlText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

        private void GridTickets_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridTickets.Columns[e.ColumnIndex].Name == "Detail")
            {
                var row = gridTickets.Rows[e.RowIndex];
                
                string waitPart = row.Cells["Waktu Tunggu Part"].Value?.ToString() ?? "-";

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
                    $"WAKTU TUNGGU OPERATOR: {row.Cells["Waktu Tunggu Operator"].Value}\n" +
                    $"TOTAL DOWNTIME: {row.Cells["Total Downtime"].Value}";

                MessageBox.Show(detailMsg, "Detail Tiket", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private MetricCard CreateMetricCard(string title, Color accent)
        {
            return new MetricCard
            {
                Title = title,
                Value = "-",
                AccentColor = accent
            };
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
                
                lblLastUpdate.Text = $"Last update: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                // Silenced error to avoid spam, but useful for debug
                // MessageBox.Show($"Error UI Monitoring: {ex.Message}");
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void GridTickets_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = gridTickets.Columns[e.ColumnIndex].Name;

            // [FIX] Multiline Problem Display with Numbering
            if (colName == "Problem" && e.Value != null)
            {
                string raw = e.Value.ToString();
                // SQL View is now using '\n' as separator, so we split by it
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
            
            // Status Column Formatting
            if (colName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString();
                
                if (status.Contains("Open") || status.Contains("Pending") || status.Contains("Belum")) 
                    e.CellStyle.ForeColor = AppColors.Danger;
                else if (status.Contains("Proses") || status.Contains("Repair")) 
                    e.CellStyle.ForeColor = AppColors.Warning;
                else if (status.Contains("Selesai") || status.Contains("Done")) 
                    e.CellStyle.ForeColor = AppColors.Success;
                else
                    e.CellStyle.ForeColor = AppColors.TextPrimary;

                e.CellStyle.Font = new Font(gridTickets.DefaultCellStyle.Font, FontStyle.Bold);
            }
        }
    }
}