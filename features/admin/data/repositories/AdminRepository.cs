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
                // Reordered columns based on supervisor request:
                // 1. Time Metrics (Lapor, Respon, Repair, Downtime)
                // 2. Main Info (Mesin, Teknisi, Masalah)
                // 3. Status & Identifiers (Status, Ticket Code, Operator)
                string sql = @"
                    SELECT 
                        t.created_at AS 'Waktu Lapor',
                        TIMEDIFF(t.started_at, t.created_at) AS 'Durasi Respon',
                        TIMEDIFF(t.technician_finished_at, t.started_at) AS 'Durasi Perbaikan',
                        TIMEDIFF(t.production_resumed_at, t.created_at) AS 'Total Downtime',
                        CONCAT(m.machine_type, '-', m.machine_area, '.', m.machine_number) AS 'Mesin',
                        IFNULL(tech.full_name, '-') AS 'Teknisi',
                        CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                            IFNULL(f.failure_name, IFNULL(t.failure_remarks, 'Unknown'))
                        ) AS 'Masalah',
                        ts.status_name AS 'Status',
                        t.ticket_display_code AS 'Kode Tiket',
                        u.full_name AS 'Operator'
                    FROM tickets t
                    LEFT JOIN ticket_statuses ts ON t.status_id = ts.status_id
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN users u ON t.operator_id = u.user_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
                    LEFT JOIN failures f ON t.failure_id = f.failure_id
                    ORDER BY t.created_at DESC
                    LIMIT 100;";

                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetReportDataAsync(DateTime start, DateTime end)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Reordered columns for Excel Report (Supervisor Request)
                string sql = @"
                    SELECT 
                        t.created_at AS 'Waktu Lapor',
                        TIMEDIFF(t.started_at, t.created_at) AS 'Durasi Respon',
                        TIMEDIFF(t.technician_finished_at, t.started_at) AS 'Durasi Perbaikan',
                        TIMEDIFF(t.production_resumed_at, t.technician_finished_at) AS 'Durasi Trial Run',
                        TIMEDIFF(t.production_resumed_at, t.created_at) AS 'Total Downtime',
                        CONCAT(m.machine_type, '-', m.machine_area, '.', m.machine_number) AS 'Mesin',
                        IFNULL(tech.full_name, '-') AS 'Teknisi',
                        CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                            IFNULL(f.failure_name, IFNULL(t.failure_remarks, 'Unknown'))
                        ) AS 'Masalah',
                        IFNULL(root.cause_name, t.root_cause_remarks) AS 'Penyebab',
                        IFNULL(act.action_name, t.action_details_manual) AS 'Tindakan',
                        ts.status_name AS 'Status',
                        t.ticket_display_code AS 'Kode Tiket',
                        u.full_name AS 'Operator'
                    FROM tickets t
                    JOIN ticket_statuses ts ON t.status_id = ts.status_id
                    JOIN machines m ON t.machine_id = m.machine_id
                    JOIN users u ON t.operator_id = u.user_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
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
