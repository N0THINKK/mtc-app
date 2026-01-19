using System.Collections.Generic;
using System.Linq;
using Dapper;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.data.repositories
{
    public class TechnicianRepository
    {
        public IEnumerable<TicketDto> GetActiveTickets()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        t.ticket_id AS TicketId,
                        CONCAT(m.machine_type, m.machine_area, m.machine_number) AS MachineName,
                        CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                            IFNULL(f.failure_name, IFNULL(t.failure_remarks, 'Unknown')),
                            IF(t.applicator_code IS NOT NULL, CONCAT(' (App: ', t.applicator_code, ')'), '')
                        ) AS FailureDetails,
                        t.created_at AS CreatedAt,
                        t.status_id AS StatusId,
                        t.gl_rating_score AS GlRatingScore,
                        t.gl_validated_at AS GlValidatedAt
                    FROM tickets t
                    JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
                    LEFT JOIN failures f ON t.failure_id = f.failure_id
                    WHERE t.status_id >= 1";
                
                return connection.Query<TicketDto>(sql);
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
    }
}
