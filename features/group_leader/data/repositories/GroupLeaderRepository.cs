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
                        t.ticket_uuid AS TicketUuid,
                        t.ticket_id AS TicketId,
                        CONCAT(m.machine_type, m.machine_area, m.machine_number) AS MachineName,
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

        public async Task<GroupLeaderTicketDetailDto> GetTicketDetailAsync(Guid ticketId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // NOTE: Querying by ticket_uuid since input is Guid
                string sql = @"
                    SELECT 
                        t.ticket_uuid AS TicketId, 
                        t.ticket_display_code AS TicketCode,
                        CONCAT(m.machine_type, m.machine_area, m.machine_number) AS MachineName,
                        tech.full_name AS TechnicianName,
                        op.full_name AS OperatorName,
                        CONCAT(
                            IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                            IFNULL(f.failure_name, IFNULL(t.failure_remarks, 'Unknown'))
                        ) AS FailureDetails,
                        CONCAT(
                            IFNULL(act.action_name, IFNULL(t.action_details_manual, '-')),
                            IF(t.root_cause_remarks IS NOT NULL, CONCAT(' (Cause: ', t.root_cause_remarks, ')'), '')
                        ) AS ActionDetails,
                        t.counter_stroke AS CounterStroke,
                        t.created_at AS CreatedAt,
                        t.started_at AS StartedAt,
                        t.technician_finished_at AS FinishedAt,
                        t.gl_rating_score AS GlRatingScore,
                        t.gl_rating_note AS GlRatingNote
                    FROM tickets t
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    LEFT JOIN users op ON t.operator_id = op.user_id
                    LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
                    LEFT JOIN failures f ON t.failure_id = f.failure_id
                    LEFT JOIN actions act ON t.action_id = act.action_id
                    WHERE t.ticket_uuid = @TicketId";

                return await connection.QueryFirstOrDefaultAsync<GroupLeaderTicketDetailDto>(sql, new { TicketId = ticketId.ToString() });
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

        public async Task ValidateTicketAsync(Guid ticketId, int rating, string note)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    UPDATE tickets 
                    SET gl_rating_score = @Rating, 
                        gl_rating_note = @Note,
                        gl_validated_at = NOW(),
                        status_id = 3
                    WHERE ticket_uuid = @TicketId";
                
                await connection.ExecuteAsync(sql, new { TicketId = ticketId.ToString(), Rating = rating, Note = note });
            }
        }
    }
}
