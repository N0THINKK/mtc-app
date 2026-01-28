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
                // PERBAIKAN: Mengambil data failure/masalah dari tabel ticket_problems
                // Menggunakan subquery untuk memastikan 1 tiket tetap 1 baris meskipun ada multiple problems
                string sql = @"
                    SELECT 
                        t.ticket_id AS TicketId,
                        CONCAT(m.machine_type, '.', m.machine_area, '-', m.machine_number) AS MachineName,
                        
                        (SELECT CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                            IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown')),
                            IF(t.applicator_code IS NOT NULL AND t.applicator_code != '', CONCAT(' (App: ', t.applicator_code, ')'), '')
                         )
                         FROM ticket_problems tp
                         LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                         LEFT JOIN failures f ON tp.failure_id = f.failure_id
                         WHERE tp.ticket_id = t.ticket_id
                         LIMIT 1
                        ) AS FailureDetails,

                        t.created_at AS CreatedAt,
                        t.status_id AS StatusId,
                        t.started_at AS StartedAt,
                        t.technician_finished_at AS FinishedAt,
                        t.gl_rating_score AS GlRatingScore,
                        t.gl_validated_at AS GlValidatedAt,
                        u.full_name AS TechnicianName
                    FROM tickets t
                    JOIN machines m ON t.machine_id = m.machine_id
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
                // PERBAIKAN: Join ke ticket_problems (tp) untuk mengambil detail masalah dan tindakan
                // Menggunakan LIMIT 1 pada JOIN atau grouping logic agar tidak error jika ada multiple records
                // Namun untuk detail, biasanya kita ingin melihat masalah utamanya.
                
                string sql = @"
                    SELECT 
                        t.ticket_id AS TicketId,
                        CONCAT(m.machine_type, '.', m.machine_area, '-', m.machine_number) AS MachineName,
                        op.full_name AS OperatorName,
                        tech.full_name AS TechnicianName,
                        
                        CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                            IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown')),
                            IF(t.applicator_code IS NOT NULL AND t.applicator_code != '', CONCAT(' (App: ', t.applicator_code, ')'), '')
                        ) AS FailureDetails,
                        
                        CONCAT(
                            IFNULL(act.action_name, IFNULL(tp.action_details_manual, '-')),
                            IF(tp.root_cause_remarks IS NOT NULL, CONCAT(' (Cause: ', tp.root_cause_remarks, ')'), '')
                        ) AS ActionDetails,

                        t.created_at AS CreatedAt,
                        t.started_at AS StartedAt,
                        t.technician_finished_at AS FinishedAt,
                        t.tech_rating_score AS TechRatingScore,
                        t.tech_rating_note AS TechRatingNote,
                        t.gl_rating_score AS GlRatingScore,
                        t.gl_rating_note AS GlRatingNote
                    FROM tickets t
                    JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN users op ON t.operator_id = op.user_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    
                    -- JOIN KE TICKET_PROBLEMS
                    LEFT JOIN ticket_problems tp ON t.ticket_id = tp.ticket_id
                    LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                    LEFT JOIN failures f ON tp.failure_id = f.failure_id
                    LEFT JOIN actions act ON tp.action_id = act.action_id
                    
                    WHERE t.ticket_id = @TicketId
                    LIMIT 1"; // Pastikan hanya return 1 baris

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
                        COUNT(CASE WHEN status_id = 3 THEN 1 END) AS CompletedRepairs,
                        COALESCE(AVG(CASE WHEN gl_rating_score > 0 THEN gl_rating_score END), 0) AS AverageRating,
                        COALESCE(SUM(CASE WHEN gl_rating_score > 0 THEN gl_rating_score ELSE 0 END), 0) AS TotalStars
                    FROM tickets
                    WHERE technician_id = @TechnicianId";
                
                return connection.QueryFirstOrDefault<TechnicianStatsDto>(sql, new { TechnicianId = technicianId });
            }
        }

        public async Task<IEnumerable<TechnicianPerformanceDto>> GetLeaderboardAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT 
                    u.full_name AS TechnicianName,
                    COUNT(t.ticket_id) AS TotalRepairs,
                    AVG(t.gl_rating_score) AS AverageRating,
                    SUM(t.gl_rating_score) AS TotalStars
                FROM tickets t
                JOIN users u ON t.technician_id = u.user_id
                WHERE t.status_id = 3 
                  AND t.gl_validated_at IS NOT NULL
                  AND t.created_at BETWEEN @Start AND @End
                GROUP BY u.user_id, u.full_name
                HAVING COUNT(t.ticket_id) > 0";

            using (var connection = DatabaseHelper.GetConnection())
            {
                var data = await connection.QueryAsync<TechnicianPerformanceDto>(sql, new { Start = start, End = end });
                return data;
            }
        }

        public async Task<IEnumerable<MachinePerformanceDto>> GetMachinePerformanceAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT
                    CONCAT(m.machine_type, '.', m.machine_area, '-', m.machine_number) AS MachineName,
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
                JOIN tickets t ON m.machine_id = t.machine_id
                WHERE t.status_id = 3 
                  AND t.created_at BETWEEN @Start AND @End
                GROUP BY m.machine_id, MachineName
                ORDER BY TotalDowntimeSeconds DESC;
            ";

            using (var connection = DatabaseHelper.GetConnection())
            {
                var data = await connection.QueryAsync<MachinePerformanceDto>(sql, new { Start = start, End = end });
                return data;
            }
        }
    }
}