using System;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using ClosedXML.Excel;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.infrastructure; // Pastikan namespace untuk DatabaseHelper
using mtc_app.features.admin.services;

namespace mtc_app.features.admin.presentation.views
{
    public partial class ReportView : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblTitle, lblDateStart, lblDateEnd, lblArea;
        private DateTimePicker dateStart, dateEnd;
        private ComboBox cmbArea;
        private CheckBox chkDetailTiket, chkRekapBulanan, chkOutputHarian, chkRincianDowntime;
        private AppButton btnExport, btnPreview;
        private DataGridView gridPreview;
        private Label lblPreviewStatus;

        public ReportView()
        {
            InitializeComponent();
            dateStart.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            LoadAreasFromDatabase(); // Ambil Area langsung dari DB saat form dibuka
        }

        // =========================================================
        // AMBIL DATA AREA MURNI DARI DATABASE
        // =========================================================
        private void LoadAreasFromDatabase()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    // Ambil semua nama area dari tabel machine_areas
                    var areas = connection.Query<string>("SELECT area_name FROM machine_areas WHERE area_name != 'Lain2' ORDER BY area_name ASC").ToList();
                    
                    cmbArea.Items.Clear();
                    cmbArea.Items.Add("Semua Area"); // Pilihan Default
                    
                    foreach (var area in areas) 
                    {
                        cmbArea.Items.Add(area);
                    }
                    
