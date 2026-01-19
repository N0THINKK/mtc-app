using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
                // We use multiple result sets or multiple simple queries. 
                // Since this is a dashboard summary, simple individual queries are readable and usually fast enough.
                // Or we can combine them into one SQL string.
                
                string sql = @"
                    SELECT COUNT(*) FROM users;
                    SELECT COUNT(*) FROM machines;
                    SELECT COUNT(*) FROM tickets WHERE status_id = 1; -- Open/Pending
                    SELECT COUNT(*) FROM tickets WHERE status_id = 3 AND (gl_validated_at IS NULL OR gl_rating_score IS NULL); -- Need GL Validation
                ";

                // Using generic Dapper MultiMap is complex for scalar counts.
                // Simple approach:
                // Note: Dapper Async ExecuteScalar is per query.
                
                // Optimized single query:
                string optimizedSql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM users) as TotalUsers,
                        (SELECT COUNT(*) FROM machines) as TotalMachines,
                        (SELECT COUNT(*) FROM tickets WHERE status_id = 1) as OpenTickets,
                        (SELECT COUNT(*) FROM tickets WHERE status_id = 3 AND gl_validated_at IS NULL) as NeedValidation
                ";
                
                return await connection.QueryFirstOrDefaultAsync<AdminStatsDto>(optimizedSql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetMonitoringDataAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Reusing the query from MonitoringView.cs
                // Optimized query to fetch relevant columns
                string sql = @"
                    SELECT 
                        t.ticket_display_code AS 'Kode Tiket',
                        ts.status_name AS 'Status',
                        m.machine_name AS 'Mesin',
                        u.full_name AS 'Operator',
                        IFNULL(tech.full_name, '-') AS 'Teknisi',
                        t.created_at AS 'Waktu Lapor',
                        t.started_at AS 'Mulai',
                        t.technician_finished_at AS 'Selesai',
                        TIMEDIFF(t.technician_finished_at, t.created_at) AS 'Total Downtime'
                    FROM tickets t
                    LEFT JOIN ticket_statuses ts ON t.status_id = ts.status_id
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN users u ON t.operator_id = u.user_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    ORDER BY t.created_at DESC
                    LIMIT 100;";

                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetReportDataAsync(DateTime start, DateTime end)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        t.ticket_display_code AS 'Kode Tiket',
                        ts.status_name AS 'Status',
                        m.machine_name AS 'Mesin',
                        u.full_name AS 'Operator',
                        t.created_at AS 'Waktu Lapor',
                        t.technician_finished_at AS 'Waktu Selesai',
                        IFNULL(f.failure_name, t.failure_remarks) AS 'Masalah',
                        IFNULL(root.cause_name, t.root_cause_remarks) AS 'Penyebab',
                        IFNULL(act.action_name, t.action_details_manual) AS 'Tindakan'
                    FROM tickets t
                    JOIN ticket_statuses ts ON t.status_id = ts.status_id
                    JOIN machines m ON t.machine_id = m.machine_id
                    JOIN users u ON t.operator_id = u.user_id
                    LEFT JOIN failures f ON t.failure_id = f.failure_id
                    LEFT JOIN failure_causes root ON t.root_cause_id = root.cause_id
                    LEFT JOIN actions act ON t.action_id = act.action_id
                    WHERE t.created_at BETWEEN @StartDate AND @EndDate
                    ORDER BY t.created_at DESC";

                return await connection.QueryAsync(sql, new { StartDate = start, EndDate = end });
            }
        }
    }
}
