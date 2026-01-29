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
    }
}