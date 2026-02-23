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
                            
                            // ═══ PENTING: AGAR TEXT TIDAK MENUMPUK, KITA WRAP TEXT ═══
                            wsDetail.Rows().Style.Alignment.WrapText = true; 
                            wsDetail.Row(1).Style.Alignment.WrapText = false; // Header jangan di-wrap
                            wsDetail.Rows().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            // ═════════════════════════════════════════════════════════

                            wsDetail.Row(1).Style.Font.Bold = true;
                            wsDetail.Row(1).Style.Fill.BackgroundColor = XLColor.FromColor(AppColors.Primary);
                            wsDetail.Row(1).Style.Font.FontColor = XLColor.White;
                            wsDetail.Columns().AdjustToContents();

                            // =========================================================
                            // SHEET 2: REKAP DOWNTIME BULANAN 
                            // =========================================================
                            var wsBulanan = workbook.Worksheets.Add("Rekap Downtime (Bulan)");
                            wsBulanan.Cell("A1").InsertTable(dataRekapBulanan);
                            wsBulanan.Row(1).Style.Font.Bold = true;
                            wsBulanan.Row(1).Style.Fill.BackgroundColor = XLColor.Firebrick; 
                            wsBulanan.Row(1).Style.Font.FontColor = XLColor.White;
                            wsBulanan.Columns().AdjustToContents();

                            // =========================================================
                            // SHEET 3: REKAP OUTPUT HARIAN & EFISIENSI
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
        
        // FUNGSI 1: DATA DETAIL (KODE SUDAH SANGAT BERSIH)
        private DataTable FetchDataForReport(DateTime startDate, DateTime endDate)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Semua kolom sudah diatur rapi oleh SQL View, C# tinggal narik datanya saja.
                string sql = @"
                    SELECT v.* FROM view_admin_report v
                    JOIN tickets t ON v.`ID Tiket` = t.ticket_id
                    WHERE t.created_at BETWEEN @StartDate AND @EndDate
                    ORDER BY t.created_at DESC";
                
                var reader = connection.ExecuteReader(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1) });
                var dataTable = new DataTable();
                dataTable.Load(reader);

                if (dataTable.Columns.Contains("Status Terkini"))
                {
                    dataTable.Columns.Remove("Status Terkini");
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
                        COUNT(t.ticket_id) AS 'Total Tiket Problem',
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

        // FUNGSI 3: REKAP OUTPUT HARIAN & EFISIENSI
        private DataTable FetchDailyOutputSummary(DateTime startDate, DateTime endDate)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        DATE_FORMAT(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR), '%d %M %Y') AS 'Tanggal Produksi',
                        CONCAT(IFNULL(mt.type_name, ''), '-', IFNULL(ma.area_name, ''), '.', IFNULL(m.machine_number, '')) AS 'Nama Mesin',
                        
                        MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND HOUR(mpl.created_at) < 19 THEN mpl.produced_pieces ELSE 0 END) AS 'Output Pagi',
                        MAX(CASE WHEN HOUR(mpl.created_at) < 7 OR HOUR(mpl.created_at) >= 19 THEN mpl.produced_pieces ELSE 0 END) AS 'Output Malam',
                        
                        (MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND HOUR(mpl.created_at) < 19 THEN mpl.auto_time ELSE 0 END) +
                         MAX(CASE WHEN HOUR(mpl.created_at) < 7 OR HOUR(mpl.created_at) >= 19 THEN mpl.auto_time ELSE 0 END)) AS 'RawAutoSec',
                         
                        (MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND HOUR(mpl.created_at) < 19 THEN mpl.monitor_time ELSE 0 END) +
                         MAX(CASE WHEN HOUR(mpl.created_at) < 7 OR HOUR(mpl.created_at) >= 19 THEN mpl.monitor_time ELSE 0 END)) AS 'RawMonitorSec'
                        
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

                dataTable.Columns.Add("Total Output (Pcs)", typeof(long));
                dataTable.Columns.Add("Rata-rata / Jam (Pcs)", typeof(long));
                dataTable.Columns.Add("Production Time (Menit)", typeof(long));
                dataTable.Columns.Add("Loss Time (Menit)", typeof(long));
                dataTable.Columns.Add("Efisiensi (%)", typeof(string));

                foreach (DataRow row in dataTable.Rows)
                {
                    long outPagi = row["Output Pagi"] != DBNull.Value ? Convert.ToInt64(row["Output Pagi"]) : 0;
                    long outMalam = row["Output Malam"] != DBNull.Value ? Convert.ToInt64(row["Output Malam"]) : 0;
                    long totalOut = outPagi + outMalam;
                    
                    long autoSec = row["RawAutoSec"] != DBNull.Value ? Convert.ToInt64(row["RawAutoSec"]) : 0;
                    long monSec = row["RawMonitorSec"] != DBNull.Value ? Convert.ToInt64(row["RawMonitorSec"]) : 0;
                    
                    long autoMin = autoSec / 60;
                    long monMin = monSec / 60;
                    long lossMin = monMin > autoMin ? monMin - autoMin : 0;
                    
                    double eff = monMin > 0 ? ((double)autoMin / monMin) * 100 : 0;

                    row["Total Output (Pcs)"] = totalOut;
                    row["Rata-rata / Jam (Pcs)"] = totalOut / 24; 
                    row["Production Time (Menit)"] = autoMin;
                    row["Loss Time (Menit)"] = lossMin;
                    row["Efisiensi (%)"] = eff.ToString("F1") + " %";
                }

                dataTable.Columns.Remove("RawAutoSec");
                dataTable.Columns.Remove("RawMonitorSec");

                int outPagiIdx = dataTable.Columns["Output Pagi"].Ordinal;
                dataTable.Columns["Total Output (Pcs)"].SetOrdinal(outPagiIdx + 2);
                dataTable.Columns["Rata-rata / Jam (Pcs)"].SetOrdinal(outPagiIdx + 3);

                return dataTable;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
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
            this.dateStart.Size = new Size(200, 25);
            this.dateStart.Font = AppFonts.BodySmall;
            this.dateStart.Format = DateTimePickerFormat.Short;

            this.lblDateEnd.AutoSize = true;
            this.lblDateEnd.Font = AppFonts.BodySmall;
            this.lblDateEnd.Location = new Point(220, 50);
            this.lblDateEnd.Text = "Tanggal Akhir:";

            this.dateEnd.Location = new Point(220, 75);
            this.dateEnd.Size = new Size(200, 25);
            this.dateEnd.Font = AppFonts.BodySmall;
            this.dateEnd.Format = DateTimePickerFormat.Short;

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