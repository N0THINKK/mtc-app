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
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
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
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
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
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        t.technician_id AS TechnicianId,
                        u.full_name AS TechnicianName,
                        COUNT(CASE WHEN t.status_id = 3 THEN 1 END) AS TotalRepairs,
                        COALESCE(AVG(CASE WHEN t.gl_rating_score > 0 THEN t.gl_rating_score END), 0) AS AverageRating,
                        COALESCE(SUM(CASE WHEN t.gl_rating_score > 0 THEN t.gl_rating_score ELSE 0 END), 0) AS TotalStars
                    FROM tickets t
                    LEFT JOIN users u ON t.technician_id = u.user_id
                    WHERE t.technician_id IS NOT NULL
                    GROUP BY t.technician_id, u.full_name
                    ORDER BY TotalRepairs DESC";

                return await connection.QueryAsync<TechnicianPerformanceDto>(sql);
            }
        }
    }
}

