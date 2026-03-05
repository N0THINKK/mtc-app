using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.admin.data.dtos;

namespace mtc_app.features.admin.data.repositories
{
    public class AdminRepository : IAdminRepository
    {
        // ... (Biarkan fungsi GetSummaryStatsAsync, GetMonitoringDataAsync, GetReportDataAsync, dan GetMonthlyLogsForExport tetap seperti aslinya. Jangan dihapus!)
        public async Task<AdminStatsDto> GetSummaryStatsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
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
                string sql = "SELECT * FROM view_admin_report ORDER BY `Waktu Lapor` DESC LIMIT 100";
                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetReportDataAsync(DateTime start, DateTime end)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = "SELECT * FROM view_admin_report WHERE `Waktu Lapor` BETWEEN @StartDate AND @EndDate ORDER BY `Waktu Lapor` DESC";
                return await connection.QueryAsync(sql, new { StartDate = start, EndDate = end });
            }
        }

        public IEnumerable<dynamic> GetMonthlyLogsForExport(int month, int year, string areaName)
        {
            DateTime startOfMonth = new DateTime(year, month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);
            var connection = DatabaseHelper.GetConnection(); 
            
            string sql = @"
                SELECT 
                    l.created_at AS Tanggal, CONCAT(mt.type_name, '.', ma.area_name, '-', m.machine_number) AS NamaMesin, ma.area_name AS Area,
                    CASE WHEN HOUR(l.created_at) >= 7 AND HOUR(l.created_at) < 15 THEN 'Shift 1' WHEN HOUR(l.created_at) >= 15 AND HOUR(l.created_at) < 23 THEN 'Shift 2' ELSE 'Shift 3' END AS Shift,
                    l.status_mesin AS Status, l.keterangan AS Keterangan
                FROM machine_process_logs l JOIN machines m ON l.machine_id = m.machine_id JOIN machine_areas ma ON m.area_id = ma.area_id JOIN machine_types mt ON m.type_id = mt.type_id
                WHERE l.created_at BETWEEN @Start AND @End";

            if (!string.IsNullOrEmpty(areaName) && areaName != "Semua Area") sql += " AND ma.area_name = @AreaName";
            sql += " ORDER BY l.created_at ASC";
            return connection.Query<dynamic>(sql, new { Start = startOfMonth, End = endOfMonth, AreaName = areaName }, buffered: false, commandTimeout: 300);
        }

        // ==========================================
        // DATA MASTER
        // ==========================================
        public async Task<IEnumerable<dynamic>> GetMasterUsersAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        user_id as id,
                        full_name as full_name,
                        CASE role_id 
                            WHEN 1 THEN 'Operator' 
                            WHEN 2 THEN 'Teknisi' 
                            WHEN 3 THEN 'Stock Control' 
                            WHEN 4 THEN 'Admin' 
                            WHEN 5 THEN 'Group Leader' 
                            ELSE 'Lainnya' 
                        END as role, 
                        nik as nik,
                        username as username
                    FROM users";
                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetMasterMachinesAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        m.machine_number as kode, 
                        mt.type_name as tipe, 
                        ma.area_name as area, 
                        ms.status_name as kondisi 
                    FROM machines m
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    LEFT JOIN machine_statuses ms ON m.current_status_id = ms.status_id
                    ORDER BY mt.type_name ASC, ma.area_name ASC, m.machine_number ASC";
                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetMasterSparepartsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        part_code as kode, 
                        part_name as nama, 
                        15 as stok, 
                        'Gudang Utama' as lokasi 
                    FROM parts";
                return await connection.QueryAsync(sql);
            }
        }

        // ==========================================
        // 4 TABEL KHUSUS PROBLEM
        // ==========================================
        public async Task<IEnumerable<dynamic>> GetMasterProblemTypesAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                return await connection.QueryAsync("SELECT type_id as id, type_name as nama FROM problem_types ORDER BY type_name ASC");
            }
        }

        public async Task<IEnumerable<dynamic>> GetMasterFailuresAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                return await connection.QueryAsync("SELECT failure_id as id, failure_name as nama FROM failures ORDER BY failure_name ASC");
            }
        }

        public async Task<IEnumerable<dynamic>> GetMasterCausesAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                return await connection.QueryAsync("SELECT cause_id as id, cause_name as nama FROM failure_causes ORDER BY cause_name ASC");
            }
        }

        public async Task<IEnumerable<dynamic>> GetMasterActionsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                return await connection.QueryAsync("SELECT action_id as id, action_name as nama FROM actions ORDER BY action_name ASC");
            }
        }
    }
}