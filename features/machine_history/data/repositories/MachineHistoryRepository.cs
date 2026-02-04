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

        public async Task<(long TicketId, string TicketCode)> CreateTicketAsync(CreateTicketRequest request)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Generate Code
                        // Format: TKT-yyMMdd-XXX (Daily Sequence)
                        string uuid = Guid.NewGuid().ToString();
                        string dateCode = DateTime.Now.ToString("yyMMdd");
                        int dailyCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM tickets WHERE DATE(created_at) = CURDATE()", transaction: trans);
                        string displayCode = $"TKT-{dateCode}-{(dailyCount + 1):D3}";

                        // 2. Resolve IDs (Validation)
                        int operatorId = conn.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE nik = @Nik", new { Nik = request.OperatorNik }, trans) ?? 1;
                        int? shiftId = conn.QueryFirstOrDefault<int?>("SELECT shift_id FROM shifts WHERE shift_name = @Name", new { Name = request.ShiftName }, trans);

                        // Resolve Technician (if synced from offline verification)
                        int? techId = null;
                        if (!string.IsNullOrEmpty(request.TechnicianNik))
                        {
                            techId = conn.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE nik = @Nik", new { Nik = request.TechnicianNik }, trans);
                        }

                        // 3. Insert Ticket (Full State with ALL technician fields)
                        string insertTicketSql = @"
                            INSERT INTO tickets (
                                ticket_uuid, ticket_display_code, machine_id, shift_id, operator_id, applicator_code, 
                                status_id, technician_id, started_at, technician_finished_at, production_resumed_at,
                                counter_stroke, is_4m, tech_rating_score, tech_rating_note, created_at
                            )
                            VALUES (
                                @Uuid, @Code, @MachineId, @ShiftId, @OpId, @AppCode, 
                                @StatusId, @TechId, @Started, @Finished, @Resumed,
                                @Counter, @Is4M, @Rating, @RatingNote, NOW()
                            );
                            SELECT LAST_INSERT_ID();";

                        long ticketId = conn.ExecuteScalar<long>(insertTicketSql, new {
                            Uuid = uuid, 
                            Code = displayCode, 
                            MachineId = request.MachineId, 
                            ShiftId = shiftId, 
                            OpId = operatorId, 
                            AppCode = request.ApplicatorCode,
                            StatusId = request.StatusId,
                            TechId = techId,
                            Started = request.StartedAt,
                            Finished = request.FinishedAt,
                            Resumed = request.ProductionResumedAt,
                            Counter = request.CounterStroke,
                            Is4M = request.Is4M ? 1 : 0,
                            Rating = request.TechRatingScore,
                            RatingNote = request.TechRatingNote
                        }, trans);

                        // 4. Insert Problems (with Cause and Action)
                        string insertProblemSql = @"
                            INSERT INTO ticket_problems (ticket_id, problem_type_id, problem_type_remarks, failure_id, failure_remarks, root_cause_id, root_cause_remarks, action_id, action_details_manual)
                            VALUES (@TicketId, @TypeId, @TypeRem, @FailId, @FailRem, @CauseId, @CauseRem, @ActionId, @ActionRem)";

                        foreach (var prob in request.Problems)
                        {
                            int? typeId = conn.QueryFirstOrDefault<int?>("SELECT type_id FROM problem_types WHERE type_name = @N", new { N = prob.ProblemTypeName }, trans);
                            int? failId = conn.QueryFirstOrDefault<int?>("SELECT failure_id FROM failures WHERE failure_name = @N", new { N = prob.FailureName }, trans);
                            int? causeId = conn.QueryFirstOrDefault<int?>("SELECT cause_id FROM failure_causes WHERE cause_name = @N", new { N = prob.CauseName }, trans);
                            int? actionId = conn.QueryFirstOrDefault<int?>("SELECT action_id FROM actions WHERE action_name = @N", new { N = prob.ActionName }, trans);

                            conn.Execute(insertProblemSql, new {
                                TicketId = ticketId,
                                TypeId = typeId,
                                TypeRem = (!typeId.HasValue && !string.IsNullOrEmpty(prob.ProblemTypeName)) ? prob.ProblemTypeName : null,
                                FailId = failId,
                                FailRem = (!failId.HasValue && !string.IsNullOrEmpty(prob.FailureName)) ? prob.FailureName : null,
                                CauseId = causeId,
                                CauseRem = (!causeId.HasValue && !string.IsNullOrEmpty(prob.CauseName)) ? prob.CauseName : null,
                                ActionId = actionId,
                                ActionRem = (!actionId.HasValue && !string.IsNullOrEmpty(prob.ActionName)) ? prob.ActionName : null
                            }, trans);
                        }

                        // 5. Insert Sparepart Requests (if any)
                        if (request.SparepartRequests != null && request.SparepartRequests.Count > 0)
                        {
                            string insertPartSql = @"INSERT INTO part_requests (ticket_id, part_id, part_name_manual, qty, status_id, requested_at) VALUES (@TId, @PId, @Name, 1, 1, NOW())";
                            
                            foreach (var partName in request.SparepartRequests)
                            {
                                int? partId = null;
                                if (partName.Contains(" - "))
                                {
                                    var parts = partName.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length > 0)
                                        partId = conn.QueryFirstOrDefault<int?>("SELECT part_id FROM parts WHERE part_code = @C", new { C = parts[0].Trim() }, trans);
                                }
                                if (partId == null)
                                    partId = conn.QueryFirstOrDefault<int?>("SELECT part_id FROM parts WHERE part_name = @N", new { N = partName }, trans);
                                
                                conn.Execute(insertPartSql, new { TId = ticketId, PId = partId, Name = partName }, trans);
                            }
                        }

                        // 6. Update Machine Status based on final ticket status
                        int machineStatus = 2; // Default: Down/Repairing
                        if (request.StatusId >= 4) machineStatus = 1; // Production Resumed -> Running
                        else if (request.StatusId == 3) machineStatus = 3; // Completed -> Waiting validation
                        
                        conn.Execute("UPDATE machines SET current_status_id = @Status WHERE machine_id = @Id", new { Status = machineStatus, Id = request.MachineId }, trans);

                        trans.Commit();
                        return (ticketId, displayCode);
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}