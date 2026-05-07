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
        private CheckBox chkPatroliCutting, chkPatroliMikrometer, chkPatroliAplikator, chkCounterMaterial;
        private AppButton btnExport;

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
            if (!chkDetailTiket.Checked && !chkRekapBulanan.Checked && !chkOutputHarian.Checked && !chkRincianDowntime.Checked && !chkPatroliCutting.Checked && !chkPatroliMikrometer.Checked && !chkPatroliAplikator.Checked && !chkCounterMaterial.Checked)
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
                            int totalSteps = (chkDetailTiket.Checked ? 1 : 0) + (chkRekapBulanan.Checked ? 1 : 0) + ((chkOutputHarian.Checked || chkRincianDowntime.Checked) ? 1 : 0) + (chkPatroliCutting.Checked ? 1 : 0) + (chkPatroliMikrometer.Checked ? 1 : 0) + (chkPatroliAplikator.Checked ? 1 : 0) + (chkCounterMaterial.Checked ? 1 : 0);
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

                            if (chkPatroliCutting.Checked)
                            {
                                btnExport.Text = $"Memproses Data... ({currentStep++}/{totalSteps})";
                                Application.DoEvents();
                                var dataCutting = await OutputExportService.FetchPatroliCuttingAsync(dateStart.Value, dateEnd.Value, areaName);
                                if (dataCutting.Columns.Count > 0)
                                {
                                    var wsCutting = workbook.Worksheets.Add("Patroli Mesin Cutting");
                                    wsCutting.Cell("A1").InsertTable(dataCutting);
                                    wsCutting.Row(1).Style.Font.Bold = true;
                                    wsCutting.Row(1).Style.Fill.BackgroundColor = XLColor.AirForceBlue; 
                                    wsCutting.Row(1).Style.Font.FontColor = XLColor.White;
                                    wsCutting.Columns().AdjustToContents();
                                }
                            }

                            if (chkPatroliMikrometer.Checked)
                            {
                                btnExport.Text = $"Memproses Data... ({currentStep++}/{totalSteps})";
                                Application.DoEvents();
                                var dataMikro = await OutputExportService.FetchPatroliMikrometerAsync(dateStart.Value, dateEnd.Value, areaName);
                                if (dataMikro.Columns.Count > 0)
                                {
                                    var wsMikro = workbook.Worksheets.Add("Patroli Mikrometer");
                                    wsMikro.Cell("A1").InsertTable(dataMikro);
                                    wsMikro.Row(1).Style.Font.Bold = true;
                                    wsMikro.Row(1).Style.Fill.BackgroundColor = XLColor.AirForceBlue; 
                                    wsMikro.Row(1).Style.Font.FontColor = XLColor.White;
                                    wsMikro.Columns().AdjustToContents();
                                }
                            }

                            if (chkPatroliAplikator.Checked)
                            {
                                btnExport.Text = $"Memproses Data... ({currentStep++}/{totalSteps})";
                                Application.DoEvents();
                                var dataAplikator = await OutputExportService.FetchPatroliAplikatorAsync(dateStart.Value, dateEnd.Value, areaName);
                                if (dataAplikator.Columns.Count > 0)
                                {
                                    var wsAplikator = workbook.Worksheets.Add("Patroli Aplikator");
                                    wsAplikator.Cell("A1").InsertTable(dataAplikator);
                                    wsAplikator.Row(1).Style.Font.Bold = true;
                                    wsAplikator.Row(1).Style.Fill.BackgroundColor = XLColor.AirForceBlue; 
                                    wsAplikator.Row(1).Style.Font.FontColor = XLColor.White;
                                    wsAplikator.Columns().AdjustToContents();
                                }
                            }

                            if (chkCounterMaterial.Checked)
                            {
                                btnExport.Text = $"Memproses Data... ({currentStep++}/{totalSteps})";
                                Application.DoEvents();
                                var dataCounter = await OutputExportService.FetchCounterMaterialAsync(dateStart.Value, dateEnd.Value, areaName);
                                if (dataCounter.Columns.Count > 0)
                                {
                                    var wsCounter = workbook.Worksheets.Add("Counter Material");
                                    wsCounter.Cell("A1").InsertTable(dataCounter);
                                    wsCounter.Row(1).Style.Font.Bold = true;
                                    wsCounter.Row(1).Style.Fill.BackgroundColor = XLColor.AirForceBlue; 
                                    wsCounter.Row(1).Style.Font.FontColor = XLColor.White;
                                    wsCounter.Columns().AdjustToContents();
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
        // UI COMPONENTS (Tampilan Filter Baru)
        // =========================================================
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.lblTitle = new Label();
            this.lblDateStart = new Label();
            this.dateStart = new DateTimePicker();
            this.lblDateEnd = new Label();
            this.dateEnd = new DateTimePicker();
            this.lblArea = new Label();
            this.cmbArea = new ComboBox();
            this.chkDetailTiket = new CheckBox();
            this.chkRekapBulanan = new CheckBox();
            this.chkOutputHarian = new CheckBox();
            this.chkRincianDowntime = new CheckBox();
            this.chkPatroliCutting = new CheckBox();
            this.chkPatroliMikrometer = new CheckBox();
            this.chkPatroliAplikator = new CheckBox();
            this.chkCounterMaterial = new CheckBox();
            this.btnExport = new AppButton();

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = AppFonts.Header3;
            this.lblTitle.ForeColor = AppColors.TextPrimary;
            this.lblTitle.Location = new Point(0, 0);
            this.lblTitle.Text = "Buat Laporan Tiket (Excel)";
            
            this.lblDateStart.AutoSize = true;
            this.lblDateStart.Font = AppFonts.BodySmall;
            this.lblDateStart.Location = new Point(0, 50);
            this.lblDateStart.Text = "Tanggal Mulai:";

            this.dateStart.Location = new Point(0, 75);
            this.dateStart.Size = new Size(180, 25);
            this.dateStart.Font = AppFonts.BodySmall;
            this.dateStart.Format = DateTimePickerFormat.Short;

            this.lblDateEnd.AutoSize = true;
            this.lblDateEnd.Font = AppFonts.BodySmall;
            this.lblDateEnd.Location = new Point(200, 50);
            this.lblDateEnd.Text = "Tanggal Akhir:";

            this.dateEnd.Location = new Point(200, 75);
            this.dateEnd.Size = new Size(180, 25);
            this.dateEnd.Font = AppFonts.BodySmall;
            this.dateEnd.Format = DateTimePickerFormat.Short;

            // Tambahan Dropdown Area
            this.lblArea.AutoSize = true;
            this.lblArea.Font = AppFonts.BodySmall;
            this.lblArea.Location = new Point(400, 50);
            this.lblArea.Text = "Filter Area:";

            this.cmbArea.Location = new Point(400, 75);
            this.cmbArea.Size = new Size(180, 25);
            this.cmbArea.Font = AppFonts.BodySmall;
            this.cmbArea.DropDownStyle = ComboBoxStyle.DropDownList;

            this.chkDetailTiket.AutoSize = true;
            this.chkDetailTiket.Font = AppFonts.BodySmall;
            this.chkDetailTiket.Location = new Point(0, 115);
            this.chkDetailTiket.Text = "Detail Tiket";
            this.chkDetailTiket.Checked = true;

            this.chkRekapBulanan.AutoSize = true;
            this.chkRekapBulanan.Font = AppFonts.BodySmall;
            this.chkRekapBulanan.Location = new Point(120, 115);
            this.chkRekapBulanan.Text = "Rekap Bulanan";
            this.chkRekapBulanan.Checked = true;

            this.chkOutputHarian.AutoSize = true;
            this.chkOutputHarian.Font = AppFonts.BodySmall;
            this.chkOutputHarian.Location = new Point(260, 115);
            this.chkOutputHarian.Text = "Output Harian";
            this.chkOutputHarian.Checked = true;

            this.chkRincianDowntime.AutoSize = true;
            this.chkRincianDowntime.Font = AppFonts.BodySmall;
            this.chkRincianDowntime.Location = new Point(390, 115);
            this.chkRincianDowntime.Text = "Rincian Downtime";
            this.chkRincianDowntime.Checked = true;

            this.chkPatroliCutting.AutoSize = true;
            this.chkPatroliCutting.Font = AppFonts.BodySmall;
            this.chkPatroliCutting.Location = new Point(0, 145);
            this.chkPatroliCutting.Text = "Patroli Mesin Cutting";
            this.chkPatroliCutting.Checked = true;

            this.chkPatroliMikrometer.AutoSize = true;
            this.chkPatroliMikrometer.Font = AppFonts.BodySmall;
            this.chkPatroliMikrometer.Location = new Point(190, 145);
            this.chkPatroliMikrometer.Text = "Patroli Mikrometer";
            this.chkPatroliMikrometer.Checked = true;

            this.chkPatroliAplikator.AutoSize = true;
            this.chkPatroliAplikator.Font = AppFonts.BodySmall;
            this.chkPatroliAplikator.Location = new Point(360, 145);
            this.chkPatroliAplikator.Text = "Patroli Aplikator";
            this.chkPatroliAplikator.Checked = true;

            this.chkCounterMaterial.AutoSize = true;
            this.chkCounterMaterial.Font = AppFonts.BodySmall;
            this.chkCounterMaterial.Location = new Point(510, 145);
            this.chkCounterMaterial.Text = "Counter Material";
            this.chkCounterMaterial.Checked = true;

            this.btnExport.Text = "Generate & Export Laporan Utama";
            this.btnExport.Location = new Point(0, 185);
            this.btnExport.Size = new Size(250, 50);
            this.btnExport.Click += BtnExport_Click;

            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblDateStart);
            this.Controls.Add(this.dateStart);
            this.Controls.Add(this.lblDateEnd);
            this.Controls.Add(this.dateEnd);
            this.Controls.Add(this.lblArea);
            this.Controls.Add(this.cmbArea);
            this.Controls.Add(this.chkDetailTiket);
            this.Controls.Add(this.chkRekapBulanan);
            this.Controls.Add(this.chkOutputHarian);
            this.Controls.Add(this.chkRincianDowntime);
            this.Controls.Add(this.chkPatroliCutting);
            this.Controls.Add(this.chkPatroliMikrometer);
            this.Controls.Add(this.chkPatroliAplikator);
            this.Controls.Add(this.chkCounterMaterial);
            this.Controls.Add(this.btnExport);
            
            this.Name = "ReportView";
            this.Dock = DockStyle.Fill;
            this.ResumeLayout(false);
        }
    }
}