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

        // Constructor for Designer (if needed) or Parameterless fallback
        // But for DI we need constructor with param. 
        // WinForms Designer creates restrictions. Usually we need parameterless.
        // But since we instantiate this manually in AdminMainForm, param ctor is fine.
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
                BackColor = Color.White
            };
            
            // Create Cards
            cardTotal = CreateMetricCard("Total Users", AppColors.Primary);
            cardMachines = CreateMetricCard("Total Machines", AppColors.Info);
            cardOpen = CreateMetricCard("Open Tickets", AppColors.Danger);
            cardValidate = CreateMetricCard("Need Validation", AppColors.Warning);

            // Layout Cards (Simple Flow or Manual Position)
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
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false // Disable auto-generation to prevent duplicates
            };

            // Manual Column Definition - MUST MATCH 'view_admin_report' aliases
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status Terkini", FillWeight = 80 });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mesin", HeaderText = "Mesin", DataPropertyName = "Nama Mesin" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Masalah", HeaderText = "Masalah", DataPropertyName = "Detail Masalah", FillWeight = 200 });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Teknisi", HeaderText = "Teknisi", DataPropertyName = "Nama Teknisi" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total Downtime", HeaderText = "Total Downtime", DataPropertyName = "Total Downtime" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Durasi Respon", HeaderText = "Durasi Respon", DataPropertyName = "Durasi Respon" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Durasi Perbaikan", HeaderText = "Durasi Perbaikan", DataPropertyName = "Durasi Perbaikan" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Waktu Tunggu Part", HeaderText = "Tunggu Part", DataPropertyName = "Waktu Tunggu Part" });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Waktu Tunggu Operator", HeaderText = "Tunggu Operator", DataPropertyName = "Durasi Trial Run" });
            
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

            // Hidden Columns for Detail Popup - MUST MATCH view_admin_report aliases
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "No Tiket", DataPropertyName = "No Tiket", Visible = false });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Operator", DataPropertyName = "Operator Pelapor", Visible = false });
            gridTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Waktu Lapor", DataPropertyName = "Waktu Lapor", Visible = false });

            // Grid Styling
            gridTickets.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            gridTickets.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.TextSecondary;
            gridTickets.ColumnHeadersDefaultCellStyle.Font = AppFonts.BodySmall; // or Bold
            gridTickets.ColumnHeadersDefaultCellStyle.Padding = new Padding(10);
            gridTickets.ColumnHeadersHeight = 40;

            gridTickets.DefaultCellStyle.SelectionBackColor = AppColors.PrimaryLight;
            gridTickets.DefaultCellStyle.SelectionForeColor = AppColors.TextPrimary;
            gridTickets.DefaultCellStyle.Padding = new Padding(10);
            gridTickets.RowTemplate.Height = 50;
            
            // Event for Formatting Status
            gridTickets.CellFormatting += GridTickets_CellFormatting;
            
            // Event for Button Click
            gridTickets.CellContentClick += GridTickets_CellContentClick;

            this.Controls.Add(gridTickets);
            this.Controls.Add(pnlStats);
        }

        private void GridTickets_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Check if Detail button clicked
            if (e.RowIndex >= 0 && gridTickets.Columns[e.ColumnIndex].Name == "Detail")
            {
                var row = gridTickets.Rows[e.RowIndex];
                
                string waitPart = row.Cells["Waktu Tunggu Part"].Value?.ToString() ?? "-";

                string detailMsg = 
                    $"No Tiket: {row.Cells["No Tiket"].Value}\n" +
                    $"Status: {row.Cells["Status"].Value}\n\n" +
                    $"Mesin: {row.Cells["Mesin"].Value}\n" +
                    $"Masalah: {row.Cells["Masalah"].Value}\n" +
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
            _timerRefresh = new Timer { Interval = 15000 }; // 15s
            _timerRefresh.Tick += async (s, e) => await LoadDataAsync();
        }

        // Public method to be called when view is shown
        public async void OnViewLoad()
        {
            await LoadDataAsync();
            _timerRefresh.Start();
        }

        public void OnViewUnload()
        {
            _timerRefresh.Stop();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Optional: Show loading indicator if initial load
                if (gridTickets.Rows.Count == 0) this.Cursor = Cursors.WaitCursor;

                // 1. Fetch Stats
                var stats = await _repository.GetSummaryStatsAsync();
                if (stats != null)
                {
                    cardTotal.Value = stats.TotalUsers.ToString();
                    cardMachines.Value = stats.TotalMachines.ToString();
                    cardOpen.Value = stats.OpenTickets.ToString();
                    cardValidate.Value = stats.NeedValidation.ToString();
                }

                // 2. Fetch Monitoring List
                var data = await _repository.GetMonitoringDataAsync();
                
                // Simple rebind (Columns are manually defined now)
                gridTickets.DataSource = data;
                
                // Update Timestamp
                lblLastUpdate.Text = $"Last update: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                // Show error to help debug why columns aren't hiding
                MessageBox.Show($"Error UI Monitoring: {ex.Message}");
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
            
            // Status Column Formatting
            if (colName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString();
                
                // Simpler logic than Utils if we don't have ID, matching text for now.
                if (status.Contains("Open") || status.Contains("Pending")) 
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
