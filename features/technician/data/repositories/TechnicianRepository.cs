using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Added
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
                        CONCAT(m.machine_type, '.', m.machine_area, '-', m.machine_number) AS MachineName,
                        CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), 
                               IF(t.problem_type_remarks IS NOT NULL, CONCAT('[', t.problem_type_remarks, '] '), '')), 
                            IFNULL(f.failure_name, IFNULL(t.failure_remarks, 'Unknown')),
                            IF(t.applicator_code IS NOT NULL, CONCAT(' (App: ', t.applicator_code, ')'), '')
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
                    LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
                    LEFT JOIN failures f ON t.failure_id = f.failure_id
                    LEFT JOIN users u ON t.technician_id = u.user_id
                    WHERE t.status_id >= 1";
                
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
                        CONCAT(m.machine_type, '.', m.machine_area, '-', m.machine_number) AS MachineName,
                        op.full_name AS OperatorName,
                        tech.full_name AS TechnicianName,
                        CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), 
                               IF(t.problem_type_remarks IS NOT NULL, CONCAT('[', t.problem_type_remarks, '] '), '')), 
                            IFNULL(f.failure_name, IFNULL(t.failure_remarks, 'Unknown')),
                            IF(t.applicator_code IS NOT NULL, CONCAT(' (App: ', t.applicator_code, ')'), '')
                        ) AS FailureDetails,
                        CONCAT(
                            IFNULL(act.action_name, IFNULL(t.action_details_manual, '-')),
                            IF(t.root_cause_remarks IS NOT NULL, CONCAT(' (Cause: ', t.root_cause_remarks, ')'), '')
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
                    LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
                    LEFT JOIN failures f ON t.failure_id = f.failure_id
                    LEFT JOIN actions act ON t.action_id = act.action_id
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
                        COUNT(CASE WHEN status_id = 3 THEN 1 END) AS CompletedRepairs,
                        COALESCE(AVG(CASE WHEN gl_rating_score > 0 THEN gl_rating_score END), 0) AS AverageRating,
                        COALESCE(SUM(CASE WHEN gl_rating_score > 0 THEN gl_rating_score ELSE 0 END), 0) AS TotalStars
                    FROM tickets
                    WHERE technician_id = @TechnicianId";
                
                return connection.QueryFirstOrDefault<TechnicianStatsDto>(sql, new { TechnicianId = technicianId });
            }
        }

        public async Task<IEnumerable<TechnicianPerformanceDto>> GetLeaderboardAsync()
        {
            const string sql = @"
                SELECT 
                    u.full_name AS TechnicianName,
                    COUNT(t.ticket_id) AS TotalRepairs,
                    AVG(t.gl_rating_score) AS AverageRating,
                    SUM(t.gl_rating_score) AS TotalStars
                FROM tickets t
                JOIN users u ON t.technician_id = u.user_id
                WHERE t.status_id = 3 AND t.gl_validated_at IS NOT NULL
                GROUP BY u.user_id, u.full_name
                HAVING COUNT(t.ticket_id) > 0";

            using (var connection = DatabaseHelper.GetConnection())
            {
                var data = await connection.QueryAsync<TechnicianPerformanceDto>(sql);
                return data;
            }
        }

        public async Task<IEnumerable<MachinePerformanceDto>> GetMachinePerformanceAsync()
        {
            const string sql = @"
                SELECT
                    CONCAT(m.machine_type, '-', m.machine_area, '.', m.machine_number) AS MachineName,
                    COUNT(t.ticket_id) AS RepairCount,
                    
                    -- Calculate durations in seconds
                    SUM(TIMESTAMPDIFF(SECOND, t.created_at, t.production_resumed_at)) AS TotalDowntimeSeconds,
                    SUM(TIMESTAMPDIFF(SECOND, t.created_at, t.started_at)) AS ResponseDurationSeconds,
                    SUM(TIMESTAMPDIFF(SECOND, t.started_at, t.technician_finished_at)) AS RepairDurationSeconds,
                    SUM(TIMESTAMPDIFF(SECOND, t.technician_finished_at, t.production_resumed_at)) AS OperatorWaitDurationSeconds,

                    -- Subquery for total part wait time per machine
                    (SELECT COALESCE(SUM(TIMESTAMPDIFF(SECOND, pr.requested_at, pr.ready_at)), 0)
                     FROM part_requests pr 
                     JOIN tickets t_sub ON pr.ticket_id = t_sub.ticket_id
                     WHERE t_sub.machine_id = m.machine_id AND pr.ready_at IS NOT NULL
                    ) AS PartWaitDurationSeconds

                FROM machines m
                JOIN tickets t ON m.machine_id = t.machine_id
                WHERE t.status_id = 3 -- Only completed tickets
                GROUP BY m.machine_id, MachineName
                ORDER BY TotalDowntimeSeconds DESC;
            ";

            using (var connection = DatabaseHelper.GetConnection())
            {
                var data = await connection.QueryAsync<MachinePerformanceDto>(sql);
                return data;
            }
        }
    }
}

