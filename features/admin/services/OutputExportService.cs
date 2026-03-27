using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Dapper;
using ClosedXML.Excel;
using mtc_app.shared.infrastructure;
using System.Threading.Tasks;

namespace mtc_app.features.admin.services
{
    public static class OutputExportService
    {
        public static void ExportDataTableToExcel(DataTable dataOutputHarian, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var wsHarian = workbook.Worksheets.Add("Output Harian");
                wsHarian.Cell("A1").InsertTable(dataOutputHarian);
                wsHarian.Row(1).Style.Font.Bold = true;
                wsHarian.Row(1).Style.Fill.BackgroundColor = XLColor.AirForceBlue; 
                wsHarian.Row(1).Style.Font.FontColor = XLColor.White;
                wsHarian.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
            }
        }

        public static DataTable FetchDailyOutputSummary(DateTime startDate, DateTime endDate, string area)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        DATE_FORMAT(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR), '%d %M %Y') AS TanggalProduksi,
                        CONCAT(IFNULL(mt.type_name, ''), '.', IFNULL(ma.area_name, ''), '-', LPAD(m.machine_number, 2, '0')) AS NamaMesin,
                        
                        MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND HOUR(mpl.created_at) < 19 THEN mpl.produced_pieces ELSE 0 END) AS OutputPagi,
                        MAX(CASE WHEN HOUR(mpl.created_at) < 7 OR HOUR(mpl.created_at) >= 19 THEN mpl.produced_pieces ELSE 0 END) AS OutputMalam,
                        
                        (MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND HOUR(mpl.created_at) < 19 THEN mpl.auto_time ELSE 0 END) +
                         MAX(CASE WHEN HOUR(mpl.created_at) < 7 OR HOUR(mpl.created_at) >= 19 THEN mpl.auto_time ELSE 0 END)) AS RawAutoSec,
                         
                        (MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND HOUR(mpl.created_at) < 19 THEN mpl.monitor_time ELSE 0 END) +
                         MAX(CASE WHEN HOUR(mpl.created_at) < 7 OR HOUR(mpl.created_at) >= 19 THEN mpl.monitor_time ELSE 0 END)) AS RawMonitorSec,

                        IFNULL(act.KanbanMin, 0) AS KanbanMin,
                        IFNULL(act.MaterialMin, 0) AS MaterialMin,
                        IFNULL(act.GantiMin, 0) AS GantiMin,
                        IFNULL(act.LainnyaMin, 0) AS LainnyaMin,

                        IFNULL(brk.TotalBreakMin, 0) AS BreakMin
                    FROM machine_process_logs mpl
                    JOIN machines m ON mpl.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    LEFT JOIN (
                        SELECT 
                            machine_name,
                            DATE(DATE_SUB(start_time, INTERVAL 7 HOUR)) AS act_date,
                            SUM(CASE WHEN activity_id = 1 THEN TIMESTAMPDIFF(MINUTE, start_time, IFNULL(end_time, NOW())) ELSE 0 END) AS KanbanMin,
                            SUM(CASE WHEN activity_id = 2 THEN TIMESTAMPDIFF(MINUTE, start_time, IFNULL(end_time, NOW())) ELSE 0 END) AS MaterialMin,
                            SUM(CASE WHEN activity_id = 3 THEN TIMESTAMPDIFF(MINUTE, start_time, IFNULL(end_time, NOW())) ELSE 0 END) AS GantiMin,
                            SUM(CASE WHEN activity_id = 4 THEN TIMESTAMPDIFF(MINUTE, start_time, IFNULL(end_time, NOW())) ELSE 0 END) AS LainnyaMin
                        FROM machine_operator_activities
                        GROUP BY machine_name, DATE(DATE_SUB(start_time, INTERVAL 7 HOUR))
                    ) act ON act.machine_name = CONCAT(IFNULL(mt.type_name, ''), '.', IFNULL(ma.area_name, ''), '-', LPAD(m.machine_number, 2, '0')) 
                            AND act.act_date = DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR))
                    LEFT JOIN (
                        SELECT day_id, SUM(non_ot_minutes + ot_minutes) AS TotalBreakMin
                        FROM shift_breaks
                        GROUP BY day_id
                    ) brk ON brk.day_id = (WEEKDAY(DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR))) + 1)
                    WHERE 1=1 AND mpl.created_at BETWEEN @StartDate AND @EndDate";

                if (area != "Semua Area") {
                    sql += " AND ma.area_name = @Area";
                }

                sql += @"
                    GROUP BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)), m.machine_id, mt.type_name, ma.area_name, m.machine_number, KanbanMin, MaterialMin, GantiMin, LainnyaMin, BreakMin
                    ORDER BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)) DESC, mt.type_name ASC, ma.area_name ASC, CAST(m.machine_number AS UNSIGNED) ASC";
                
                var results = connection.Query<dynamic>(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1), Area = area }, commandTimeout: 300);
                
                var dataTable = new DataTable();
                dataTable.Columns.Add("Tanggal Produksi", typeof(string));
                dataTable.Columns.Add("Nama Mesin", typeof(string));
                dataTable.Columns.Add("Output Pagi (Pcs)", typeof(long));
                dataTable.Columns.Add("Output Malam (Pcs)", typeof(long));
                dataTable.Columns.Add("Total Output (Pcs)", typeof(long));
                dataTable.Columns.Add("Rata-rata / Jam Pagi (Pcs)", typeof(long));
                dataTable.Columns.Add("Rata-rata / Jam Malam (Pcs)", typeof(long));
                dataTable.Columns.Add("Mesin Run (Menit)", typeof(long));
                dataTable.Columns.Add("Mesin Nyala (Menit)", typeof(long));
                dataTable.Columns.Add("Break (Menit)", typeof(long));
                dataTable.Columns.Add("Kanban Habis (Menit)", typeof(long));
                dataTable.Columns.Add("Material Habis (Menit)", typeof(long));
                dataTable.Columns.Add("Ganti Material (Menit)", typeof(long));
                dataTable.Columns.Add("Lainnya/Toilet (Menit)", typeof(long));
                dataTable.Columns.Add("Efisiensi (%)", typeof(string));

                foreach (var row in results)
                {
                    long outPagi = (long)(row.OutputPagi ?? 0);
                    long outMalam = (long)(row.OutputMalam ?? 0);
                    long totalOut = outPagi + outMalam;
                    
                    long autoSec = (long)(row.RawAutoSec ?? 0);
                    long monSec = (long)(row.RawMonitorSec ?? 0);
                    
                    long autoMin = autoSec / 60;
                    long monMin = monSec / 60;
                    long nyalaMin = monMin > autoMin ? monMin - autoMin : 0; 
                    
                    long kanban = Convert.ToInt64(row.KanbanMin ?? 0);
                    long material = Convert.ToInt64(row.MaterialMin ?? 0);
                    long ganti = Convert.ToInt64(row.GantiMin ?? 0);
                    long lainnya = Convert.ToInt64(row.LainnyaMin ?? 0);
                    long breakMin = Convert.ToInt64(row.BreakMin ?? 0);

                    // Efisiensi = Mesin Run / (Mesin Run + Mesin Nyala + Kanban Habis + Material Habis + Ganti Material + Lainnya atau Toilet - Break)
                    double pembagiEfisiensi = autoMin + nyalaMin + kanban + material + ganti + lainnya - breakMin;
                    double eff = pembagiEfisiensi > 0 ? ((double)autoMin / pembagiEfisiensi) * 100 : 0;

                    // Standard divider 9.75 representing active available hours in a standard 12H run (8 base + 1.75 overtime scaling)
                    long rrPagi = (long)((double)outPagi / 9.75);
                    long rrMalam = (long)((double)outMalam / 9.75);

                    dataTable.Rows.Add(
                        row.TanggalProduksi,
                        row.NamaMesin,
                        outPagi,
                        outMalam,
                        totalOut,
                        rrPagi,
                        rrMalam,
                        autoMin,
                        nyalaMin,
                        breakMin,
                        kanban,
                        material,
                        ganti,
                        lainnya,
                        eff.ToString("F1") + " %"
                    );
                }

                return dataTable;
            }
        }
    }
}
