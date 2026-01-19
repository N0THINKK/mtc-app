using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.group_leader.data.dtos;

namespace mtc_app.features.group_leader.data.repositories
{
    public class GroupLeaderRepository : IGroupLeaderRepository
    {
        public async Task<IEnumerable<GroupLeaderTicketDto>> GetTicketsAsync(string statusFilter = null, string machineFilter = null)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        t.ticket_id AS TicketId,
                        CONCAT(m.machine_type, '-', m.machine_area, '.', m.machine_number) AS MachineName,
                        u.full_name AS TechnicianName,
                        t.gl_rating_score AS GlRatingScore,
                        t.created_at AS CreatedAt,
                        t.gl_validated_at AS GlValidatedAt
                    FROM tickets t
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN users u ON t.technician_id = u.user_id
                    WHERE t.status_id >= 2
                    ORDER BY t.created_at DESC";

                return await connection.QueryAsync<GroupLeaderTicketDto>(sql);
            }
        }

        public async Task ValidateTicketAsync(long ticketId, int rating, string note)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    UPDATE tickets 
                    SET gl_rating_score = @Rating, 
                        gl_rating_note = @Note,
                        gl_validated_at = NOW(),
                        status_id = 3
                    WHERE ticket_id = @TicketId";
                
                await connection.ExecuteAsync(sql, new { TicketId = ticketId, Rating = rating, Note = note });
            }
        }
    }
}
