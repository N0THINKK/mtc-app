using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.data.repositories
{
    public class TechnicianRepository : ITechnicianRepository
    {
        public IEnumerable<TicketDto> GetActiveTickets()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        t.ticket_id AS TicketId,
                        t.ticket_uuid AS TicketUuid,
                        CONCAT(m_type.type_name, '.', m_area.area_name, '-', m.machine_number) AS MachineName,
                        
                        (SELECT GROUP_CONCAT(
                            CONCAT(
                                IF(pt.type_name IS NOT NULL, CONCAT(pt.type_name, ': '), ''), 
                                IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown')),
                                IF(t.applicator_code IS NOT NULL AND t.applicator_code != '', CONCAT(' (App: ', t.applicator_code, ')'), '')
                            ) SEPARATOR ' | ')
                         FROM ticket_problems tp
                         LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                         LEFT JOIN failures f ON tp.failure_id = f.failure_id
                         WHERE tp.ticket_id = t.ticket_id
                        ) AS FailureDetails,

                        t.created_at AS CreatedAt,
                        t.status_id AS StatusId,
                        t.is_machine_running AS IsMachineRunning,
                        t.started_at AS StartedAt,
                        t.technician_finished_at AS FinishedAt,
                        t.gl_rating_score AS GlRatingScore,
                        t.gl_validated_at AS GlValidatedAt,
                        
                        (SELECT 
                            CASE 
                                WHEN COUNT(DISTINCT tts.technician_id) > 1 
                                THEN CONCAT(
                                    (SELECT u2.full_name FROM ticket_technician_sessions tts2 JOIN users u2 ON tts2.technician_id = u2.user_id WHERE tts2.ticket_id = t.ticket_id ORDER BY tts2.session_id ASC LIMIT 1), 
                                    ' + ', 
                                    (COUNT(DISTINCT tts.technician_id) - 1), 
                                    ' Others'
                                )
                                ELSE u.full_name 
                            END
                         FROM ticket_technician_sessions tts
                         WHERE tts.ticket_id = t.ticket_id
                        ) AS TechnicianName,
                        
                        t.arrival_elapsed_seconds AS ArrivalSeconds,
                        t.repair_elapsed_seconds AS RepairSeconds,
                        t.inspection_elapsed_seconds AS InspectionSeconds

                    FROM tickets t
                    JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN machine_types m_type ON m.type_id = m_type.type_id
                    LEFT JOIN machine_areas m_area ON m.area_id = m_area.area_id
                    LEFT JOIN users u ON t.technician_id = u.user_id
                    WHERE t.status_id >= 1
                    ORDER BY t.created_at DESC";
                
                return connection.Query<TicketDto>(sql);
            }
        }

        public async Task<TechnicianTicketDetailDto> GetTicketDetailAsync(long ticketId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        t.ticket_id AS TicketId,
                        CONCAT(m_type.type_name, '.', m_area.area_name, '-', m.machine_number) AS MachineName,
                        op.full_name AS OperatorName,
                        
                        (SELECT GROUP_CONCAT(DISTINCT u_tech.full_name SEPARATOR ', ')
                         FROM ticket_technician_sessions tts
                         JOIN users u_tech ON tts.technician_id = u_tech.user_id
                         WHERE tts.ticket_id = t.ticket_id
                        ) AS TechnicianName,
                        
                        (SELECT GROUP_CONCAT(
                            CONCAT(
                                IF(pt.type_name IS NOT NULL, CONCAT(pt.type_name, ': '), ''), 
                                IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown')),
                                IF(t.applicator_code IS NOT NULL AND t.applicator_code != '', CONCAT(' (App: ', t.applicator_code, ')'), '')
                            ) SEPARATOR ' | ')
                         FROM ticket_problems tp
                         LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                         LEFT JOIN failures f ON tp.failure_id = f.failure_id
                         WHERE tp.ticket_id = t.ticket_id
                        ) AS FailureDetails,
                        
                        (SELECT GROUP_CONCAT(
                            CONCAT(
                                IFNULL(act.action_name, IFNULL(tp.action_details_manual, '-')),
                                IF(tp.root_cause_remarks IS NOT NULL, CONCAT(' (Cause: ', tp.root_cause_remarks, ')'), '')
                            ) SEPARATOR ' | ')
                         FROM ticket_problems tp
                         LEFT JOIN actions act ON tp.action_id = act.action_id
                         WHERE tp.ticket_id = t.ticket_id
                        ) AS ActionDetails,

                        t.created_at AS CreatedAt,
                        t.started_at AS StartedAt,
                        t.technician_finished_at AS FinishedAt,
                        t.tech_rating_score AS TechRatingScore,
                        t.tech_rating_note AS TechRatingNote,
                        t.gl_rating_score AS GlRatingScore,
                        t.gl_rating_note AS GlRatingNote,

                        (SELECT GROUP_CONCAT(
                            CONCAT(COALESCE(p.part_name, pr.part_name_manual), ' x', pr.qty)
                            SEPARATOR ', ')
                         FROM part_requests pr
                         LEFT JOIN parts p ON pr.part_id = p.part_id
                         WHERE pr.ticket_id = t.ticket_id
                        ) AS SparepartRequests

                    FROM tickets t
                    JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN machine_types m_type ON m.type_id = m_type.type_id
                    LEFT JOIN machine_areas m_area ON m.area_id = m_area.area_id
                    LEFT JOIN users op ON t.operator_id = op.user_id
                    WHERE t.ticket_id = @TicketId";

                return await connection.QueryFirstOrDefaultAsync<TechnicianTicketDetailDto>(sql, new { TicketId = ticketId });
            }
        }

        public async Task UpdateOperatorRatingAsync(long ticketId, int rating, string note)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    UPDATE tickets 
                    SET tech_rating_score = @Rating, 
                        tech_rating_note = @Note
                    WHERE ticket_id = @TicketId";
                
                await connection.ExecuteAsync(sql, new { TicketId = ticketId, Rating = rating, Note = note });
            }
        }

        public TechnicianStatsDto GetTechnicianStatistics(long technicianId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        COUNT(DISTINCT t.ticket_id) AS CompletedRepairs,
                        COALESCE(AVG(CASE WHEN t.gl_rating_score > 0 THEN t.gl_rating_score END), 0) AS AverageRating,
                        COALESCE(SUM(CASE WHEN t.gl_rating_score > 0 THEN t.gl_rating_score ELSE 0 END), 0) AS TotalStars
                    FROM ticket_technician_sessions tts
                    JOIN tickets t ON tts.ticket_id = t.ticket_id
                    WHERE tts.technician_id = @TechnicianId
                      AND t.status_id = 3";
                
                return connection.QueryFirstOrDefault<TechnicianStatsDto>(sql, new { TechnicianId = technicianId });
            }
        }

        public async Task<IEnumerable<TechnicianPerformanceDto>> GetLeaderboardAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT 
                    T.TechnicianName,
                    COUNT(T.ticket_id) AS TotalRepairs,
                    COALESCE(AVG(NULLIF(T.gl_rating_score, 0)), 0) AS AverageRating,
                    COALESCE(SUM(T.gl_rating_score), 0) AS TotalStars
                FROM (
                    SELECT DISTINCT 
                        u.user_id, 
                        u.full_name AS TechnicianName, 
                        t.ticket_id, 
                        t.gl_rating_score
                    FROM ticket_technician_sessions tts
                    JOIN tickets t ON tts.ticket_id = t.ticket_id
                    JOIN users u ON tts.technician_id = u.user_id
                    WHERE t.status_id = 3 
                      AND t.created_at BETWEEN @Start AND @End
                ) AS T
                GROUP BY T.user_id, T.TechnicianName
                HAVING COUNT(T.ticket_id) > 0";

            using (var connection = DatabaseHelper.GetConnection())
            {
                var data = await connection.QueryAsync<TechnicianPerformanceDto>(sql, new { Start = start, End = end });
                return data;
            }
        }

        public async Task<IEnumerable<MachinePerformanceDto>> GetMachinePerformanceAsync(DateTime start, DateTime end, string area = null)
        {
            string sql = @"
                SELECT
                    CONCAT(mt.type_name, '.', ma.area_name, '-', m.machine_number) AS MachineName,
                    COUNT(t.ticket_id) AS RepairCount,
                    
                    SUM(TIMESTAMPDIFF(SECOND, t.created_at, t.production_resumed_at)) AS TotalDowntimeSeconds,
                    SUM(TIMESTAMPDIFF(SECOND, t.created_at, t.started_at)) AS ResponseDurationSeconds,
                    SUM(TIMESTAMPDIFF(SECOND, t.started_at, t.technician_finished_at)) AS RepairDurationSeconds,
                    SUM(TIMESTAMPDIFF(SECOND, t.technician_finished_at, t.production_resumed_at)) AS OperatorWaitDurationSeconds,

                    (SELECT COALESCE(SUM(TIMESTAMPDIFF(SECOND, pr.requested_at, pr.ready_at)), 0)
                     FROM part_requests pr 
                     JOIN tickets t_sub ON pr.ticket_id = t_sub.ticket_id
                     WHERE t_sub.machine_id = m.machine_id AND pr.ready_at IS NOT NULL
                    ) AS PartWaitDurationSeconds

                FROM machines m
                JOIN machine_types mt ON m.type_id = mt.type_id
                JOIN machine_areas ma ON m.area_id = ma.area_id
                JOIN tickets t ON m.machine_id = t.machine_id
                WHERE t.status_id = 3 
                  AND t.created_at BETWEEN @Start AND @End";

            if (!string.IsNullOrEmpty(area) && area != "All")
            {
                sql += " AND ma.area_name = @Area";
            }

            sql += @"
                GROUP BY m.machine_id, MachineName
                ORDER BY TotalDowntimeSeconds DESC;";

            using (var connection = DatabaseHelper.GetConnection())
            {
                var data = await connection.QueryAsync<MachinePerformanceDto>(sql, new { Start = start, End = end, Area = area });
                return data;
            }
        }

        // [BARU] Logika untuk menghitung a/b dari tabel log efisiensi
        public async Task<(int Running, int Total)> GetMachineRunStatsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        (SELECT COUNT(DISTINCT machine_id) 
                         FROM machine_process_logs 
                         WHERE created_at >= NOW() - INTERVAL 10 MINUTE) as Running,
                         
                        (SELECT COUNT(*) FROM machines) as Total
                ";
                
                return await connection.QueryFirstOrDefaultAsync<(int, int)>(sql);
            }
        }
    }
}