using System;
using System.Data;
using System.IO;
using Dapper;
using ClosedXML.Excel;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace mtc_auto_export
{
    public class DailyOutputDto
    {
        public string TanggalProduksi { get; set; }
        public string NamaMesin { get; set; }
        public long? OutputPagi { get; set; }
        public long? OutputMalam { get; set; }
        public long? RawAutoSec { get; set; }
        public long? RawMonitorSec { get; set; }
        public long? KanbanMin { get; set; }
        public long? MaterialMin { get; set; }
        public long? GantiMin { get; set; }
        public long? LainnyaMin { get; set; }
        public long? BreakMin { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Memulai Export Otomatis MTC...");
                
                // Cari appsettings.json di folder jalannya aplikasi ini, atau fallback ke path absolut saat .exe di-copy
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                
                var builder = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                
                var config = builder.Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    // Fallback to explicit if appsettings not found (for testing or if it's placed differently)
                    // You can change this or instruct admin to put appsettings.json next to the .exe
                    connectionString = "server=localhost;database=mtc_db;user=root;password=;"; // Ubah sesuai DB MTC kalian jika perlu
                    Console.WriteLine("Warning: appsettings.json tidak ditemukan atau connection string DefaultConnection kosong. Menggunakan fallback connection string.");
                }

                DateTime yesterday = DateTime.Now.Date.AddDays(-1);
                string directoryPath = @"D:\export output";
                
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string fileName = $"Output_Harian_SemuaArea_{yesterday:yyyy-MM-dd}.xlsx";
                string filePath = Path.Combine(directoryPath, fileName);

                if (File.Exists(filePath))
                {
                    Console.WriteLine($"Export dibatalkan. File untuk kemarin sudah ada: {filePath}");
                    return;
                }

                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Terkoneksi ke database. Mengambil data output...");
                    var dataOutputHarian = await FetchDailyOutputSummaryAsync(connection, yesterday, yesterday, "Semua Area");

                    if (dataOutputHarian.Rows.Count > 0)
                    {
                        ExportDataTableToExcel(dataOutputHarian, filePath);
                        Console.WriteLine($"[Sukses] Berhasil mengekspor output harian ke: {filePath}");
                    }
                    else
                    {
                        Console.WriteLine($"[Info] Tidak ada data output untuk diproses pada tanggal {yesterday:yyyy-MM-dd}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Terjadi kesalahan: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static async Task<DataTable> FetchDailyOutputSummaryAsync(IDbConnection connection, DateTime startDate, DateTime endDate, string area)
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
            
            var results = await connection.QueryAsync<DailyOutputDto>(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1), Area = area }, commandTimeout: 300);
            
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
                long outPagi = row.OutputPagi ?? 0;
                long outMalam = row.OutputMalam ?? 0;
                long totalOut = outPagi + outMalam;
                
                long autoSec = row.RawAutoSec ?? 0;
                long monSec = row.RawMonitorSec ?? 0;
                
                long autoMin = autoSec / 60;
                long monMin = monSec / 60;
                long nyalaMin = monMin > autoMin ? monMin - autoMin : 0; 
                
                long kanban = row.KanbanMin ?? 0;
                long material = row.MaterialMin ?? 0;
                long ganti = row.GantiMin ?? 0;
                long lainnya = row.LainnyaMin ?? 0;
                long breakMin = row.BreakMin ?? 0;

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

        private static void ExportDataTableToExcel(DataTable dataOutputHarian, string filePath)
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
    }
}
