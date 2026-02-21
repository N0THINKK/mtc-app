using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using ClosedXML.Excel;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.views
{
    public partial class ReportView : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblTitle, lblDateStart, lblDateEnd;
        private DateTimePicker dateStart, dateEnd;
        private AppButton btnExport;

        public ReportView()
        {
            InitializeComponent();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                saveFileDialog.Title = "Simpan Laporan Excel";
                saveFileDialog.FileName = $"Laporan_Maintenance_{dateStart.Value:yyyy-MM-dd}_hingga_{dateEnd.Value:yyyy-MM-dd}.xlsx";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Tarik 3 Jenis Data dari Database
                        var dataDetail = FetchDataForReport(dateStart.Value, dateEnd.Value);
                        var dataRekapBulanan = FetchMonthlyDowntimeSummary(dateStart.Value, dateEnd.Value);
                        var dataOutputHarian = FetchDailyOutputSummary(dateStart.Value, dateEnd.Value);

                        using (var workbook = new XLWorkbook())
                        {
                            // =========================================================
                            // SHEET 1: DETAIL TIKET
                            // =========================================================
                            var wsDetail = workbook.Worksheets.Add("Detail Tiket");
                            wsDetail.Cell("A1").InsertTable(dataDetail);
                            wsDetail.Row(1).Style.Font.Bold = true;
                            wsDetail.Row(1).Style.Fill.BackgroundColor = XLColor.FromColor(AppColors.Primary);
                            wsDetail.Row(1).Style.Font.FontColor = XLColor.White;
                            wsDetail.Columns().AdjustToContents();

                            // =========================================================
                            // SHEET 2: REKAP DOWNTIME BULANAN (Untuk Grafik Bar)
                            // =========================================================
                            var wsBulanan = workbook.Worksheets.Add("Rekap Downtime (Bulan)");
                            wsBulanan.Cell("A1").InsertTable(dataRekapBulanan);
                            wsBulanan.Row(1).Style.Font.Bold = true;
                            wsBulanan.Row(1).Style.Fill.BackgroundColor = XLColor.Firebrick; 
                            wsBulanan.Row(1).Style.Font.FontColor = XLColor.White;
                            wsBulanan.Columns().AdjustToContents();

                            // =========================================================
                            // SHEET 3: REKAP OUTPUT HARIAN (Untuk Grafik Garis)
                            // =========================================================
                            var wsHarian = workbook.Worksheets.Add("Output Harian");
                            wsHarian.Cell("A1").InsertTable(dataOutputHarian);
                            wsHarian.Row(1).Style.Font.Bold = true;
                            wsHarian.Row(1).Style.Fill.BackgroundColor = XLColor.SeaGreen; 
                            wsHarian.Row(1).Style.Font.FontColor = XLColor.White;
                            wsHarian.Columns().AdjustToContents();

                            workbook.SaveAs(saveFileDialog.FileName);
                        }

                        MessageBox.Show($"Laporan berhasil diekspor!\n\nFile Excel ini berisi 3 Sheet:\n1. Detail Tiket\n2. Rekap Downtime Bulanan\n3. Output Mesin Harian", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Terjadi kesalahan saat membuat laporan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        // FUNGSI 1: DATA DETAIL
        private DataTable FetchDataForReport(DateTime startDate, DateTime endDate)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT * FROM view_admin_report 
                    WHERE `Waktu Lapor` BETWEEN @StartDate AND @EndDate
                    ORDER BY `Waktu Lapor` DESC";
                
                var reader = connection.ExecuteReader(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1) });
                var dataTable = new DataTable();
                dataTable.Load(reader);

                string colName = "Detail Masalah"; 
                if (dataTable.Columns.Contains(colName))
                {
                    dataTable.Columns.Add("Jenis Masalah", typeof(string));
                    dataTable.Columns.Add("Deskripsi Detail", typeof(string));
                    dataTable.Columns.Add("Nomor Aplikator", typeof(string));

                    int colIndex = dataTable.Columns[colName].Ordinal;
                    dataTable.Columns["Jenis Masalah"].SetOrdinal(colIndex);
                    dataTable.Columns["Deskripsi Detail"].SetOrdinal(colIndex + 1);
                    dataTable.Columns["Nomor Aplikator"].SetOrdinal(colIndex + 2);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string rawData = row[colName]?.ToString() ?? "";
                        string jenis = "-", deskripsi = "-", aplikator = "-";

                        if (rawData.Contains("(App: "))
                        {
                            int appIndex = rawData.IndexOf("(App: ");
                            aplikator = rawData.Substring(appIndex + 6).TrimEnd(')'); 
                            rawData = rawData.Substring(0, appIndex).Trim(); 
                        }
                        if (rawData.Contains(": "))
                        {
                            int colonIndex = rawData.IndexOf(": ");
                            jenis = rawData.Substring(0, colonIndex).Trim();
                            deskripsi = rawData.Substring(colonIndex + 2).Trim();
                        }
                        else
                        {
                            deskripsi = rawData.Trim(); 
                        }

                        row["Jenis Masalah"] = jenis;
                        row["Deskripsi Detail"] = deskripsi;
                        row["Nomor Aplikator"] = aplikator;
                    }
                    dataTable.Columns.Remove(colName);
                }

                if (dataTable.Columns.Contains("Durasi Respon")) dataTable.Columns["Durasi Respon"].ColumnName = "Tunggu Teknisi";
                if (dataTable.Columns.Contains("Waktu Tunggu Part")) dataTable.Columns["Waktu Tunggu Part"].ColumnName = "Tunggu Part";
                if (dataTable.Columns.Contains("Durasi Trial Run")) dataTable.Columns["Durasi Trial Run"].ColumnName = "Tunggu Operator";

                if (dataTable.Columns.Contains("Durasi Perbaikan") && dataTable.Columns.Contains("Tunggu Part"))
                {
                    int perbaikanIndex = dataTable.Columns["Durasi Perbaikan"].Ordinal;
                    dataTable.Columns["Tunggu Part"].SetOrdinal(perbaikanIndex);
                }

                return dataTable;
            }
        }

        // FUNGSI 2: REKAP DOWNTIME BULANAN
        private DataTable FetchMonthlyDowntimeSummary(DateTime startDate, DateTime endDate)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        DATE_FORMAT(t.created_at, '%M %Y') AS 'Bulan',
                        CONCAT(IFNULL(mt.type_name, ''), '-', IFNULL(ma.area_name, ''), '.', IFNULL(m.machine_number, '')) AS 'Nama Mesin',
                        COUNT(t.ticket_id) AS 'Total Tiket Masalah',
                        IFNULL(SUM(TIMESTAMPDIFF(MINUTE, t.created_at, IFNULL(t.production_resumed_at, t.technician_finished_at))), 0) AS 'Total Downtime (Menit)'
                    FROM tickets t
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    WHERE t.created_at BETWEEN @StartDate AND @EndDate
                    GROUP BY DATE_FORMAT(t.created_at, '%M %Y'), YEAR(t.created_at), MONTH(t.created_at), m.machine_id
                    ORDER BY YEAR(t.created_at) DESC, MONTH(t.created_at) DESC, 'Nama Mesin' ASC";
                
                var reader = connection.ExecuteReader(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1) });
                var dataTable = new DataTable();
                dataTable.Load(reader);
                return dataTable;
            }
        }

        // FUNGSI 3: REKAP OUTPUT HARIAN & SHIFT (SINKRON DENGAN MONITOR DASHBOARD)
        private DataTable FetchDailyOutputSummary(DateTime startDate, DateTime endDate)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // LOGIKA SQL YANG BARU (Sesuai dengan MachineMonitorControl.cs):
                // 1. INTERVAL 7 HOUR -> Jam 00:00 s/d 06:59 pagi akan dihitung sebagai tanggal kemarin.
                // 2. HOUR >= 7 AND HOUR < 19 -> Shift Pagi (07:00 - 18:59)
                // 3. HOUR < 7 OR HOUR >= 19 -> Shift Malam (19:00 - 06:59)

                string sql = @"
                    SELECT 
                        DATE_FORMAT(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR), '%d %M %Y') AS 'Tanggal Produksi',
                        CONCAT(IFNULL(mt.type_name, ''), '-', IFNULL(ma.area_name, ''), '.', IFNULL(m.machine_number, '')) AS 'Nama Mesin',
                        
                        -- Output Shift Pagi (07.00 - 18.59)
                        MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND HOUR(mpl.created_at) < 19 THEN mpl.produced_pieces ELSE 0 END) AS 'Output Pagi (Pcs)',
                        
                        -- Output Shift Malam (19.00 - 06.59 besoknya)
                        MAX(CASE WHEN HOUR(mpl.created_at) < 7 OR HOUR(mpl.created_at) >= 19 THEN mpl.produced_pieces ELSE 0 END) AS 'Output Malam (Pcs)',
                        
                        -- Total Harian (Pagi + Malam)
                        (
                            MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND HOUR(mpl.created_at) < 19 THEN mpl.produced_pieces ELSE 0 END) +
                            MAX(CASE WHEN HOUR(mpl.created_at) < 7 OR HOUR(mpl.created_at) >= 19 THEN mpl.produced_pieces ELSE 0 END)
                        ) AS 'Total Output Harian (Pcs)'
                        
                    FROM machine_process_logs mpl
                    JOIN machines m ON mpl.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    WHERE mpl.created_at BETWEEN @StartDate AND @EndDate
                    
                    GROUP BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)), m.machine_id
                    ORDER BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)) DESC, 'Nama Mesin' ASC";
                
                var reader = connection.ExecuteReader(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1) });
                var dataTable = new DataTable();
                dataTable.Load(reader);
                return dataTable;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.lblTitle = new Label();
            this.lblDateStart = new Label();
            this.dateStart = new DateTimePicker();
            this.lblDateEnd = new Label();
            this.dateEnd = new DateTimePicker();
            this.btnExport = new AppButton();

            // Title
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = AppFonts.Header3;
            this.lblTitle.ForeColor = AppColors.TextPrimary;
            this.lblTitle.Location = new Point(0, 0);
            this.lblTitle.Text = "Buat Laporan Tiket (Excel)";
            
            // Date Start Label
            this.lblDateStart.AutoSize = true;
            this.lblDateStart.Font = AppFonts.BodySmall;
            this.lblDateStart.Location = new Point(0, 50);
            this.lblDateStart.Text = "Tanggal Mulai:";

            // Date Start Picker
            this.dateStart.Location = new Point(0, 75);
            this.dateStart.Size = new Size(200, 25);
            this.dateStart.Font = AppFonts.BodySmall;
            this.dateStart.Format = DateTimePickerFormat.Short;

            // Date End Label
            this.lblDateEnd.AutoSize = true;
            this.lblDateEnd.Font = AppFonts.BodySmall;
            this.lblDateEnd.Location = new Point(220, 50);
            this.lblDateEnd.Text = "Tanggal Akhir:";

            // Date End Picker
            this.dateEnd.Location = new Point(220, 75);
            this.dateEnd.Size = new Size(200, 25);
            this.dateEnd.Font = AppFonts.BodySmall;
            this.dateEnd.Format = DateTimePickerFormat.Short;

            // Export Button
            this.btnExport.Text = "Generate & Export Excel";
            this.btnExport.Location = new Point(0, 120);
            this.btnExport.Size = new Size(250, 50);
            this.btnExport.Click += BtnExport_Click;

            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblDateStart);
            this.Controls.Add(this.dateStart);
            this.Controls.Add(this.lblDateEnd);
            this.Controls.Add(this.dateEnd);
            this.Controls.Add(this.btnExport);
            this.Name = "ReportView";
            this.Dock = DockStyle.Fill;
            this.ResumeLayout(false);
        }
    }
}