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
                // PERBAIKAN: Mengambil data failure/masalah dari tabel ticket_problems
                // [FIX] Menggunakan GROUP_CONCAT untuk menampilkan SEMUA masalah.
                // [FIX] Menggunakan GROUP_CONCAT untuk menampilkan SEMUA teknisi (Concurrent Support)
                string sql = @"
                    SELECT 
                        t.ticket_uuid AS TicketUuid,
                        t.ticket_id AS TicketId,
                        CONCAT(mt.type_name, '.', ma.area_name, '-', m.machine_number) AS MachineName,
                        
                        -- [NEW] Aggregate Technician Names
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
                        
                        -- Subquery mengambil detail masalah dari ticket_problems (Multi-Problem Support)
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

                        t.gl_rating_score AS GlRatingScore,
                        t.created_at AS CreatedAt,
                        t.gl_validated_at AS GlValidatedAt,
                        t.status_id AS StatusId
                    FROM tickets t
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
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
                // PERBAIKAN: Menggunakan Subquery untuk FailureDetails dan ActionDetails
                string sql = @"
                    SELECT 
                        t.ticket_uuid AS TicketId, 
                        t.ticket_display_code AS TicketCode,
                        CONCAT(mt.type_name, '.', ma.area_name, '-', m.machine_number) AS MachineName,
                        
                        -- [FIXED] Aggregate Technician Names for Detail View (Comma Separated)
                        (SELECT GROUP_CONCAT(DISTINCT u_tech.full_name SEPARATOR ', ')
                         FROM ticket_technician_sessions tts
                         JOIN users u_tech ON tts.technician_id = u_tech.user_id
                         WHERE tts.ticket_id = t.ticket_id
                        ) AS TechnicianName,
                        
                        op.full_name AS OperatorName,
                        
                        -- Subquery FailureDetails (Multi-Problem Support)
                        (SELECT GROUP_CONCAT(
                            CONCAT(
                                IF(pt.type_name IS NOT NULL, CONCAT(pt.type_name, ': '), ''), 
                                IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown'))
                            ) SEPARATOR ' | ')
                         FROM ticket_problems tp
                         LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                         LEFT JOIN failures f ON tp.failure_id = f.failure_id
                         WHERE tp.ticket_id = t.ticket_id
                        ) AS FailureDetails,

                        -- Subquery ActionDetails (termasuk Root Cause) - Multi
                        (SELECT GROUP_CONCAT(
                            CONCAT(
                                IFNULL(act.action_name, IFNULL(tp.action_details_manual, '-')),
                                IF(tp.root_cause_remarks IS NOT NULL, CONCAT(' (Cause: ', tp.root_cause_remarks, ')'), '')
                            ) SEPARATOR ' | ')
                         FROM ticket_problems tp
                         LEFT JOIN actions act ON tp.action_id = act.action_id
                         WHERE tp.ticket_id = t.ticket_id
                        ) AS ActionDetails,

                        t.counter_stroke AS CounterStroke,
                        t.created_at AS CreatedAt,
                        t.started_at AS StartedAt,
                        t.technician_finished_at AS FinishedAt,
                        t.gl_rating_score AS GlRatingScore,
                        t.gl_rating_note AS GlRatingNote,
                        t.tech_rating_score AS TechRatingScore,
                        t.tech_rating_note AS TechRatingNote
                    FROM tickets t
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    LEFT JOIN users op ON t.operator_id = op.user_id
                    -- Join ke failures/actions dihapus dari query utama
                    WHERE t.ticket_uuid = @TicketId";

                return await connection.QueryFirstOrDefaultAsync<GroupLeaderTicketDetailDto>(sql, new { TicketId = ticketId.ToString() });
            }
        }

        public async Task ValidateTicketAsync(Guid ticketId, int rating, string note)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Tidak ada perubahan di sini karena hanya update status & rating
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