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
    public class DailyOutputDto
    {
        public string TanggalProduksi { get; set; }
        public string NamaMesin { get; set; }
        public long? OutputPagi { get; set; }
        public long? OutputMalam { get; set; }
        public long? RawAutoSec { get; set; }
        public long? RawMonitorSec { get; set; }
        public long? PlannedMin { get; set; }
        public long? SuddenMin { get; set; }
        public long? BreakMin { get; set; }
    }

    public static class OutputExportService
    {
        public static void ExportDataSetToExcel(DataSet dataSet, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                foreach (DataTable dt in dataSet.Tables)
                {
                    var ws = workbook.Worksheets.Add(dt.TableName);
                    ws.Cell("A1").InsertTable(dt);
                    ws.Row(1).Style.Font.Bold = true;
                    // Beri warna ungu tua untuk header agar lebih elegan
                    ws.Row(1).Style.Fill.BackgroundColor = XLColor.AirForceBlue; 
                    ws.Row(1).Style.Font.FontColor = XLColor.White;
                    ws.Columns().AdjustToContents();
                }

                workbook.SaveAs(filePath);
            }
        }

        public static async Task<DataSet> FetchDailyOutputSummaryAsync(DateTime startDate, DateTime endDate, string area)
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

                        IFNULL(act.PlannedMin, 0) AS PlannedMin,
                        IFNULL(act.SuddenMin, 0) AS SuddenMin,

                        IFNULL(brk.TotalBreakMin, 0) AS BreakMin
                    FROM machine_process_logs mpl
                    JOIN machines m ON mpl.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    LEFT JOIN (
                        SELECT 
                            moa.machine_id,
                            DATE(DATE_SUB(moa.start_time, INTERVAL 7 HOUR)) AS act_date,
                            SUM(CASE WHEN it.category IN ('Planned Stop', 'Berhenti Terencana') THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS PlannedMin,
                            SUM(CASE WHEN it.category IN ('Sudden Stop', 'Berhenti Tiba Tiba') THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS SuddenMin
                        FROM machine_operator_activities moa
                        LEFT JOIN activity_types it ON moa.activity_id = it.id
                        GROUP BY moa.machine_id, DATE(DATE_SUB(moa.start_time, INTERVAL 7 HOUR))
                    ) act ON act.machine_id = m.machine_id 
                            AND act.act_date = DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR))
                    LEFT JOIN (
                        SELECT day_id, SUM(non_ot_minutes + ot_minutes) AS TotalBreakMin
                        FROM shift_breaks
                        GROUP BY day_id
                    ) brk ON brk.day_id = (WEEKDAY(DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR))) + 1)
                    WHERE 1=1 AND mpl.created_at BETWEEN @StartDate AND @EndDate AND ma.area_name != 'Lain2'";

                if (area != "Semua Area") {
                    sql += " AND ma.area_name = @Area";
                }

                sql += @"
                    GROUP BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)), m.machine_id, mt.type_name, ma.area_name, m.machine_number, PlannedMin, SuddenMin, BreakMin
                    ORDER BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)) DESC, mt.type_name ASC, ma.area_name ASC, CAST(m.machine_number AS UNSIGNED) ASC";
                
                var results = await connection.QueryAsync<DailyOutputDto>(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1), Area = area }, commandTimeout: 300);
                
                var dataTable = new DataTable("Output Harian");
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
                dataTable.Columns.Add("Planned Stop (Menit)", typeof(long));
                dataTable.Columns.Add("Sudden Stop (Menit)", typeof(long));
                dataTable.Columns.Add("Efisiensi (%)", typeof(string));

                foreach (var row in results)
                {
                    long outPagi = row.OutputPagi ?? 0;
                    long outMalam = row.OutputMalam ?? 0;
                    long totalOut = outPagi + outMalam;
                    
                    long autoSec = row.RawAutoSec ?? 0;
                    long monSec = row.RawMonitorSec ?? 0;
                    
                    long autoMin = autoSec / 60;
                    long monMin = monSec / 60;
                    long nyalaMin = monMin > autoMin ? monMin - autoMin : 0; 
                    
                    long planned = row.PlannedMin ?? 0;
                    long sudden = row.SuddenMin ?? 0;
                    long breakMin = row.BreakMin ?? 0;

                    // Efisiensi = Mesin Run / (Mesin Run + Mesin Nyala + Planned + Sudden - Break)
                    double pembagiEfisiensi = autoMin + nyalaMin + planned + sudden - breakMin;
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
                        planned,
                        sudden,
                        eff.ToString("F1") + " %"
                    );
                }

                // --- DOWNTIME BREAKDOWN ---
                var dtDetails = new DataTable("Rincian Downtime Operator");
                dtDetails.Columns.Add("Tanggal Produksi", typeof(string));
                dtDetails.Columns.Add("Nama Mesin", typeof(string));

                var activities = await connection.QueryAsync("SELECT id, activity_name FROM activity_types ORDER BY id", commandTimeout: 30);
                var activityList = System.Linq.Enumerable.ToList(activities);
                foreach (var act in activityList)
                {
                    dtDetails.Columns.Add((string)act.activity_name, typeof(string));
                }

                string detailSql = @"
                SELECT 
                    DATE_FORMAT(DATE_SUB(moa.start_time, INTERVAL 7 HOUR), '%d %M %Y') AS TanggalProduksi,
                    CONCAT(IFNULL(mt.type_name, ''), '.', IFNULL(ma.area_name, ''), '-', LPAD(m.machine_number, 2, '0')) AS NamaMesin,
                    moa.activity_id AS ActivityId,
                    SUM(TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW()))) AS DurationMin
                FROM machine_operator_activities moa
                LEFT JOIN machines m ON moa.machine_id = m.machine_id
                LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                WHERE moa.start_time >= @StartDate AND moa.start_time < @EndDatePlusOne
                GROUP BY TanggalProduksi, NamaMesin, ActivityId";

                var detailResults = await connection.QueryAsync(detailSql, new { StartDate = startDate.Date, EndDatePlusOne = endDate.Date.AddDays(1) }, commandTimeout: 300);

                var detailDict = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<int, long>>();
                foreach (var dr in detailResults)
                {
                    string key = $"{(string)dr.TanggalProduksi}_{(string)dr.NamaMesin}";
                    if (!detailDict.ContainsKey(key)) detailDict[key] = new System.Collections.Generic.Dictionary<int, long>();
                    detailDict[key][(int)dr.ActivityId] = (long)Math.Max(0, (long)dr.DurationMin);
                }

                var machinesWithDowntime = new System.Collections.Generic.List<DailyOutputDto>();
                var machinesWithoutDowntime = new System.Collections.Generic.List<DailyOutputDto>();

                foreach (var row in results)
                {
                    if ((row.PlannedMin ?? 0) > 0 || (row.SuddenMin ?? 0) > 0)
                        machinesWithDowntime.Add(row);
                    else
                        machinesWithoutDowntime.Add(row);
                }

                foreach (var row in machinesWithDowntime)
                {
                    var dataRow = dtDetails.NewRow();
                    dataRow["Tanggal Produksi"] = row.TanggalProduksi;
                    dataRow["Nama Mesin"] = row.NamaMesin;
                    string key = $"{row.TanggalProduksi}_{row.NamaMesin}";
                    var acts = detailDict.ContainsKey(key) ? detailDict[key] : null;

                    foreach (var act in activityList)
                    {
                        long min = (acts != null && acts.ContainsKey((int)act.id)) ? acts[(int)act.id] : 0;
                        dataRow[(string)act.activity_name] = min.ToString();
                    }
                    dtDetails.Rows.Add(dataRow);
                }

                if (machinesWithoutDowntime.Count > 0)
                {
                    dtDetails.Rows.Add(dtDetails.NewRow()); // Gap row

                    foreach (var row in machinesWithoutDowntime)
                    {
                        var dataRow = dtDetails.NewRow();
                        dataRow["Tanggal Produksi"] = row.TanggalProduksi;
                        dataRow["Nama Mesin"] = row.NamaMesin;
                        bool first = true;
                        foreach (var act in activityList)
                        {
                            dataRow[(string)act.activity_name] = first ? "Jalan Terus" : "-";
                            first = false;
                        }
                        dtDetails.Rows.Add(dataRow);
                    }
                }

                var ds = new DataSet();
                ds.Tables.Add(dataTable);
                ds.Tables.Add(dtDetails);

                return ds;
            }
        }
    }
}
