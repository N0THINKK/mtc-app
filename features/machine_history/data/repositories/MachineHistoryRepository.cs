using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Dapper;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.data.repositories
{
    public class MachineHistoryRepository : IMachineHistoryRepository
    {
        public async Task<IEnumerable<MachineHistoryDto>> GetHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, string search = null, string areaFilter = null, int? machineId = null)
        {
            DateTime start = startDate ?? DateTime.Now.AddDays(-30);
            DateTime end = endDate ?? DateTime.Now.AddDays(1); 

            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        t.ticket_id AS TicketId,
                        t.ticket_uuid AS TicketUuid,
                        t.ticket_display_code AS TicketCode,
                        IFNULL(CONCAT(mt.type_name, '.', ma.area_name, '-', m.machine_number), 'Unknown') AS MachineName,
                        IFNULL(tech.full_name, '-') AS TechnicianName,
                        IFNULL(op.full_name, '-') AS OperatorName,
                        
                        (SELECT GROUP_CONCAT(
                            CONCAT(
                                IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                                IFNULL(f.failure_name, IFNULL(tp.failure_remarks, 'Unknown'))
                            ) SEPARATOR ' | '
                         )
                         FROM ticket_problems tp
                         LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                         LEFT JOIN failures f ON tp.failure_id = f.failure_id
                         WHERE tp.ticket_id = t.ticket_id
                        ) AS Issue,

                        (SELECT GROUP_CONCAT(
                            IFNULL(act.action_name, IFNULL(tp.action_details_manual, '-'))
                            SEPARATOR ' | '
                         )
                         FROM ticket_problems tp
                         LEFT JOIN actions act ON tp.action_id = act.action_id
                         WHERE tp.ticket_id = t.ticket_id
                        ) AS ActionDetails,

                        CONCAT_WS(', ',
                            (SELECT GROUP_CONCAT(
                                CONCAT(COALESCE(p.part_name, pr.part_name_manual), ' x', pr.qty)
                                SEPARATOR ', '
                             )
                             FROM part_requests pr
                             LEFT JOIN parts p ON pr.part_id = p.part_id
                             WHERE pr.ticket_id = t.ticket_id
                            ),
                            IF(t.counter_stroke > 0, CONCAT('Counter: ', t.counter_stroke), NULL)
                        ) AS SparepartUsed,

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
                        IFNULL(ts.status_name, 
                             CASE 
                                WHEN t.status_id = 1 THEN 'Open'
                                WHEN t.status_id = 2 THEN 'Repairing'
                                WHEN t.status_id = 3 THEN 'Done'
                                ELSE 'Unknown'
                             END
                        ) AS StatusName,

                        t.started_at AS StartedAt,
                        t.production_resumed_at AS ProductionResumedAt,
                        t.counter_stroke AS CounterStroke,
                        t.tech_rating_score AS TechRatingScore,
                        t.tech_rating_note AS TechRatingNote,
                        t.gl_rating_score AS GlRatingScore,
                        t.gl_rating_note AS GlRatingNote
                    FROM tickets t
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    LEFT JOIN users op ON t.operator_id = op.user_id
                    LEFT JOIN ticket_statuses ts ON t.status_id = ts.status_id
                    WHERE t.created_at >= @Start AND t.created_at < @End";

                var parameters = new DynamicParameters();
                parameters.Add("Start", start);
                parameters.Add("End", end);

                // Filter Area Spesifik
                if (!string.IsNullOrEmpty(areaFilter))
                {
                    sql += " AND ma.area_name = @Area";
                    parameters.Add("Area", areaFilter);
                }

                // Filter Mesin Spesifik
                if (machineId.HasValue)
                {
                    sql += " AND t.machine_id = @MachineId";
                    parameters.Add("MachineId", machineId.Value);
                }

                // Filter Pencarian Teks (Optional)
                if (!string.IsNullOrEmpty(search))
                {
                    sql += " AND CONCAT(IFNULL(mt.type_name,''), IFNULL(ma.area_name,''), IFNULL(m.machine_number,'')) LIKE @Search";
                    parameters.Add("Search", $"%{search}%");
                }

                sql += " ORDER BY t.created_at DESC";

                return await connection.QueryAsync<MachineHistoryDto>(sql, parameters);
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
                        string uuid = Guid.NewGuid().ToString();
                        string dateCode = DateTime.Now.ToString("yyMMdd");
                        int dailyCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM tickets WHERE DATE(created_at) = CURDATE()", transaction: trans);
                        string displayCode = $"TKT-{dateCode}-{(dailyCount + 1):D3}";

                        int operatorId = conn.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE nik = @Nik", new { Nik = request.OperatorNik }, trans) ?? 1;
                        int? shiftId = conn.QueryFirstOrDefault<int?>("SELECT shift_id FROM shifts WHERE shift_name = @Name", new { Name = request.ShiftName }, trans);

                        int? techId = null;
                        if (!string.IsNullOrEmpty(request.TechnicianNik))
                        {
                            techId = conn.QueryFirstOrDefault<int?>("SELECT user_id FROM users WHERE nik = @Nik", new { Nik = request.TechnicianNik }, trans);
                        }

                        // 1. Insert Header Ticket
                        string insertTicketSql = @"
                            INSERT INTO tickets (
                                ticket_uuid, ticket_display_code, machine_id, shift_id, operator_id, applicator_code, 
                                status_id, is_machine_running, technician_id, started_at, inspection_started_at, technician_finished_at, production_resumed_at,
                                counter_stroke, is_4m, tech_rating_score, tech_rating_note, gl_rating_score, gl_rating_note, 
                                arrival_elapsed_seconds, repair_elapsed_seconds, inspection_elapsed_seconds, run_elapsed_seconds, created_at
                            )
                            VALUES (
                                @Uuid, @Code, @MachineId, @ShiftId, @OpId, @AppCode, 
                                @StatusId, @IsRunning, @TechId, @Started, @Inspection, @Finished, @Resumed,
                                @Counter, @Is4M, @TechRating, @TechNote, @GlRating, @GlNote, 
                                @Arrival, @Repair, @Inspect, @RunElapsed, NOW()
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
                            IsRunning = request.IsMachineRunning,
                            TechId = techId,
                            Started = request.StartedAt,
                            Inspection = request.InspectionStartedAt,
                            Finished = request.FinishedAt,
                            Resumed = request.ProductionResumedAt,
                            Counter = request.CounterStroke,
                            Is4M = request.Is4M ? 1 : 0,
                            TechRating = request.TechRatingScore,
                            TechNote = request.TechRatingNote,
                            GlRating = request.GlRatingScore,
                            GlNote = request.GlRatingNote,
                            Arrival = request.ArrivalElapsedSeconds,
                            Repair = request.RepairElapsedSeconds,
                            Inspect = request.InspectionElapsedSeconds,
                            RunElapsed = request.RunElapsedSeconds
                        }, trans);

                        // 2. Insert Session Log (Jika ada teknisi)
                        if (techId.HasValue)
                        {
                            int elapsed = 0;
                            if (request.FinishedAt.HasValue && request.StartedAt.HasValue)
                            {
                                elapsed = (int)(request.FinishedAt.Value - request.StartedAt.Value).TotalSeconds;
                                if (elapsed < 0) elapsed = 0;
                            }
                            
                            int isCompleting = request.FinishedAt.HasValue ? 1 : 0;

                            string insertSessionSql = @"
                                INSERT INTO ticket_technician_sessions 
                                (ticket_id, technician_id, shift_id, started_at, ended_at, elapsed_seconds, is_completing_session)
                                VALUES (@TId, @TechId, @ShiftId, @Start, @End, @Elapsed, @IsComp)";

                            conn.Execute(insertSessionSql, new {
                                TId = ticketId,
                                TechId = techId.Value,
                                ShiftId = shiftId,
                                Start = request.StartedAt ?? DateTime.Now,
                                End = request.FinishedAt,
                                Elapsed = elapsed,
                                IsComp = isCompleting
                            }, trans);
                        }

                        // 3. Insert Problems (AUTO-LEARNING FITUR)
                        string insertProblemSql = @"
                            INSERT INTO ticket_problems (ticket_id, problem_type_id, problem_type_remarks, failure_id, failure_remarks, root_cause_id, root_cause_remarks, action_id, action_details_manual)
                            VALUES (@TicketId, @TypeId, @TypeRem, @FailId, @FailRem, @CauseId, @CauseRem, @ActionId, @ActionRem)";

                        foreach (var prob in request.Problems)
                        {
                            // A. JENIS PROBLEM (Auto Add ke tabel problem_types)
                            int? typeId = GetOrCreateMasterData(conn, trans, "problem_types", "type_id", "type_name", prob.ProblemTypeName);
                            
                            // B. DETAIL MASALAH (Auto Add ke tabel failures)
                            int? failId = GetOrCreateMasterData(conn, trans, "failures", "failure_id", "failure_name", prob.FailureName);
                            
                            // C. PENYEBAB (Auto Add ke tabel failure_causes)
                            int? causeId = GetOrCreateMasterData(conn, trans, "failure_causes", "cause_id", "cause_name", prob.CauseName);
                            
                            // D. TINDAKAN (Auto Add ke tabel actions)
                            int? actionId = GetOrCreateMasterData(conn, trans, "actions", "action_id", "action_name", prob.ActionName);

                            // Simpan ke Ticket Problem. 
                            // Remarks dikosongkan (NULL) karena data dipaksa masuk master via GetOrCreateMasterData.
                            conn.Execute(insertProblemSql, new {
                                TicketId = ticketId,
                                TypeId = typeId,
                                TypeRem = (string)null, 
                                FailId = failId,
                                FailRem = (string)null,
                                CauseId = causeId,
                                CauseRem = (string)null,
                                ActionId = actionId,
                                ActionRem = (string)null
                            }, trans);
                        }

                        // 4. Sparepart Requests
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

                        // 5. Update Status Mesin
                        int machineStatus = 2; // Default DOWN
                        if (request.IsMachineRunning == 1) machineStatus = 1; // Machine Running
                        else if (request.StatusId == 3) machineStatus = 1; // Completed -> Running
                        
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

        public async Task<MachineHistoryDto> GetActiveTicketForMachineAsync(int machineId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        t.ticket_id AS TicketId,
                        t.ticket_uuid AS TicketUuid,
                        t.ticket_display_code AS TicketCode,
                        IFNULL(CONCAT(mt.type_name, '.', ma.area_name, '-', m.machine_number), 'Unknown') AS MachineName,
                        IFNULL(tech.full_name, '-') AS TechnicianName,
                        IFNULL(op.full_name, '-') AS OperatorName,
                        
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

                        t.created_at AS CreatedAt,
                        t.started_at AS StartedAt,
                        t.technician_finished_at AS FinishedAt,
                        t.status_id AS StatusId,
                        IFNULL(ts.status_name, 'Unknown') AS StatusName
                    FROM tickets t
                    LEFT JOIN machines m ON t.machine_id = m.machine_id
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    LEFT JOIN users tech ON t.technician_id = tech.user_id
                    LEFT JOIN users op ON t.operator_id = op.user_id
                    LEFT JOIN ticket_statuses ts ON t.status_id = ts.status_id
                    WHERE t.machine_id = @MachineId AND t.status_id IN (1, 2)
                    ORDER BY t.created_at DESC
                    LIMIT 1";

                return await connection.QueryFirstOrDefaultAsync<MachineHistoryDto>(sql, new { MachineId = machineId });
            }
        }

        public async Task<DataTable> GetChecksheetHistoryPivotAsync(int machineId, int templateId, string roleTarget, int days = 30)
        {
            DataTable pivotTable = new DataTable();
            pivotTable.Columns.Add("Tanggal", typeof(string));

            using (var connection = DatabaseHelper.GetConnection())
            {
                // 1. Ambil data raw logs (hanya ambil X hari terakhir)
                DateTime startDate = DateTime.Now.Date.AddDays(-days);

                string rawDataSql = @"
                    SELECT 
                        DATE(l.patrol_date) AS PatrolDate,
                        l.shift AS Shift,
                        d.item_id AS ItemId,
                        d.status AS Status
                    FROM patrol_logs l
                    JOIN patrol_log_details d ON l.log_id = d.log_id
                    JOIN checksheet_items i ON d.item_id = i.item_id
                    WHERE l.machine_id = @MachId AND i.template_id = @TempId AND i.role_target = @RoleTarget AND l.patrol_date >= @Start
                    ORDER BY l.patrol_date ASC";

                var rawData = await connection.QueryAsync(rawDataSql, new { MachId = machineId, TempId = templateId, RoleTarget = roleTarget, Start = startDate });

                // 2. Tentukan Kolom (Berdasarkan Tanggal yang unik dari data yang sudah difilter)
                var rawDataList = rawData.ToList();
                var distinctDates = rawDataList.Select(r => {
                    string shiftName = r.Shift == "A" ? "Pagi" : (r.Shift == "B" ? "Malam" : r.Shift);
                    return $"{((DateTime)r.PatrolDate).ToString("dd/MM/yyyy")} ({shiftName})";
                }).Distinct().ToList();

                foreach (var dateStr in distinctDates)
                {
                    pivotTable.Columns.Add(dateStr, typeof(string));
                }

                // Jika tidak ada data raw, kembalikan tabel kosong sesuai format
                if (!distinctDates.Any()) return pivotTable;

                // 3. Ambil Item untuk dijadikan Baris
                string itemSql = "SELECT item_id, item_name, role_target FROM checksheet_items WHERE template_id = @TempId AND role_target = @RoleTarget ORDER BY item_id";
                var items = await connection.QueryAsync(itemSql, new { TempId = templateId, RoleTarget = roleTarget });

                int index = 1;
                foreach (var item in items)
                {
                    DataRow row = pivotTable.NewRow();
                    row["Tanggal"] = $"{index}. {item.item_name}";

                    int itemId = (int)item.item_id;

                    // 4. Isi cell dengan mencocokkan item ID dan Tanggal
                    foreach (var dateCol in distinctDates)
                    {
                        var matchingRecords = rawDataList.Where(r => {
                            string shiftName = r.Shift == "A" ? "Pagi" : (r.Shift == "B" ? "Malam" : r.Shift);
                            return (int)r.ItemId == itemId && $"{((DateTime)r.PatrolDate).ToString("dd/MM/yyyy")} ({shiftName})" == dateCol;
                        });
                        
                        if (matchingRecords.Any())
                        {
                            // Ambil record terakhir untuk cell ini
                            var lastRecord = matchingRecords.Last();
                            string status = lastRecord.Status?.ToString() ?? "";

                            if (status == "OK")
                            {
                                row[dateCol] = "OK";
                            }
                            else if (status == "NG" || status == "NOT_OK" || status == "PERBAIKAN_OK" || status == "NG_CARRYOVER")
                            {
                                row[dateCol] = "NG";
                            }
                            else if (status == "N/A")
                            {
                                row[dateCol] = "N/A";
                            }
                            else
                            {
                                // Nilai numerik atau teks lainnya — tampilkan apa adanya
                                row[dateCol] = status;
                            }
                        }
                        else
                        {
                            row[dateCol] = "N/A";
                        }
                    }

                    pivotTable.Rows.Add(row);
                    index++;
                }
            }

            return pivotTable;
        }

        public async Task<List<int>> GetPendingNgItemIdsAsync(int machineId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Mengambil item_id dari patrol_log_details yang berstatus NOT_OK atau NG
                // Berdasarkan log terakhir per item untuk mesin tertentu
                string sql = @"
                    SELECT DISTINCT d.item_id
                    FROM patrol_logs l
                    JOIN patrol_log_details d ON l.log_id = d.log_id
                    WHERE l.machine_id = @MachineId 
                      AND d.status IN ('NOT_OK', 'NG', 'NG_CARRYOVER')
                      -- Kita hanya ambil yang benar-benar belum selesai (is_ticket_created tidak menuntaskan masalah secara langsung tanpa tiket ditutup, tapi di sini kita cukup cek status detailnya)
                ";
                var result = await connection.QueryAsync<int>(sql, new { MachineId = machineId });
                return result.ToList();
            }
        }

        // =========================================================================================
        // HELPER FUNCTIONS (FORMATTING & AUTO-ADD TO MASTER)
        // =========================================================================================

        private int? GetOrCreateMasterData(IDbConnection conn, IDbTransaction trans, string tableName, string idCol, string nameCol, string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return null;

            // 1. FORMATTING INPUT (Huruf Besar Awal, Singkatan, dll)
            string formattedValue = FormatInputText(rawValue);

            // 2. Cek apakah sudah ada (pakai nilai yang sudah diformat)
            string checkSql = $"SELECT {idCol} FROM {tableName} WHERE {nameCol} = @Name";
            var existingId = conn.QueryFirstOrDefault<int?>(checkSql, new { Name = formattedValue }, trans);

            if (existingId.HasValue)
            {
                return existingId.Value; // Sudah ada, kembalikan ID lama
            }

            // 3. Insert Baru (pakai nilai yang sudah diformat)
            string insertSql = $"INSERT INTO {tableName} ({nameCol}) VALUES (@Name); SELECT LAST_INSERT_ID();";
            int newId = conn.ExecuteScalar<int>(insertSql, new { Name = formattedValue }, trans);

            return newId; // Kembalikan ID baru
        }

        // LOGIKA FORMATTING TEKS
        private string FormatInputText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // Split berdasarkan spasi
            var words = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];

                // Aturan 1: Pengecualian kata "aus" -> "Aus" (Tetap Title Case, bukan UPPER)
                if (word.Equals("aus", StringComparison.OrdinalIgnoreCase))
                {
                    words[i] = "Aus";
                }
                // Aturan 2: Singkatan 2-3 huruf -> UPPERCASE (contoh: PLC, NG, OK, MCB)
                else if (word.Length >= 2 && word.Length <= 3)
                {
                    words[i] = word.ToUpper();
                }
                // Aturan 3: Default -> Title Case (Ganti -> Ganti, sensor -> Sensor)
                else
                {
                    // Huruf pertama besar, sisanya kecil
                    words[i] = char.ToUpper(word[0]) + word.Substring(1).ToLower();
                }
            }

            return string.Join(" ", words);
        }
    }
}