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
        
        public long? OutputPagiNormal { get; set; }
        public long? OutputPagiOT { get; set; }
        public long? OutputMalamNormal { get; set; }
        public long? OutputMalamOT { get; set; }
        
        public long? RawAutoPagiNormal { get; set; }
        public long? RawAutoPagiOT { get; set; }
        public long? RawAutoMalamNormal { get; set; }
        public long? RawAutoMalamOT { get; set; }
        
        public long? RawMonitorPagiNormal { get; set; }
        public long? RawMonitorPagiOT { get; set; }
        public long? RawMonitorMalamNormal { get; set; }
        public long? RawMonitorMalamOT { get; set; }

        public long? PlannedPagiNormal { get; set; }
        public long? PlannedPagiOT { get; set; }
        public long? PlannedMalamNormal { get; set; }
        public long? PlannedMalamOT { get; set; }

        public long? SuddenPagiNormal { get; set; }
        public long? SuddenPagiOT { get; set; }
        public long? SuddenMalamNormal { get; set; }
        public long? SuddenMalamOT { get; set; }
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
                int? lain2Id = await connection.QueryFirstOrDefaultAsync<int?>("SELECT area_id FROM machine_areas WHERE area_name = 'Lain2'");
                string sql = @"
                    SELECT 
                        DATE_FORMAT(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR), '%d %M %Y') AS TanggalProduksi,
                        CONCAT(IFNULL(mt.type_name, ''), '.', IFNULL(ma.area_name, ''), '-', LPAD(m.machine_number, 2, '0')) AS NamaMesin,
                        
                        MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND (HOUR(mpl.created_at) < 16 OR (HOUR(mpl.created_at) = 16 AND MINUTE(mpl.created_at) < 30)) THEN mpl.produced_pieces ELSE 0 END) AS OutputPagiNormal,
                        MAX(CASE WHEN (HOUR(mpl.created_at) = 16 AND MINUTE(mpl.created_at) >= 30) OR HOUR(mpl.created_at) = 17 OR HOUR(mpl.created_at) = 18 THEN mpl.produced_pieces ELSE 0 END) AS OutputPagiOT,
                        MAX(CASE WHEN HOUR(mpl.created_at) >= 19 OR HOUR(mpl.created_at) < 4 OR (HOUR(mpl.created_at) = 4 AND MINUTE(mpl.created_at) < 30) THEN mpl.produced_pieces ELSE 0 END) AS OutputMalamNormal,
                        MAX(CASE WHEN (HOUR(mpl.created_at) = 4 AND MINUTE(mpl.created_at) >= 30) OR HOUR(mpl.created_at) = 5 OR HOUR(mpl.created_at) = 6 THEN mpl.produced_pieces ELSE 0 END) AS OutputMalamOT,

                        MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND (HOUR(mpl.created_at) < 16 OR (HOUR(mpl.created_at) = 16 AND MINUTE(mpl.created_at) < 30)) THEN mpl.auto_time ELSE 0 END) AS RawAutoPagiNormal,
                        MAX(CASE WHEN (HOUR(mpl.created_at) = 16 AND MINUTE(mpl.created_at) >= 30) OR HOUR(mpl.created_at) = 17 OR HOUR(mpl.created_at) = 18 THEN mpl.auto_time ELSE 0 END) AS RawAutoPagiOT,
                        MAX(CASE WHEN HOUR(mpl.created_at) >= 19 OR HOUR(mpl.created_at) < 4 OR (HOUR(mpl.created_at) = 4 AND MINUTE(mpl.created_at) < 30) THEN mpl.auto_time ELSE 0 END) AS RawAutoMalamNormal,
                        MAX(CASE WHEN (HOUR(mpl.created_at) = 4 AND MINUTE(mpl.created_at) >= 30) OR HOUR(mpl.created_at) = 5 OR HOUR(mpl.created_at) = 6 THEN mpl.auto_time ELSE 0 END) AS RawAutoMalamOT,

                        MAX(CASE WHEN HOUR(mpl.created_at) >= 7 AND (HOUR(mpl.created_at) < 16 OR (HOUR(mpl.created_at) = 16 AND MINUTE(mpl.created_at) < 30)) THEN mpl.monitor_time ELSE 0 END) AS RawMonitorPagiNormal,
                        MAX(CASE WHEN (HOUR(mpl.created_at) = 16 AND MINUTE(mpl.created_at) >= 30) OR HOUR(mpl.created_at) = 17 OR HOUR(mpl.created_at) = 18 THEN mpl.monitor_time ELSE 0 END) AS RawMonitorPagiOT,
                        MAX(CASE WHEN HOUR(mpl.created_at) >= 19 OR HOUR(mpl.created_at) < 4 OR (HOUR(mpl.created_at) = 4 AND MINUTE(mpl.created_at) < 30) THEN mpl.monitor_time ELSE 0 END) AS RawMonitorMalamNormal,
                        MAX(CASE WHEN (HOUR(mpl.created_at) = 4 AND MINUTE(mpl.created_at) >= 30) OR HOUR(mpl.created_at) = 5 OR HOUR(mpl.created_at) = 6 THEN mpl.monitor_time ELSE 0 END) AS RawMonitorMalamOT,

                        IFNULL(act.PlannedPagiNormal, 0) AS PlannedPagiNormal,
                        IFNULL(act.PlannedPagiOT, 0) AS PlannedPagiOT,
                        IFNULL(act.PlannedMalamNormal, 0) AS PlannedMalamNormal,
                        IFNULL(act.PlannedMalamOT, 0) AS PlannedMalamOT,

                        IFNULL(act.SuddenPagiNormal, 0) AS SuddenPagiNormal,
                        IFNULL(act.SuddenPagiOT, 0) AS SuddenPagiOT,
                        IFNULL(act.SuddenMalamNormal, 0) AS SuddenMalamNormal,
                        IFNULL(act.SuddenMalamOT, 0) AS SuddenMalamOT
                    FROM machine_process_logs mpl
                    JOIN machines m ON mpl.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    LEFT JOIN (
                        SELECT 
                            moa.machine_id,
                            DATE(DATE_SUB(moa.start_time, INTERVAL 7 HOUR)) AS act_date,
                            SUM(CASE WHEN it.category IN ('Planned Stop', 'Berhenti Terencana') AND (HOUR(moa.start_time) >= 7 AND (HOUR(moa.start_time) < 16 OR (HOUR(moa.start_time) = 16 AND MINUTE(moa.start_time) < 30))) THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS PlannedPagiNormal,
                            SUM(CASE WHEN it.category IN ('Planned Stop', 'Berhenti Terencana') AND ((HOUR(moa.start_time) = 16 AND MINUTE(moa.start_time) >= 30) OR HOUR(moa.start_time) = 17 OR HOUR(moa.start_time) = 18) THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS PlannedPagiOT,
                            SUM(CASE WHEN it.category IN ('Planned Stop', 'Berhenti Terencana') AND (HOUR(moa.start_time) >= 19 OR HOUR(moa.start_time) < 4 OR (HOUR(moa.start_time) = 4 AND MINUTE(moa.start_time) < 30)) THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS PlannedMalamNormal,
                            SUM(CASE WHEN it.category IN ('Planned Stop', 'Berhenti Terencana') AND ((HOUR(moa.start_time) = 4 AND MINUTE(moa.start_time) >= 30) OR HOUR(moa.start_time) = 5 OR HOUR(moa.start_time) = 6) THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS PlannedMalamOT,

                            SUM(CASE WHEN it.category IN ('Sudden Stop', 'Berhenti Tiba Tiba') AND (HOUR(moa.start_time) >= 7 AND (HOUR(moa.start_time) < 16 OR (HOUR(moa.start_time) = 16 AND MINUTE(moa.start_time) < 30))) THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS SuddenPagiNormal,
                            SUM(CASE WHEN it.category IN ('Sudden Stop', 'Berhenti Tiba Tiba') AND ((HOUR(moa.start_time) = 16 AND MINUTE(moa.start_time) >= 30) OR HOUR(moa.start_time) = 17 OR HOUR(moa.start_time) = 18) THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS SuddenPagiOT,
                            SUM(CASE WHEN it.category IN ('Sudden Stop', 'Berhenti Tiba Tiba') AND (HOUR(moa.start_time) >= 19 OR HOUR(moa.start_time) < 4 OR (HOUR(moa.start_time) = 4 AND MINUTE(moa.start_time) < 30)) THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS SuddenMalamNormal,
                            SUM(CASE WHEN it.category IN ('Sudden Stop', 'Berhenti Tiba Tiba') AND ((HOUR(moa.start_time) = 4 AND MINUTE(moa.start_time) >= 30) OR HOUR(moa.start_time) = 5 OR HOUR(moa.start_time) = 6) THEN TIMESTAMPDIFF(MINUTE, moa.start_time, IFNULL(moa.end_time, NOW())) ELSE 0 END) AS SuddenMalamOT
                        FROM machine_operator_activities moa
                        LEFT JOIN activity_types it ON moa.activity_id = it.id
                        WHERE moa.start_time >= @StartDate AND moa.start_time < @EndDatePlusOne
                        GROUP BY moa.machine_id, DATE(DATE_SUB(moa.start_time, INTERVAL 7 HOUR))
                    ) act ON act.machine_id = m.machine_id 
                            AND act.act_date = DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR))
                    WHERE 1=1 AND mpl.created_at BETWEEN @StartDate AND @EndDate";
                    
                if (lain2Id.HasValue) {
                    sql += " AND m.area_id != @Lain2Id";
                }

                if (area != "Semua Area") {
                    sql += " AND ma.area_name = @Area";
                }

                sql += @"
                    GROUP BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)), m.machine_id, mt.type_name, ma.area_name, m.machine_number, PlannedPagiNormal, PlannedPagiOT, PlannedMalamNormal, PlannedMalamOT, SuddenPagiNormal, SuddenPagiOT, SuddenMalamNormal, SuddenMalamOT
                    ORDER BY DATE(DATE_SUB(mpl.created_at, INTERVAL 7 HOUR)) DESC, mt.type_name ASC, ma.area_name ASC, CAST(m.machine_number AS UNSIGNED) ASC";
                
                var results = await connection.QueryAsync<DailyOutputDto>(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1), EndDatePlusOne = endDate.Date.AddDays(1), Area = area, Lain2Id = lain2Id }, commandTimeout: 300);

                
                var overrides = await connection.QueryAsync("SELECT override_date, shift_name, non_ot_minutes, ot_minutes FROM shift_break_overrides WHERE override_date BETWEEN @S AND @E", new { S = startDate.Date, E = endDate.Date.AddDays(1) });
                var regulars = await connection.QueryAsync("SELECT day_id, shift_name, non_ot_minutes, ot_minutes FROM shift_breaks");

                var overrideList = System.Linq.Enumerable.ToList(overrides);
                var regularList = System.Linq.Enumerable.ToList(regulars);

                long GetBreakMinutes(DateTime d, string shiftName, bool isOt)
                {
                    var ovr = overrideList.Find(o => ((DateTime)o.override_date).Date == d.Date && (string)o.shift_name == shiftName);
                    if (ovr != null) return isOt ? (long)ovr.ot_minutes : (long)ovr.non_ot_minutes;

                    int dId = (int)d.DayOfWeek;
                    if (dId == 0) dId = 7;
                    var reg = regularList.Find(r => (int)r.day_id == dId && (string)r.shift_name == shiftName);
                    if (reg != null) return isOt ? (long)reg.ot_minutes : (long)reg.non_ot_minutes;
                    return 0;
                }

                var dataTable = new DataTable("Output Harian");
                dataTable.Columns.Add("Tanggal Produksi", typeof(string));
                dataTable.Columns.Add("Nama Mesin", typeof(string));
                
                dataTable.Columns.Add("Output Pagi (Pcs)", typeof(long));
                dataTable.Columns.Add("Output OT Pagi (Pcs)", typeof(long));
                dataTable.Columns.Add("Output Malam (Pcs)", typeof(long));
                dataTable.Columns.Add("Output OT Malam (Pcs)", typeof(long));
                dataTable.Columns.Add("Total Output (Pcs)", typeof(long));
                
                dataTable.Columns.Add("Rata-rata / Jam Pagi Normal (Pcs)", typeof(long));
                dataTable.Columns.Add("Rata-rata / Jam Pagi OT (Pcs)", typeof(long));
                dataTable.Columns.Add("Rata-rata / Jam Malam Normal (Pcs)", typeof(long));
                dataTable.Columns.Add("Rata-rata / Jam Malam OT (Pcs)", typeof(long));
                
                dataTable.Columns.Add("Mesin Run Pagi Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Mesin Run Pagi OT (Menit)", typeof(long));
                dataTable.Columns.Add("Mesin Run Malam Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Mesin Run Malam OT (Menit)", typeof(long));
                
                dataTable.Columns.Add("Mesin Nyala Pagi Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Mesin Nyala Pagi OT (Menit)", typeof(long));
                dataTable.Columns.Add("Mesin Nyala Malam Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Mesin Nyala Malam OT (Menit)", typeof(long));
                
                dataTable.Columns.Add("Break Pagi Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Break Pagi OT (Menit)", typeof(long));
                dataTable.Columns.Add("Break Malam Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Break Malam OT (Menit)", typeof(long));
                
                dataTable.Columns.Add("Planned Pagi Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Planned Pagi OT (Menit)", typeof(long));
                dataTable.Columns.Add("Planned Malam Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Planned Malam OT (Menit)", typeof(long));

                dataTable.Columns.Add("Sudden Pagi Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Sudden Pagi OT (Menit)", typeof(long));
                dataTable.Columns.Add("Sudden Malam Normal (Menit)", typeof(long));
                dataTable.Columns.Add("Sudden Malam OT (Menit)", typeof(long));
                dataTable.Columns.Add("Efisiensi (%)", typeof(string));

                foreach (var row in results)
                {
                    DateTime parsedDate = DateTime.Parse(row.TanggalProduksi);
                    
                    long breakPagiN = GetBreakMinutes(parsedDate, "Shift 1", false);
                    long breakPagiOT = GetBreakMinutes(parsedDate, "Shift 1", true);
                    long breakMalamN = GetBreakMinutes(parsedDate, "Shift 2", false);
                    long breakMalamOT = GetBreakMinutes(parsedDate, "Shift 2", true);

                    long outPagiN = row.OutputPagiNormal ?? 0;
                    long maxPagiTotal = row.OutputPagiOT ?? 0;
                    long outPagiOT = Math.Max(0, maxPagiTotal - outPagiN);

                    long outMalamN = row.OutputMalamNormal ?? 0;
                    long maxMalamTotal = row.OutputMalamOT ?? 0;
                    long outMalamOT = Math.Max(0, maxMalamTotal - outMalamN);

                    long totalOut = outPagiN + outPagiOT + outMalamN + outMalamOT;

                    long autoPagiN = (row.RawAutoPagiNormal ?? 0) / 60;
                    long autoPagiOT = Math.Max(0, (row.RawAutoPagiOT ?? 0) / 60 - autoPagiN);
                    long autoMalamN = (row.RawAutoMalamNormal ?? 0) / 60;
                    long autoMalamOT = Math.Max(0, (row.RawAutoMalamOT ?? 0) / 60 - autoMalamN);

                    long monPagiN = (row.RawMonitorPagiNormal ?? 0) / 60;
                    long monPagiOT = Math.Max(0, (row.RawMonitorPagiOT ?? 0) / 60 - monPagiN);
                    long monMalamN = (row.RawMonitorMalamNormal ?? 0) / 60;
                    long monMalamOT = Math.Max(0, (row.RawMonitorMalamOT ?? 0) / 60 - monMalamN);

                    long nyalaPagiN = Math.Max(0, monPagiN - autoPagiN);
                    long nyalaPagiOT = Math.Max(0, monPagiOT - autoPagiOT);
                    long nyalaMalamN = Math.Max(0, monMalamN - autoMalamN);
                    long nyalaMalamOT = Math.Max(0, monMalamOT - autoMalamOT);

                    long planPN = row.PlannedPagiNormal ?? 0;
                    long planPOT = row.PlannedPagiOT ?? 0;
                    long planMN = row.PlannedMalamNormal ?? 0;
                    long planMOT = row.PlannedMalamOT ?? 0;

                    long sudPN = row.SuddenPagiNormal ?? 0;
                    long sudPOT = row.SuddenPagiOT ?? 0;
                    long sudMN = row.SuddenMalamNormal ?? 0;
                    long sudMOT = row.SuddenMalamOT ?? 0;

                    long rrPagiN = (long)((double)outPagiN / 9.5);
                    long rrPagiOT = (long)((double)outPagiOT / 2.5);
                    long rrMalamN = (long)((double)outMalamN / 9.5);
                    long rrMalamOT = (long)((double)outMalamOT / 2.5);

                    long totalRun = autoPagiN + autoPagiOT + autoMalamN + autoMalamOT;
                    long totalNyala = nyalaPagiN + nyalaPagiOT + nyalaMalamN + nyalaMalamOT;
                    long totalBreak = breakPagiN + breakPagiOT + breakMalamN + breakMalamOT;
                    long totalPlanned = planPN + planPOT + planMN + planMOT;
                    long totalSudden = sudPN + sudPOT + sudMN + sudMOT;

                    double pembagiEfisiensi = totalRun + totalNyala + totalPlanned + totalSudden - totalBreak;
                    double eff = pembagiEfisiensi > 0 ? ((double)totalRun / pembagiEfisiensi) * 100 : 0;

                    dataTable.Rows.Add(
                        row.TanggalProduksi,
                        row.NamaMesin,
                        outPagiN, outPagiOT, outMalamN, outMalamOT, totalOut,
                        rrPagiN, rrPagiOT, rrMalamN, rrMalamOT,
                        autoPagiN, autoPagiOT, autoMalamN, autoMalamOT,
                        nyalaPagiN, nyalaPagiOT, nyalaMalamN, nyalaMalamOT,
                        breakPagiN, breakPagiOT, breakMalamN, breakMalamOT,
                        planPN, planPOT, planMN, planMOT,
                        sudPN, sudPOT, sudMN, sudMOT,
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
                    long totalPlanned = (row.PlannedPagiNormal ?? 0) + (row.PlannedPagiOT ?? 0) + (row.PlannedMalamNormal ?? 0) + (row.PlannedMalamOT ?? 0);
                    long totalSudden = (row.SuddenPagiNormal ?? 0) + (row.SuddenPagiOT ?? 0) + (row.SuddenMalamNormal ?? 0) + (row.SuddenMalamOT ?? 0);
                    if (totalPlanned > 0 || totalSudden > 0)
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
