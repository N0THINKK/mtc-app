using System;
using System.Data;
using System.IO;
using Dapper;
using ClosedXML.Excel;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace mtc_auto_export
{
    class Program
    {
        static void Main(string[] args)
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
                    var dataOutputHarian = FetchDailyOutputSummary(connection, yesterday, yesterday, "Semua Area");

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

        private static DataTable FetchDailyOutputSummary(IDbConnection connection, DateTime startDate, DateTime endDate, string area)
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
