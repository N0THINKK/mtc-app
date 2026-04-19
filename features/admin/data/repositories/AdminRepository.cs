using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.admin.data.dtos;

namespace mtc_app.features.admin.data.repositories
{
    public class AdminRepository : IAdminRepository
    {
        // ... (Biarkan fungsi GetSummaryStatsAsync, GetMonitoringDataAsync, GetReportDataAsync, dan GetMonthlyLogsForExport tetap seperti aslinya. Jangan dihapus!)
        public async Task<AdminStatsDto> GetSummaryStatsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM users) as TotalUsers,
                        (SELECT COUNT(*) FROM machines) as TotalMachines,
                        (SELECT COUNT(*) FROM tickets WHERE status_id = 1) as OpenTickets,
                        (SELECT COUNT(*) FROM tickets WHERE status_id = 3 AND gl_validated_at IS NULL) as NeedValidation
                ";
                return await connection.QueryFirstOrDefaultAsync<AdminStatsDto>(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetMonitoringDataAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = "SELECT * FROM view_admin_report ORDER BY `Waktu Lapor` DESC LIMIT 100";
                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetReportDataAsync(DateTime start, DateTime end)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = "SELECT * FROM view_admin_report WHERE `Waktu Lapor` BETWEEN @StartDate AND @EndDate ORDER BY `Waktu Lapor` DESC";
                return await connection.QueryAsync(sql, new { StartDate = start, EndDate = end });
            }
        }

        public IEnumerable<dynamic> GetMonthlyLogsForExport(int month, int year, string areaName)
        {
            DateTime startOfMonth = new DateTime(year, month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);
            
            using (var connection = DatabaseHelper.GetConnection())
            {
                int? lain2Id = connection.QueryFirstOrDefault<int?>("SELECT area_id FROM machine_areas WHERE area_name = 'Lain2'");
                
                string sql = @"
                SELECT 
                    l.created_at AS Tanggal, CONCAT(mt.type_name, '.', ma.area_name, '-', m.machine_number) AS NamaMesin, ma.area_name AS Area,
                    CASE WHEN HOUR(l.created_at) >= 7 AND HOUR(l.created_at) < 15 THEN 'Shift 1' WHEN HOUR(l.created_at) >= 15 AND HOUR(l.created_at) < 23 THEN 'Shift 2' ELSE 'Shift 3' END AS Shift,
                    l.status_mesin AS Status, l.keterangan AS Keterangan
                FROM machine_process_logs l JOIN machines m ON l.machine_id = m.machine_id JOIN machine_areas ma ON m.area_id = ma.area_id JOIN machine_types mt ON m.type_id = mt.type_id
                WHERE l.created_at BETWEEN @Start AND @End";

            if (lain2Id.HasValue) sql += " AND m.area_id != @Lain2Id";
            if (!string.IsNullOrEmpty(areaName) && areaName != "Semua Area") sql += " AND ma.area_name = @AreaName";
            sql += " ORDER BY l.created_at ASC";
            return connection.Query<dynamic>(sql, new { Start = startOfMonth, End = endOfMonth, AreaName = areaName, Lain2Id = lain2Id }, buffered: false, commandTimeout: 300);
            }
        }

        // ==========================================
        // DATA MASTER
        // ==========================================
        public async Task<IEnumerable<dynamic>> GetMasterUsersAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        user_id as id,
                        full_name as full_name,
                        CASE role_id 
                            WHEN 1 THEN 'Operator' 
                            WHEN 2 THEN 'Teknisi' 
                            WHEN 3 THEN 'Stock Control' 
                            WHEN 4 THEN 'Admin' 
                            WHEN 5 THEN 'Group Leader' 
                            ELSE 'Lainnya' 
                        END as role, 
                        nik as nik,
                        username as username
                    FROM users
                    WHERE is_deleted = 0";
                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetMasterMachinesAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        m.machine_id as id,
                        m.machine_number as kode, 
                        mt.type_name as tipe, 
                        ma.area_name as area, 
                        ms.status_name as kondisi 
                    FROM machines m
                    LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                    LEFT JOIN machine_areas ma ON m.area_id = ma.area_id
                    LEFT JOIN machine_statuses ms ON m.current_status_id = ms.status_id
                    WHERE m.is_deleted = 0
                    ORDER BY mt.type_name ASC, ma.area_name ASC, m.machine_number ASC";
                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetMasterMachineAreasDataAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        area_id as id,
                        area_name as nama 
                    FROM machine_areas
                    WHERE area_name != 'Lain2'
                    ORDER BY area_name ASC";
                return await connection.QueryAsync(sql);
            }
        }

        public async Task<IEnumerable<dynamic>> GetMasterSparepartsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        part_id as id,
                        part_code as kode, 
                        part_name as nama, 
                        stock_qty as stok, 
                        'Gudang Utama' as lokasi 
                    FROM parts
                    WHERE is_deleted = 0
                    ORDER BY part_name ASC";
                return await connection.QueryAsync(sql);
            }
        }

        // ==========================================
        // 4 TABEL KHUSUS PROBLEM
        // ==========================================
        public async Task<IEnumerable<dynamic>> GetMasterProblemTypesAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
                return await connection.QueryAsync("SELECT type_id as id, type_name as nama FROM problem_types WHERE is_deleted = 0 ORDER BY type_name ASC");
        }

        public async Task<IEnumerable<dynamic>> GetMasterFailuresAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
                return await connection.QueryAsync("SELECT failure_id as id, failure_name as nama FROM failures WHERE is_deleted = 0 ORDER BY failure_name ASC");
        }

        public async Task<IEnumerable<dynamic>> GetMasterCausesAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
                return await connection.QueryAsync("SELECT cause_id as id, cause_name as nama FROM failure_causes WHERE is_deleted = 0 ORDER BY cause_name ASC");
        }

        public async Task<IEnumerable<dynamic>> GetMasterActionsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
                return await connection.QueryAsync("SELECT action_id as id, action_name as nama FROM actions WHERE is_deleted = 0 ORDER BY action_name ASC");
        }

        public async Task<IEnumerable<dynamic>> GetMasterChecksheetsAsync(string roleTarget)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        ci.item_id as id,
                        ci.role_target as role_target,
                        ct.template_name as tipe_mesin,
                        ci.item_name as item_pengecekan,
                        ci.standard_judgment as standar,
                        ci.check_method as metode,
                        ci.input_type as tipe_input
                    FROM checksheet_items ci
                    JOIN checksheet_templates ct ON ci.template_id = ct.template_id
                    WHERE ci.role_target = @roleTarget AND ci.is_deleted = 0
                    ORDER BY ct.template_name ASC, ci.item_id ASC";
                return await connection.QueryAsync(sql, new { roleTarget });
            }
        }

        public async Task<IEnumerable<string>> GetChecksheetTemplatesAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                return await connection.QueryAsync<string>("SELECT template_name FROM checksheet_templates ORDER BY template_name ASC");
            }
        }

        public async Task<IEnumerable<string>> GetMachineTypesAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                return await connection.QueryAsync<string>("SELECT type_name FROM machine_types ORDER BY type_name ASC");
            }
        }

        public async Task<IEnumerable<string>> GetMachineAreasAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                return await connection.QueryAsync<string>("SELECT area_name FROM machine_areas WHERE area_name != 'Lain2' ORDER BY area_name ASC");
            }
        }

        // ==========================================
        // EKSEKUSI CRUD (SIMPAN & HAPUS)
        // ==========================================
        // ==========================================
        // EKSEKUSI CRUD (SIMPAN & HAPUS)
        // ==========================================
        public async Task<bool> SaveMasterDataAsync(string category, string subCategory, bool isEdit, IDictionary<string, object> data)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                if (category == "User")
                {
                    // Konversi text Role kembali menjadi role_id
                    int roleId = data["role"].ToString() switch { "Operator" => 1, "Teknisi" => 2, "Stock Control" => 3, "Admin" => 4, "Group Leader" => 5, _ => 1 };
                    
                    if (isEdit) 
                    {
                        // Cek apakah user mengisi kolom "new_password" (ingin ganti password)
                        if (data.ContainsKey("new_password") && !string.IsNullOrWhiteSpace(data["new_password"]?.ToString()))
                        {
                            string oldPassInput = data.ContainsKey("old_password") ? data["old_password"]?.ToString() : "";
                            
                            // Tarik password yang sekarang aktif dari database
                            string currentPass = await connection.QueryFirstOrDefaultAsync<string>("SELECT password FROM users WHERE user_id=@id", new { id = data["id"] });
                            
                            // Validasi: Tolak jika password lama salah
                            if (currentPass != oldPassInput)
                            {
                                throw new Exception("Password lama yang Anda masukkan salah!");
                            }
                            
                            // Jika benar, Update semua termasuk password baru
                            string sql = "UPDATE users SET full_name=@f, nik=@n, username=@u, role_id=@r, password=@p WHERE user_id=@id";
                            return await connection.ExecuteAsync(sql, new { f = data["full_name"], n = data["nik"], u = data["username"], r = roleId, p = data["new_password"], id = data["id"] }) > 0;
                        }
                        else 
                        {
                            // Jika kolom password dikosongi, Update data profilnya saja
                            string sql = "UPDATE users SET full_name=@f, nik=@n, username=@u, role_id=@r WHERE user_id=@id";
                            return await connection.ExecuteAsync(sql, new { f = data["full_name"], n = data["nik"], u = data["username"], r = roleId, id = data["id"] }) > 0;
                        }
                    } 
                    else 
                    {
                        // Insert user baru (Gunakan password yang diisi, atau '123456' jika dibiarkan kosong)
                        string newPass = data.ContainsKey("new_password") && !string.IsNullOrWhiteSpace(data["new_password"]?.ToString()) ? data["new_password"].ToString() : "123456";
                        string sql = "INSERT INTO users (full_name, nik, username, role_id, password) VALUES (@f, @n, @u, @r, @p)";
                        return await connection.ExecuteAsync(sql, new { f = data["full_name"], n = data["nik"], u = data["username"], r = roleId, p = newPass }) > 0;
                    }
                }
                else if (category == "Problem")
                {
                    string table = subCategory == "Kategori Masalah" ? "problem_types" : subCategory == "Detail Problem" ? "failures" : subCategory == "Penyebab Problem" ? "failure_causes" : "actions";
                    string colId = subCategory == "Kategori Masalah" ? "type_id" : subCategory == "Detail Problem" ? "failure_id" : subCategory == "Penyebab Problem" ? "cause_id" : "action_id";
                    string colName = subCategory == "Kategori Masalah" ? "type_name" : subCategory == "Detail Problem" ? "failure_name" : subCategory == "Penyebab Problem" ? "cause_name" : "action_name";

                    if (isEdit) return await connection.ExecuteAsync($"UPDATE {table} SET {colName}=@nama WHERE {colId}=@id", new { nama = data["nama"], id = data["id"] }) > 0;
                    else return await connection.ExecuteAsync($"INSERT INTO {table} ({colName}) VALUES (@nama)", new { nama = data["nama"] }) > 0;
                }

                else if (category == "Checksheet")
                {
                    string targetRole = subCategory == "Checksheet Operator" ? "Operator" : "Teknisi";
                    string inputType = data.ContainsKey("tipe_input") && data["tipe_input"].ToString() == "Angka/Teks" ? "numeric/text" : "options";
                    
                    if (isEdit) {
                        string sql = "UPDATE checksheet_items SET item_name=@item, standard_judgment=@standar, check_method=@metode, input_type=@inputType, template_id=(SELECT template_id FROM checksheet_templates WHERE template_name=@tipe LIMIT 1) WHERE item_id=@id";
                        return await connection.ExecuteAsync(sql, new { item = data["item_pengecekan"], standar = data["standar"], metode = data["metode"], inputType = inputType, tipe = data["tipe_mesin"], id = data["id"] }) > 0;
                    } else {
                        string sql = "INSERT INTO checksheet_items (template_id, role_target, item_name, standard_judgment, check_method, input_type) VALUES ((SELECT template_id FROM checksheet_templates WHERE template_name=@tipe LIMIT 1), @targetRole, @item, @standar, @metode, @inputType)";
                        return await connection.ExecuteAsync(sql, new { targetRole, item = data["item_pengecekan"], standar = data["standar"], metode = data["metode"], inputType = inputType, tipe = data["tipe_mesin"] }) > 0;
                    }
                }
                else if (category == "Mesin")
                {
                    // Pastikan type dan area ada di master table
                    string typeName = data["nama"]?.ToString() ?? "Unknown";
                    int typeId = await GetOrCreateTypeId(connection, typeName);
                    
                    string areaName = data["area"]?.ToString() ?? "Unknown";
                    int areaId = await GetOrCreateAreaId(connection, areaName);
                    
                    // Kondisi default ke 1 (Running) saat pertama buat
                    if (isEdit) {
                        string sql = "UPDATE machines SET machine_number=@kode, type_id=@typeId, area_id=@areaId WHERE machine_id=@id";
                        return await connection.ExecuteAsync(sql, new { kode = data["kode"], typeId = typeId, areaId = areaId, id = data["id"] }) > 0;
                    } else {
                        string sql = "INSERT INTO machines (machine_number, type_id, area_id) VALUES (@kode, @typeId, @areaId)";
                        return await connection.ExecuteAsync(sql, new { kode = data["kode"], typeId = typeId, areaId = areaId }) > 0;
                    }
                }
                else if (category == "Area Mesin")
                {
                    if (isEdit) {
                        string sql = "UPDATE machine_areas SET area_name=@nama WHERE area_id=@id";
                        return await connection.ExecuteAsync(sql, new { nama = data["nama"], id = data["id"] }) > 0;
                    } else {
                        string sql = "INSERT INTO machine_areas (area_name) VALUES (@nama)";
                        return await connection.ExecuteAsync(sql, new { nama = data["nama"] }) > 0;
                    }
                }
                else if (category == "Sparepart")
                {
                    if (isEdit) {
                        string sql = "UPDATE parts SET part_code=@kode, part_name=@nama, stock_qty=@stok WHERE part_id=@id";
                        return await connection.ExecuteAsync(sql, new { kode = data["kode"], nama = data["nama"], stok = Convert.ToInt32(data["stok"] ?? 0), id = data["id"] }) > 0;
                    } else {
                        string sql = "INSERT INTO parts (part_code, part_name, stock_qty) VALUES (@kode, @nama, @stok)";
                        return await connection.ExecuteAsync(sql, new { kode = data["kode"], nama = data["nama"], stok = Convert.ToInt32(data["stok"] ?? 0) }) > 0;
                    }
                }
                
                return false;
            }
        }

        public async Task<bool> DeleteMasterDataAsync(string category, string subCategory, int id)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // KITA GANTI DELETE MENJADI UPDATE is_deleted = 1
                if (category == "User") 
                {
                    return await connection.ExecuteAsync("UPDATE users SET is_deleted = 1 WHERE user_id=@id", new { id }) > 0;
                }
                else if (category == "Problem") 
                {
                    string table = subCategory == "Kategori Masalah" ? "problem_types" : subCategory == "Detail Problem" ? "failures" : subCategory == "Penyebab Problem" ? "failure_causes" : "actions";
                    string colId = subCategory == "Kategori Masalah" ? "type_id" : subCategory == "Detail Problem" ? "failure_id" : subCategory == "Penyebab Problem" ? "cause_id" : "action_id";
                    
                    return await connection.ExecuteAsync($"UPDATE {table} SET is_deleted = 1 WHERE {colId}=@id", new { id }) > 0;
                }
                else if (category == "Checksheet")
                {
                    // Menghapus permanen karena items checksheet jarang terhubung ke histori log secara langsung
                    return await connection.ExecuteAsync("UPDATE checksheet_items SET is_deleted = 1 WHERE item_id=@id", new { id }) > 0;
                }
                else if (category == "Mesin")
                {
                    return await connection.ExecuteAsync("UPDATE machines SET is_deleted = 1 WHERE machine_id=@id", new { id }) > 0;
                }
                else if (category == "Area Mesin")
                {
                    return await connection.ExecuteAsync("DELETE FROM machine_areas WHERE area_id=@id", new { id }) > 0;
                }
                else if (category == "Sparepart")
                {
                    return await connection.ExecuteAsync("UPDATE parts SET is_deleted = 1 WHERE part_id=@id", new { id }) > 0;
                }
                return false;
            }
        }

        // ==========================================
        // HELPER FUNCTIONS UNTUK MESIN
        // ==========================================
        private async Task<int> GetOrCreateTypeId(System.Data.IDbConnection connection, string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return 0;
            string checkSql = "SELECT type_id FROM machine_types WHERE type_name = @name LIMIT 1";
            var existingId = await connection.QueryFirstOrDefaultAsync<int?>(checkSql, new { name = typeName });
            
            if (existingId.HasValue && existingId.Value > 0) return existingId.Value;

            string insertSql = "INSERT INTO machine_types (type_name) VALUES (@name); SELECT LAST_INSERT_ID();";
            return await connection.ExecuteScalarAsync<int>(insertSql, new { name = typeName });
        }

        private async Task<int> GetOrCreateAreaId(System.Data.IDbConnection connection, string areaName)
        {
            if (string.IsNullOrWhiteSpace(areaName)) return 0;
            string checkSql = "SELECT area_id FROM machine_areas WHERE area_name = @name LIMIT 1";
            var existingId = await connection.QueryFirstOrDefaultAsync<int?>(checkSql, new { name = areaName });
            
            if (existingId.HasValue && existingId.Value > 0) return existingId.Value;

            string insertSql = "INSERT INTO machine_areas (area_name) VALUES (@name); SELECT LAST_INSERT_ID();";
            return await connection.ExecuteScalarAsync<int>(insertSql, new { name = areaName });
        }

        // ==========================================
        // OUTPUT TARGET CRUD
        // ==========================================
        public async Task<IEnumerable<dynamic>> GetOutputTargetsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        ot.target_id AS id,
                        mt.type_name AS tipe_mesin,
                        ma.area_name AS area,
                        m.machine_number AS no_mesin,
                        ot.target_per_hour AS target_per_jam
                    FROM machine_output_targets ot
                    JOIN machines m ON ot.machine_id = m.machine_id
                    JOIN machine_types mt ON m.type_id = mt.type_id
                    JOIN machine_areas ma ON m.area_id = ma.area_id
                    ORDER BY mt.type_name, ma.area_name, m.machine_number";
                return await connection.QueryAsync(sql);
            }
        }

        public async Task<bool> SaveOutputTargetAsync(int? targetId, int typeId, int areaId, string machineNumber, int targetPerHour)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                int? machineId = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT machine_id FROM machines WHERE type_id = @typeId AND area_id = @areaId AND machine_number = @machineNumber AND is_deleted = 0",
                    new { typeId, areaId, machineNumber }
                );

                if (!machineId.HasValue)
                {
                    throw new Exception("Mesin dengan Tipe, Area, dan Nomor tersebut tidak ditemukan di Master Mesin. Harap daftarkan atau periksa kembali data mesin.");
                }

                if (targetId.HasValue && targetId.Value > 0)
                {
                    string sql = @"
                        UPDATE machine_output_targets 
                        SET machine_id = @machineId, target_per_hour = @target
                        WHERE target_id = @targetId";
                    return await connection.ExecuteAsync(sql, new { targetId = targetId.Value, machineId = machineId.Value, target = targetPerHour }) > 0;
                }
                else
                {
                    string sql = @"
                        INSERT INTO machine_output_targets (machine_id, target_per_hour)
                        VALUES (@machineId, @target)
                        ON DUPLICATE KEY UPDATE target_per_hour = @target";
                    return await connection.ExecuteAsync(sql, new { machineId = machineId.Value, target = targetPerHour }) > 0;
                }
            }
        }

        public async Task<bool> DeleteOutputTargetAsync(int targetId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                return await connection.ExecuteAsync("DELETE FROM machine_output_targets WHERE target_id = @id", new { id = targetId }) > 0;
            }
        }
        
        public async Task<IEnumerable<dynamic>> GetShiftBreaksAsync(string shiftName)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        sb.id AS id,
                        dw.day_name AS hari,
                        sb.non_ot_minutes AS non_ot_minutes,
                        sb.ot_minutes AS ot_minutes
                    FROM shift_breaks sb
                    JOIN days_of_week dw ON sb.day_id = dw.id
                    WHERE sb.shift_name = @shiftName
                    ORDER BY dw.id";
                return await connection.QueryAsync(sql, new { shiftName });
            }
        }

        public async Task<bool> SaveShiftBreakAsync(int? breakId, string shiftName, int dayId, int nonOtMinutes, int otMinutes)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                if (breakId.HasValue && breakId.Value > 0)
                {
                    string sql = @"
                        UPDATE shift_breaks 
                        SET non_ot_minutes = @nonOtMinutes, ot_minutes = @otMinutes
                        WHERE id = @breakId";
                    return await connection.ExecuteAsync(sql, new { breakId = breakId.Value, nonOtMinutes, otMinutes }) > 0;
                }
                else
                {
                    string sql = @"
                        INSERT INTO shift_breaks (shift_name, day_id, non_ot_minutes, ot_minutes)
                        VALUES (@shiftName, @dayId, @nonOtMinutes, @otMinutes)";
                    return await connection.ExecuteAsync(sql, new { shiftName, dayId, nonOtMinutes, otMinutes }) > 0;
                }
            }
        }

        public async Task<bool> DeleteShiftBreakAsync(int breakId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                return await connection.ExecuteAsync("DELETE FROM shift_breaks WHERE id = @id", new { id = breakId }) > 0;
            }
        }

        // ==========================================
        // PATROL NG & TICKETS MANAGEMENT
        // ==========================================
        public async Task<bool> DeleteTicketAsync(long ticketId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Eksekusi hapus di tabel-tabel anak terlebih dahulu untuk mencegah issue foreign key, lalu hapus tiket utamanya.
                string sql = @"
                    DELETE FROM ticket_problems WHERE ticket_id = @TicketId;
                    DELETE FROM ticket_technician_sessions WHERE ticket_id = @TicketId;
                    DELETE FROM part_requests WHERE ticket_id = @TicketId;
                    DELETE FROM tickets WHERE ticket_id = @TicketId;
                ";
                return await connection.ExecuteAsync(sql, new { TicketId = ticketId }) > 0;
            }
        }

        public async Task<bool> DeletePatrolNgAsync(int detailId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = "DELETE FROM patrol_log_details WHERE detail_id = @DetailId";
                return await connection.ExecuteAsync(sql, new { DetailId = detailId }) > 0;
            }
        }
    }
}