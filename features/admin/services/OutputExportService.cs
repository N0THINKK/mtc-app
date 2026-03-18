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
                        DATE_FORMAT(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR), '%d %M %Y') AS 'Tanggal Produksi',
                        CONCAT(IFNULL(mt.type_name, ''), '.', IFNULL(ma.area_name, ''), '-', LPAD(m.machine_number, 2, '0')) AS 'Nama Mesin',
                        
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
                    WHERE mpl.created_at BETWEEN @StartDate AND @EndDate";

                if (area != "Semua Area") {
                    sql += " AND ma.area_name = @Area";
                }

                sql += @"
                    GROUP BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)), m.machine_id, mt.type_name, ma.area_name, m.machine_number
                    ORDER BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)) DESC, mt.type_name ASC, ma.area_name ASC, CAST(m.machine_number AS UNSIGNED) ASC";
                
                var reader = connection.ExecuteReader(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1), Area = area }, commandTimeout: 300);
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
    }
}
