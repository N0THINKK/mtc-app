using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.data.repositories
{
    public class MachineHistoryRepository : IMachineHistoryRepository
    {
        public async Task<IEnumerable<MachineHistoryDto>> GetHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, string machineFilter = null)
        {
            // Default to last 30 days if not specified
            DateTime start = startDate ?? DateTime.Now.AddDays(-30);
            DateTime end = endDate ?? DateTime.Now.AddDays(1); // Add 1 day to cover the full end date

            using (var connection = DatabaseHelper.GetConnection())
            {
                // PERBAIKAN: Menggunakan Subquery ke tabel ticket_problems (tp)
                // untuk mengambil detail Issue dan Resolution agar tidak error "Unknown Column"
                string sql = @"
                    SELECT 
                        t.ticket_id AS TicketId,
                        t.ticket_display_code AS TicketCode,
                        IFNULL(CONCAT(m.machine_type, '.', m.machine_area, '-', m.machine_number), 'Unknown') AS MachineName,
                        IFNULL(tech.full_name, '-') AS TechnicianName,
                        IFNULL(op.full_name, '-') AS OperatorName,
                        
                        -- Subquery untuk mengambil 'Issue' dari ticket_problems
                        (SELECT CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                            IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown'))
                         )
                         FROM ticket_problems tp
                         LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                         LEFT JOIN failures f ON tp.failure_id = f.failure_id
                         WHERE tp.ticket_id = t.ticket_id
                         LIMIT 1
                        ) AS Issue,

                        -- Subquery untuk mengambil 'Resolution' dari ticket_problems
                        (SELECT CONCAT(
                            IFNULL(act.action_name, IFNULL(tp.action_details_manual, '-')),
                            IF(tp.root_cause_remarks IS NOT NULL, CONCAT(' (Cause: ', tp.root_cause_remarks, ')'), '')
                         )
                         FROM ticket_problems tp
                         LEFT JOIN actions act ON tp.action_id = act.action_id
                         WHERE tp.ticket_id = t.ticket_id
                         LIMIT 1
                        ) AS Resolution,

                        t.created_at AS CreatedAt,
                        t.technician_finished_at AS FinishedAt,
                        t.status_id AS StatusId,
                        CASE 
                            WHEN t.status_id = 1 THEN 'Open'
                            WHEN t.status_id = 2 THEN 'Repairing'
                            WHEN t.status_id = 3 THEN 'Done'
                            ELSE 'Unknown'
                        END AS StatusName
                    FROM tickets t
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    LEFT JOIN users op ON t.operator_id = op.user_id
                    -- Note: Join ke failures/actions dihapus dari sini karena sudah pindah ke subquery
                    WHERE t.created_at >= @Start AND t.created_at < @End";

                if (!string.IsNullOrEmpty(machineFilter))
                {
                    sql += " AND CONCAT(m.machine_type, m.machine_area, m.machine_number) LIKE @Machine";
                    machineFilter = $"%{machineFilter}%";
                }

                sql += " ORDER BY t.created_at DESC";

                return await connection.QueryAsync<MachineHistoryDto>(sql, new { Start = start, End = end, Machine = machineFilter });
            }
        }
    }
}