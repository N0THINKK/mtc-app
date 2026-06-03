using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.data.repositories
{
    public class TechnicianTicketRepository : ITechnicianTicketRepository
    {
        public async Task<long?> ResolveSyncedTicketIdAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<long?>(
                    "SELECT ticket_id FROM tickets WHERE status_id IN (1, 2, 3) ORDER BY created_at DESC LIMIT 1");
            }
        }

        public async Task<TicketStatusDto> LoadTicketStatusAsync(long ticketId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<TicketStatusDto>(@"
                    SELECT status_id AS StatusId, 
                           IFNULL(arrival_elapsed_seconds, 0) AS ArrivalSeconds,
                           IFNULL(repair_elapsed_seconds, 0) AS RepairSeconds,
                           IFNULL(inspection_elapsed_seconds, 0) AS InspectionSeconds,
                           IFNULL(is_machine_running, 0) AS IsMachineRunning
                    FROM tickets WHERE ticket_id = @Id", new { Id = ticketId });
            }
        }

        public async Task UpdateMachineRunningStateAsync(long ticketId, int state)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                await conn.ExecuteAsync(
                    "UPDATE tickets SET is_machine_running = @State WHERE ticket_id = @Id",
                    new { State = state, Id = ticketId });
            }
        }

        public async Task SaveTicketTimersAsync(long ticketId, int arrival, int repair, int inspect)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                await conn.ExecuteAsync(@"
                    UPDATE tickets 
                    SET arrival_elapsed_seconds = @Arrival, 
                        repair_elapsed_seconds = @Repair,
                        inspection_elapsed_seconds = @Inspect
                    WHERE ticket_id = @Id",
                    new { Arrival = arrival, Repair = repair, Inspect = inspect, Id = ticketId });
            }
        }

        public async Task<long> CreateTechnicianSessionAsync(long ticketId, int technicianId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO ticket_technician_sessions 
                    (ticket_id, technician_id, shift_id, started_at, elapsed_seconds, is_completing_session)
                    VALUES (@TicketId, @TechId, NULL, NOW(), 0, 0)",
                    new { TicketId = ticketId, TechId = technicianId });
                
                return await conn.QueryFirstOrDefaultAsync<long>("SELECT LAST_INSERT_ID()");
            }
        }

        public async Task SaveSessionElapsedAsync(long sessionId, int elapsedSeconds)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                await conn.ExecuteAsync(@"
                    UPDATE ticket_technician_sessions 
                    SET elapsed_seconds = @Elapsed, ended_at = NOW()
                    WHERE session_id = @Id",
                    new { Elapsed = elapsedSeconds, Id = sessionId });
            }
        }

        public async Task CompleteSessionAsync(long sessionId, int elapsedSeconds)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                await conn.ExecuteAsync(@"
                    UPDATE ticket_technician_sessions 
                    SET elapsed_seconds = @Elapsed, ended_at = NOW(), is_completing_session = 1
                    WHERE session_id = @Id",
                    new { Elapsed = elapsedSeconds, Id = sessionId });
            }
        }

        public async Task<List<TechnicianSessionDto>> LoadPreviousSessionsAsync(long ticketId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                var sessions = await conn.QueryAsync<TechnicianSessionDto>(@"
                    SELECT u.full_name AS TechName, 
                           tts.elapsed_seconds AS Elapsed,
                           tts.is_completing_session AS IsCompleting
                    FROM ticket_technician_sessions tts
                    JOIN users u ON tts.technician_id = u.user_id
                    WHERE tts.ticket_id = @Id
                    ORDER BY tts.started_at ASC",
                    new { Id = ticketId });
                return sessions.ToList();
            }
        }

        public async Task<List<TicketProblemDto>> LoadTicketProblemsAsync(long ticketId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                var problems = await conn.QueryAsync<TicketProblemDto>(@"
                    SELECT 
                        tp.problem_id AS ProblemId,
                        COALESCE(pt.type_name, tp.problem_type_remarks, '') AS ProblemType,
                        COALESCE(f.failure_name, tp.failure_remarks, '') AS ProblemDetail
                    FROM ticket_problems tp
                    LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                    LEFT JOIN failures f ON tp.failure_id = f.failure_id
                    WHERE tp.ticket_id = @Id", new { Id = ticketId });
                return problems.ToList();
            }
        }
    }
}