                    cmbArea.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                // Jika DB bermasalah, beri tahu Admin
                cmbArea.Items.Add("Semua Area");
                cmbArea.SelectedIndex = 0;
                MessageBox.Show($"Gagal memuat daftar Area dari Database:\n{ex.Message}", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void BtnExport_Click(object sender, EventArgs e)
        {
            if (!chkDetailTiket.Checked && !chkRekapBulanan.Checked && !chkOutputHarian.Checked && !chkRincianDowntime.Checked)
            {
                MessageBox.Show("Pilih minimal satu jenis laporan untuk diekspor.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                string areaName = cmbArea.SelectedItem?.ToString() ?? "Semua Area";
                string safeArea = areaName == "Semua Area" ? "SemuaArea" : areaName;
                
                saveFileDialog.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                saveFileDialog.Title = "Simpan Laporan Excel";
                saveFileDialog.FileName = $"Laporan_Maintenance_{safeArea}_{dateStart.Value:yyyy-MM-dd}_hingga_{dateEnd.Value:yyyy-MM-dd}.xlsx";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        btnExport.Enabled = false;
                        btnExport.Text = "Memproses Data...";
                        Application.DoEvents(); // Agar UI tidak lag

                        using (var workbook = new XLWorkbook())
                        {
                            int totalSteps = (chkDetailTiket.Checked ? 1 : 0) + (chkRekapBulanan.Checked ? 1 : 0) + ((chkOutputHarian.Checked || chkRincianDowntime.Checked) ? 1 : 0);
                            int currentStep = 1;

                            if (chkDetailTiket.Checked)
                            {
                                btnExport.Text = $"Memproses Data... ({currentStep++}/{totalSteps})";
                                Application.DoEvents();
                                var dataDetail = await FetchDataForReportAsync(dateStart.Value, dateEnd.Value, areaName);
                                if (dataDetail.Columns.Count > 0)
                                {
                                    var wsDetail = workbook.Worksheets.Add("Detail Tiket");
                                    wsDetail.Cell("A1").InsertTable(dataDetail);
                                    wsDetail.Rows().Style.Alignment.WrapText = true; 
                                    wsDetail.Row(1).Style.Alignment.WrapText = false; 
                                    wsDetail.Rows().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                    wsDetail.Row(1).Style.Font.Bold = true;
                                    wsDetail.Row(1).Style.Fill.BackgroundColor = XLColor.FromColor(AppColors.Primary);
                                    wsDetail.Row(1).Style.Font.FontColor = XLColor.White;
                                    wsDetail.Columns().AdjustToContents();
                                }
                            }

                            if (chkRekapBulanan.Checked)
                            {
                                btnExport.Text = $"Memproses Data... ({currentStep++}/{totalSteps})";
                                Application.DoEvents();
                                var dataRekapBulanan = await FetchMonthlyDowntimeSummaryAsync(dateStart.Value, dateEnd.Value, areaName);
                                if (dataRekapBulanan.Columns.Count > 0)
                                {
                                    var wsBulanan = workbook.Worksheets.Add("Rekap Downtime (Bulan)");
                                    wsBulanan.Cell("A1").InsertTable(dataRekapBulanan);
                                    wsBulanan.Row(1).Style.Font.Bold = true;
                                    wsBulanan.Row(1).Style.Fill.BackgroundColor = XLColor.Firebrick; 
                                    wsBulanan.Row(1).Style.Font.FontColor = XLColor.White;
                                    wsBulanan.Columns().AdjustToContents();
                                }
                            }

                            if (chkOutputHarian.Checked || chkRincianDowntime.Checked)
                            {
                                btnExport.Text = $"Memproses Data... ({currentStep++}/{totalSteps})";
                                Application.DoEvents();
                                var dataOutputHarian = await OutputExportService.FetchDailyOutputSummaryAsync(dateStart.Value, dateEnd.Value, areaName);

                                if (chkOutputHarian.Checked && dataOutputHarian.Tables.Count > 0 && dataOutputHarian.Tables[0].Columns.Count > 0)
                                {
                                    var wsHarian = workbook.Worksheets.Add("Output Harian");
                                    wsHarian.Cell("A1").InsertTable(dataOutputHarian.Tables[0]);
                                    wsHarian.Row(1).Style.Font.Bold = true;
                                    wsHarian.Row(1).Style.Fill.BackgroundColor = XLColor.SeaGreen; 
                                    wsHarian.Row(1).Style.Font.FontColor = XLColor.White;
                                    wsHarian.Columns().AdjustToContents();
                                }

                                if (chkRincianDowntime.Checked && dataOutputHarian.Tables.Count > 1 && dataOutputHarian.Tables[1].Columns.Count > 0)
                                {
                                    var wsDowntime = workbook.Worksheets.Add("Rincian Downtime Operator");
                                    wsDowntime.Cell("A1").InsertTable(dataOutputHarian.Tables[1]);
                                    wsDowntime.Row(1).Style.Font.Bold = true;
                                    wsDowntime.Row(1).Style.Fill.BackgroundColor = XLColor.Purple; 
                                    wsDowntime.Row(1).Style.Font.FontColor = XLColor.White;
                                    wsDowntime.Columns().AdjustToContents();
                                }
                            }

                            if (workbook.Worksheets.Count > 0)
                            {
                                workbook.SaveAs(saveFileDialog.FileName);
                                MessageBox.Show("Laporan berhasil diekspor!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("Tidak ada data untuk diekspor pada rentang tanggal tersebut.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Terjadi kesalahan saat membuat laporan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btnExport.Enabled = true;
                        btnExport.Text = "Generate & Export Laporan Utama";
                    }
                }
            }
        }
        
        // =========================================================
        // FUNGSI 1: DATA DETAIL
        // =========================================================
        private async Task<DataTable> FetchDataForReportAsync(DateTime startDate, DateTime endDate, string area)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Step 1: Get filtered ticket IDs using indexed columns only
                string idSql = @"
                    SELECT t.ticket_id 
                    FROM tickets t
                    JOIN machines m ON t.machine_id = m.machine_id
                    JOIN machine_areas ma ON m.area_id = ma.area_id
                    JOIN machine_types mt ON m.type_id = mt.type_id
                    WHERE t.created_at BETWEEN @StartDate AND @EndDate";

                if (area != "Semua Area") {
                    idSql += " AND ma.area_name = @Area";
                }

                // URUTAN KHUSUS: Tipe -> Area -> Angka (Casting String ke Integer) -> Waktu Terbaru
                idSql += " ORDER BY mt.type_name ASC, ma.area_name ASC, CAST(m.machine_number AS UNSIGNED) ASC, t.created_at DESC";

                var ticketIds = (await connection.QueryAsync<long>(idSql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1), Area = area }, commandTimeout: 300)).AsList();

                if (ticketIds.Count == 0)
                {
                    var emptyReader = await connection.ExecuteReaderAsync("SELECT * FROM view_admin_report WHERE 1=0");
                    var dataTableEmpty = new DataTable();
                    dataTableEmpty.Load(emptyReader);
                    if (dataTableEmpty.Columns.Contains("Status Terkini"))
                    {
                        dataTableEmpty.Columns.Remove("Status Terkini");
                    }
                    return dataTableEmpty;
                }

                // Step 2: Fetch view data only for matching IDs (avoids double-join)
                string viewSql = $"SELECT * FROM view_admin_report WHERE `ID Tiket` IN @Ids ORDER BY FIELD(`ID Tiket`, {string.Join(",", ticketIds)})";
                
                var reader = await connection.ExecuteReaderAsync(viewSql, new { Ids = ticketIds }, commandTimeout: 300);
                var dataTable = new DataTable();
                dataTable.Load(reader);

                if (dataTable.Columns.Contains("Status Terkini"))
                {
                    dataTable.Columns.Remove("Status Terkini");
                }

                return dataTable;
            }
        }

        // =========================================================
        // FUNGSI 2: REKAP DOWNTIME BULANAN
        // =========================================================
        private async Task<DataTable> FetchMonthlyDowntimeSummaryAsync(DateTime startDate, DateTime endDate, string area)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        DATE_FORMAT(t.created_at, '%M %Y') AS 'Bulan',
                        CONCAT(IFNULL(mt.type_name, ''), '.', IFNULL(ma.area_name, ''), '-', LPAD(m.machine_number, 2, '0')) AS 'Nama Mesin',
                        COUNT(t.ticket_id) AS 'Total Tiket Problem',
                        IFNULL(SUM(TIMESTAMPDIFF(MINUTE, t.created_at, IFNULL(t.production_resumed_at, t.technician_finished_at))), 0) AS 'Total Downtime (Menit)'
                    FROM tickets t
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    WHERE t.created_at BETWEEN @StartDate AND @EndDate";

                if (area != "Semua Area") {
                    sql += " AND ma.area_name = @Area";
                }

                sql += @"
                    GROUP BY DATE_FORMAT(t.created_at, '%M %Y'), YEAR(t.created_at), MONTH(t.created_at), m.machine_id, mt.type_name, ma.area_name, m.machine_number
                    ORDER BY YEAR(t.created_at) DESC, MONTH(t.created_at) DESC, mt.type_name ASC, ma.area_name ASC, CAST(m.machine_number AS UNSIGNED) ASC";
                
                var reader = await connection.ExecuteReaderAsync(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1), Area = area }, commandTimeout: 300);
                var dataTable = new DataTable();
                dataTable.Load(reader);
                return dataTable;
            }
        }

        // FUNGSI 3 dihapus karena pindah ke OutputExportService

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // =========================================================
        // UI COMPONENTS (Tampilan Konfigurasi & Preview Baru)
        // =========================================================
        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Name = "ReportView";
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Surface;
            this.Padding = new Padding(24);

            // ==========================================
            // KIRI: PANEL KONFIGURASI
            // ==========================================
            AppCard cardConfig = new AppCard
            {
                Dock = DockStyle.Left,
                Width = 380,
                ShowShadow = true,
                CornerRadius = 16,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(20)
            };

            this.lblTitle = new Label { Text = "Export Laporan", Font = AppFonts.Header3, ForeColor = AppColors.TextPrimary, AutoSize = true, Location = new Point(20, 20) };
            cardConfig.Controls.Add(lblTitle);

            // Rentang Waktu
            this.lblDateStart = new Label { Text = "Tanggal Mulai", Font = AppFonts.BodySmall, ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(20, 70) };
            this.dateStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Font = AppFonts.BodySmall, Location = new Point(20, 95), Width = 150 };
            
            this.lblDateEnd = new Label { Text = "Tanggal Akhir", Font = AppFonts.BodySmall, ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(190, 70) };
            this.dateEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Font = AppFonts.BodySmall, Location = new Point(190, 95), Width = 150 };

            cardConfig.Controls.Add(lblDateStart); cardConfig.Controls.Add(dateStart);
            cardConfig.Controls.Add(lblDateEnd); cardConfig.Controls.Add(dateEnd);

            // Area Filter
            this.lblArea = new Label { Text = "Filter Area", Font = AppFonts.BodySmall, ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(20, 140) };
            this.cmbArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppFonts.BodySmall, Location = new Point(20, 165), Width = 320 };
            cardConfig.Controls.Add(lblArea); cardConfig.Controls.Add(cmbArea);

            // Pilihan Export
            Label lblJenis = new Label { Text = "Jenis Laporan yang Diekspor:", Font = new Font(AppFonts.FontFamily, 10F, FontStyle.Bold), ForeColor = AppColors.TextPrimary, AutoSize = true, Location = new Point(20, 215) };
            cardConfig.Controls.Add(lblJenis);

            this.chkDetailTiket = new CheckBox { Text = "Detail Tiket (Raw Data)", Font = AppFonts.BodySmall, Location = new Point(20, 245), AutoSize = true, Checked = true };
            this.chkRekapBulanan = new CheckBox { Text = "Rekap Bulanan Downtime", Font = AppFonts.BodySmall, Location = new Point(20, 275), AutoSize = true, Checked = true };
            this.chkOutputHarian = new CheckBox { Text = "Output Harian Mesin", Font = AppFonts.BodySmall, Location = new Point(20, 305), AutoSize = true, Checked = true };
            this.chkRincianDowntime = new CheckBox { Text = "Rincian Waktu Downtime", Font = AppFonts.BodySmall, Location = new Point(20, 335), AutoSize = true, Checked = true };
            
            cardConfig.Controls.Add(chkDetailTiket); cardConfig.Controls.Add(chkRekapBulanan);
            cardConfig.Controls.Add(chkOutputHarian); cardConfig.Controls.Add(chkRincianDowntime);

            // Tombol Action
            this.btnPreview = new AppButton { Text = "🔍 Tampilkan Pratinjau (Preview)", Type = AppButton.ButtonType.Secondary, Location = new Point(20, 390), Size = new Size(320, 45) };
            this.btnPreview.Click += BtnPreview_Click;
            cardConfig.Controls.Add(btnPreview);

            this.btnExport = new AppButton { Text = "📥 Generate & Export Excel", Type = AppButton.ButtonType.Primary, Location = new Point(20, 445), Size = new Size(320, 50) };
            this.btnExport.Click += BtnExport_Click;
            cardConfig.Controls.Add(btnExport);

            // ==========================================
            // KANAN: PANEL PREVIEW
            // ==========================================
            AppCard cardPreview = new AppCard
            {
                Dock = DockStyle.Fill,
                ShowShadow = true,
                CornerRadius = 16,
                BackColor = AppColors.CardBackground,
                Padding = new Padding(20)
            };

            Panel pnlPreviewHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            Label lblPreviewTitle = new Label { Text = "Pratinjau Data (Detail Tiket - Maks 50 Baris)", Font = new Font(AppFonts.FontFamily, 11F, FontStyle.Bold), ForeColor = AppColors.TextPrimary, AutoSize = true, Dock = DockStyle.Left };
            this.lblPreviewStatus = new Label { Text = "Belum memuat data", Font = AppFonts.BodySmall, ForeColor = AppColors.TextSecondary, AutoSize = true, Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight };
            pnlPreviewHeader.Controls.Add(lblPreviewTitle);
            pnlPreviewHeader.Controls.Add(lblPreviewStatus);
            cardPreview.Controls.Add(pnlPreviewHeader);

            this.gridPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.CardBackground,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(238, 242, 246),
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                Margin = new Padding(0, 16, 0, 0)
            };
            this.gridPreview.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            this.gridPreview.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
            this.gridPreview.ColumnHeadersDefaultCellStyle.Font = new Font(AppFonts.FontFamily, 9.5F, FontStyle.Bold);
            this.gridPreview.ColumnHeadersHeight = 40;
            this.gridPreview.DefaultCellStyle.SelectionBackColor = AppColors.CardBackground;
            this.gridPreview.DefaultCellStyle.SelectionForeColor = AppColors.TextPrimary;
            this.gridPreview.DefaultCellStyle.Font = AppFonts.BodySmall;
            this.gridPreview.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
            this.gridPreview.RowTemplate.Height = 40;

            cardPreview.Controls.Add(gridPreview);
            gridPreview.BringToFront(); // Ensures grid takes remaining space AFTER header, making column headers visible

            // Container Wrapper untuk spasi antar Card
            Panel pnlSpacer = new Panel { Dock = DockStyle.Left, Width = 24, BackColor = Color.Transparent };

            this.Controls.Add(cardPreview);
            this.Controls.Add(pnlSpacer);
            this.Controls.Add(cardConfig);

            // Z-Order
            cardPreview.BringToFront();

            this.ResumeLayout(false);
        }

        private async void BtnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                btnPreview.Enabled = false;
                btnPreview.Text = "Memuat Pratinjau...";
                lblPreviewStatus.Text = "Mengambil data...";
                Application.DoEvents();

                string areaName = cmbArea.SelectedItem?.ToString() ?? "Semua Area";
                
                // Fetch top 50 detailed tickets
                var dataDetail = await FetchDataForReportAsync(dateStart.Value, dateEnd.Value, areaName);
                
                if (dataDetail != null && dataDetail.Rows.Count > 0)
                {
                    // Limit to 50 for preview
                    var previewTable = dataDetail.Clone();
                    for (int i = 0; i < Math.Min(50, dataDetail.Rows.Count); i++)
                    {
                        previewTable.ImportRow(dataDetail.Rows[i]);
                    }
                    
                    gridPreview.DataSource = previewTable;
                    lblPreviewStatus.Text = $"Menampilkan {previewTable.Rows.Count} dari {dataDetail.Rows.Count} total baris";
                }
                else
                {
                    gridPreview.DataSource = null;
                    lblPreviewStatus.Text = "Tidak ada data pada rentang ini.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat pratinjau: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblPreviewStatus.Text = "Gagal memuat pratinjau.";
            }
            finally
            {
                btnPreview.Enabled = true;
                btnPreview.Text = "🔍 Tampilkan Pratinjau (Preview)";
            }
        }
    }
}