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
                // Tidak berubah
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
                // PERBAIKAN: Subquery untuk kolom 'Masalah'
                string sql = @"
                    SELECT 
                        t.created_at AS 'Waktu Lapor',
                        s.shift_name AS 'Shift',
                        TIMEDIFF(t.production_resumed_at, t.created_at) AS 'Total Downtime',
                        CONCAT(m.machine_type, '.', m.machine_area, '-', m.machine_number) AS 'Mesin',
                        
                        -- Subquery Masalah
                        (SELECT CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                            IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown'))
                         )
                         FROM ticket_problems tp
                         LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                         LEFT JOIN failures f ON tp.failure_id = f.failure_id
                         WHERE tp.ticket_id = t.ticket_id
                         LIMIT 1
                        ) AS 'Masalah',

                        IFNULL(tech.full_name, '-') AS 'Teknisi',
                        TIMEDIFF(t.started_at, t.created_at) AS 'Durasi Respon',
                        TIMEDIFF(t.technician_finished_at, t.started_at) AS 'Durasi Perbaikan',
                        
                        (SELECT SEC_TO_TIME(SUM(TIME_TO_SEC(TIMEDIFF(pr.ready_at, pr.requested_at)))) 
                         FROM part_requests pr 
                         WHERE pr.ticket_id = t.ticket_id AND pr.ready_at IS NOT NULL
                        ) AS 'Waktu Tunggu Part',

                        TIMEDIFF(t.production_resumed_at, t.technician_finished_at) AS 'Waktu Tunggu Operator',

                        ts.status_name AS 'Status',
                        t.ticket_display_code AS 'Kode Tiket',
                        u.full_name AS 'Operator'
                    FROM tickets t
                    LEFT JOIN ticket_statuses ts ON t.status_id = ts.status_id
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN users u ON t.operator_id = u.user_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    LEFT JOIN shifts s ON t.shift_id = s.shift_id
                    -- Join ke failures/problem_types dihapus
                    ORDER BY t.created_at DESC
                    LIMIT 100;";

                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetReportDataAsync(DateTime start, DateTime end)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // PERBAIKAN: Subquery untuk Masalah, Penyebab, dan Tindakan
                string sql = @"
                    SELECT 
                        t.created_at AS 'Waktu Lapor',
                        s.shift_name AS 'Shift',
                        TIMEDIFF(t.production_resumed_at, t.created_at) AS 'Total Downtime',
                        CONCAT(m.machine_type, '.', m.machine_area, '-', m.machine_number) AS 'Mesin',
                        
                        -- Subquery Masalah
                        (SELECT CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                            IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown'))
                         )
                         FROM ticket_problems tp
                         LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                         LEFT JOIN failures f ON tp.failure_id = f.failure_id
                         WHERE tp.ticket_id = t.ticket_id
                         LIMIT 1
                        ) AS 'Masalah',

                        IFNULL(tech.full_name, '-') AS 'Teknisi',
                        TIMEDIFF(t.started_at, t.created_at) AS 'Durasi Respon',
                        TIMEDIFF(t.technician_finished_at, t.started_at) AS 'Durasi Perbaikan',
                        
                        (SELECT SEC_TO_TIME(SUM(TIME_TO_SEC(TIMEDIFF(pr.ready_at, pr.requested_at)))) 
                         FROM part_requests pr 
                         WHERE pr.ticket_id = t.ticket_id AND pr.ready_at IS NOT NULL
                        ) AS 'Waktu Tunggu Part',

                        TIMEDIFF(t.production_resumed_at, t.technician_finished_at) AS 'Waktu Tunggu Operator',
                        
                        -- Subquery Penyebab (Root Cause)
                        (SELECT IFNULL(rc.cause_name, tp.root_cause_remarks)
                         FROM ticket_problems tp
                         LEFT JOIN failure_causes rc ON tp.root_cause_id = rc.cause_id
                         WHERE tp.ticket_id = t.ticket_id
                         LIMIT 1
                        ) AS 'Penyebab',

                        -- Subquery Tindakan (Action)
                        (SELECT IFNULL(act.action_name, tp.action_details_manual)
                         FROM ticket_problems tp
                         LEFT JOIN actions act ON tp.action_id = act.action_id
                         WHERE tp.ticket_id = t.ticket_id
                         LIMIT 1
                        ) AS 'Tindakan',

                        ts.status_name AS 'Status',
                        t.ticket_display_code AS 'Kode Tiket',
                        u.full_name AS 'Operator'
                    FROM tickets t
                    JOIN ticket_statuses ts ON t.status_id = ts.status_id
                    JOIN machines m ON t.machine_id = m.machine_id
                    JOIN users u ON t.operator_id = u.user_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    LEFT JOIN shifts s ON t.shift_id = s.shift_id
                    -- Join ke failures/actions/causes dihapus
                    WHERE t.created_at BETWEEN @StartDate AND @EndDate
                    ORDER BY t.created_at DESC";

                return await connection.QueryAsync(sql, new { StartDate = start, EndDate = end });
            }
        }
    }
}