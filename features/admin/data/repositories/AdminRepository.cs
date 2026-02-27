using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.admin.data.dtos;

namespace mtc_app.features.admin.data.repositories
{
    public class AdminRepository : IAdminRepository
    {
        public async Task<AdminStatsDto> GetSummaryStatsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Query statistik tetap sama (ringan)
                string sql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM users) as TotalUsers,
                        (SELECT COUNT(*) FROM machines) as TotalMachines,
                        (SELECT COUNT(*) FROM tickets WHERE status_id = 1) as OpenTickets,
                        (SELECT COUNT(*) FROM tickets WHERE status_id = 3 AND gl_validated_at IS NULL) as NeedValidation
                ";
                
                return await connection.QueryFirstOrDefaultAsync<AdminStatsDto>(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetMonitoringDataAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // PERUBAHAN: Sekarang kita cukup panggil View yang sudah Anda buat di SQL.
                // View ini ('view_admin_report') sudah berisi logika GROUP_CONCAT yang "sakti".
                
                string sql = @"SELECT * FROM view_admin_report ORDER BY `Waktu Lapor` DESC LIMIT 100";

                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetReportDataAsync(DateTime start, DateTime end)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // PERUBAHAN: Panggil View dengan filter tanggal.
                // Kita filter berdasarkan kolom 'Waktu Lapor' yang ada di View.
                
                string sql = @"
                    SELECT * FROM view_admin_report 
                    WHERE `Waktu Lapor` BETWEEN @StartDate AND @EndDate 
                    ORDER BY `Waktu Lapor` DESC";

                return await connection.QueryAsync(sql, new { StartDate = start, EndDate = end });
            }
        }

                // Tambahkan di AdminRepository.cs
        public IEnumerable<dynamic> GetMonthlyLogsForExport(int month, int year, string areaName)
        {
            // Kita hitung awal dan akhir bulan di C# agar Index Database tetap bekerja maksimal!
            DateTime startOfMonth = new DateTime(year, month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1); // Menjadi tgl 30/31 jam 23:59:59

            var connection = DatabaseHelper.GetConnection(); // Tanpa 'using' karena stream dibiarkan mengalir
            
            string sql = @"
                SELECT 
                    l.created_at AS Tanggal,
                    CONCAT(mt.type_name, '.', ma.area_name, '-', m.machine_number) AS NamaMesin,
                    ma.area_name AS Area,
                    
                    -- Jika Shift sudah ada di tabel Anda (misal kolom 'shift'), panggil saja langsung:
                    -- l.shift AS Shift,
                    
                    -- TAPI jika di tabel belum ada kolom shift, kita bisa buat otomatis berdasarkan jam:
                    CASE 
                        WHEN HOUR(l.created_at) >= 7 AND HOUR(l.created_at) < 15 THEN 'Shift 1'
                        WHEN HOUR(l.created_at) >= 15 AND HOUR(l.created_at) < 23 THEN 'Shift 2'
                        ELSE 'Shift 3'
                    END AS Shift,
                    
                    l.status_mesin AS Status,
                    l.keterangan AS Keterangan
                FROM machine_process_logs l
                JOIN machines m ON l.machine_id = m.machine_id
                JOIN machine_areas ma ON m.area_id = ma.area_id
                JOIN machine_types mt ON m.type_id = mt.type_id
                WHERE l.created_at BETWEEN @Start AND @End";

            if (!string.IsNullOrEmpty(areaName) && areaName != "Semua Area")
            {
                sql += " AND ma.area_name = @AreaName";
            }

            sql += " ORDER BY l.created_at ASC";

            // KUNCI UTAMA: buffered: false agar RAM komputer Admin tidak jebol saat narik 1 juta baris!
            return connection.Query<dynamic>(sql, new { Start = startOfMonth, End = endOfMonth, AreaName = areaName }, buffered: false, commandTimeout: 300);
        }
    }
}