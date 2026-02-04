using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Dapper;
using System.IO;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.shared.data.dtos;
using Newtonsoft.Json;

namespace mtc_app.shared.data.local
{
    /// <summary>
    /// Thread-safe repository for managing the local SQLite offline database.
    /// Implements Store-and-Forward pattern with caching for offline reads.
    /// </summary>
    public class OfflineRepository : IDisposable
    {
        private static readonly object _lock = new object();
        private readonly string _dbPath;
        private readonly string _connectionString;
        private bool _disposed;

        public const int MAX_RETRY_COUNT = 5;

        public OfflineRepository()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MTC_App"
            );
            Directory.CreateDirectory(appDataPath);
            
            _dbPath = Path.Combine(appDataPath, "offline.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    // SyncQueue table
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS SyncQueue (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ActionType TEXT NOT NULL,
                            TableName TEXT NOT NULL,
                            PayloadJson TEXT NOT NULL,
                            CreatedAt TEXT NOT NULL,
                            RetryCount INTEGER DEFAULT 0
                        );");
                    
                    // SyncDeadLetter table (for failed items after max retries)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS SyncDeadLetter (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ActionType TEXT NOT NULL,
                            TableName TEXT NOT NULL,
                            PayloadJson TEXT NOT NULL,
                            OriginalCreatedAt TEXT NOT NULL,
                            FailedAt TEXT NOT NULL,
                            ErrorMessage TEXT
                        );");
                    
                    // LocalCache table (generic key-value)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS LocalCache (
                            Key TEXT PRIMARY KEY,
                            ValueJson TEXT NOT NULL,
                            ExpiresAt TEXT NOT NULL
                        );");
                    
                    // CachedTickets table (for offline ticket reads)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS CachedTickets (
                            TicketId INTEGER PRIMARY KEY,
                            TicketUuid TEXT,
                            TicketCode TEXT,
                            MachineName TEXT,
                            StatusId INTEGER,
                            TechnicianName TEXT,
                            OperatorName TEXT,
                            CreatedAt TEXT,
                            DataJson TEXT,
                            CachedAt TEXT NOT NULL
                        );");
                    
                    // CachedMachineHistory table (last 90 days)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS CachedMachineHistory (
                            TicketId INTEGER PRIMARY KEY,
                            TicketCode TEXT,
                            MachineName TEXT,
                            TechnicianName TEXT,
                            OperatorName TEXT,
                            Issue TEXT,
                            Resolution TEXT,
                            StatusId INTEGER,
                            StatusName TEXT,
                            CreatedAt TEXT,
                            FinishedAt TEXT,
                            CachedAt TEXT NOT NULL
                        );");
                    
                    // CachedUsers table (for offline authentication)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS CachedUsers (
                            UserId INTEGER PRIMARY KEY,
                            Username TEXT NOT NULL UNIQUE,
                            PasswordHash TEXT NOT NULL,
                            FullName TEXT,
                            Nik TEXT,
                            RoleId INTEGER,
                            RoleName TEXT,
                            IsActive INTEGER DEFAULT 1,
                            LastSyncedAt TEXT NOT NULL
                        );");
                    
                    // CachedMachines table (for offline dropdowns)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS CachedMachines (
                            MachineId INTEGER PRIMARY KEY,
                            Code TEXT NOT NULL,
                            MachineType TEXT,
                            MachineArea TEXT,
                            MachineNumber TEXT,
                            StatusId INTEGER DEFAULT 1,
                            CachedAt TEXT NOT NULL
                        );");
                    
                    // CachedShifts table (for offline dropdowns)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS CachedShifts (
                            ShiftId INTEGER PRIMARY KEY,
                            ShiftName TEXT NOT NULL,
                            CachedAt TEXT NOT NULL
                        );");
                    
                    // CachedProblemTypes table (for offline dropdowns)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS CachedProblemTypes (
                            TypeId INTEGER PRIMARY KEY,
                            TypeName TEXT NOT NULL,
                            CachedAt TEXT NOT NULL
                        );");
                        
                     // CachedFailures table (for offline dropdowns)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS CachedFailures (
                            FailureId INTEGER PRIMARY KEY,
                            FailureName TEXT NOT NULL,
                            CachedAt TEXT NOT NULL
                        );");

                    // CachedCauses table (NEW)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS CachedCauses (
                            CauseId INTEGER PRIMARY KEY,
                            CauseName TEXT NOT NULL,
                            CachedAt TEXT NOT NULL
                        );");

                    // CachedActions table (NEW)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS CachedActions (
                            ActionId INTEGER PRIMARY KEY,
                            ActionName TEXT NOT NULL,
                            CachedAt TEXT NOT NULL
                        );");

                     // PendingTickets table (for offline writes)
                    ExecuteNonQuery(connection, @"
                        CREATE TABLE IF NOT EXISTS PendingTickets (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            TicketData TEXT NOT NULL,
                            CreatedAt TEXT NOT NULL,
                            IsSynced INTEGER DEFAULT 0
                        );");
                }
            }
        }

        private void ExecuteNonQuery(SQLiteConnection conn, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        #region Sync Queue Operations

        public void AddToQueue(string actionType, string tableName, object payload)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var sql = @"
                        INSERT INTO SyncQueue (ActionType, TableName, PayloadJson, CreatedAt, RetryCount)
                        VALUES (@ActionType, @TableName, @PayloadJson, @CreatedAt, 0);";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ActionType", actionType);
                        cmd.Parameters.AddWithValue("@TableName", tableName);
                        cmd.Parameters.AddWithValue("@PayloadJson", JsonConvert.SerializeObject(payload));
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<SyncQueueItem> GetPendingItems()
        {
            lock (_lock)
            {
                var items = new List<SyncQueueItem>();
                
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var sql = "SELECT * FROM SyncQueue ORDER BY CreatedAt ASC;";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new SyncQueueItem
                            {
                                Id = (long)reader["Id"],
                                ActionType = reader["ActionType"].ToString(),
                                TableName = reader["TableName"].ToString(),
                                PayloadJson = reader["PayloadJson"].ToString(),
                                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                                RetryCount = Convert.ToInt32(reader["RetryCount"])
                            });
                        }
                    }
                }
                
                return items;
            }
        }

        public void RemoveFromQueue(long id)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM SyncQueue WHERE Id = @Id;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void IncrementRetryCount(long id)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("UPDATE SyncQueue SET RetryCount = RetryCount + 1 WHERE Id = @Id;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public int GetQueueCount()
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM SyncQueue;", connection))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
        }

        #endregion

        #region Dead Letter Queue Operations

        /// <summary>
        /// Moves a failed item to the dead letter queue.
        /// </summary>
        public void MoveToDeadLetter(SyncQueueItem item, string errorMessage)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Insert into dead letter
                    var insertSql = @"
                        INSERT INTO SyncDeadLetter (ActionType, TableName, PayloadJson, OriginalCreatedAt, FailedAt, ErrorMessage)
                        VALUES (@ActionType, @TableName, @PayloadJson, @OriginalCreatedAt, @FailedAt, @ErrorMessage);";
                    
                    using (var cmd = new SQLiteCommand(insertSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ActionType", item.ActionType);
                        cmd.Parameters.AddWithValue("@TableName", item.TableName);
                        cmd.Parameters.AddWithValue("@PayloadJson", item.PayloadJson);
                        cmd.Parameters.AddWithValue("@OriginalCreatedAt", item.CreatedAt.ToString("o"));
                        cmd.Parameters.AddWithValue("@FailedAt", DateTime.UtcNow.ToString("o"));
                        cmd.Parameters.AddWithValue("@ErrorMessage", errorMessage ?? "Max retries exceeded");
                        cmd.ExecuteNonQuery();
                    }
                    
                    // Remove from queue
                    using (var cmd = new SQLiteCommand("DELETE FROM SyncQueue WHERE Id = @Id;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", item.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Gets count of items in dead letter queue.
        /// </summary>
        public int GetDeadLetterCount()
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM SyncDeadLetter;", connection))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
        }

        #endregion

        #region Ticket Cache Operations

        /// <summary>
        /// Saves tickets to local cache for offline access.
        /// </summary>
        public void SaveTicketsToCache<T>(IEnumerable<T> tickets, Func<T, long> getTicketId, Func<T, string> getTicketUuid = null)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Clear existing cache
                    ExecuteNonQuery(connection, "DELETE FROM CachedTickets;");
                    
                    var sql = @"
                        INSERT INTO CachedTickets (TicketId, TicketUuid, DataJson, CachedAt)
                        VALUES (@TicketId, @TicketUuid, @DataJson, @CachedAt);";
                    
                    foreach (var ticket in tickets)
                    {
                        using (var cmd = new SQLiteCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@TicketId", getTicketId(ticket));
                            cmd.Parameters.AddWithValue("@TicketUuid", getTicketUuid?.Invoke(ticket) ?? "");
                            cmd.Parameters.AddWithValue("@DataJson", JsonConvert.SerializeObject(ticket));
                            cmd.Parameters.AddWithValue("@CachedAt", DateTime.UtcNow.ToString("o"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("[OfflineRepo] Tickets cached successfully");
        }

        /// <summary>
        /// Gets tickets from local cache.
        /// </summary>
        public List<T> GetTicketsFromCache<T>()
        {
            lock (_lock)
            {
                var items = new List<T>();
                
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var cmd = new SQLiteCommand("SELECT DataJson FROM CachedTickets;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var json = reader["DataJson"].ToString();
                            items.Add(JsonConvert.DeserializeObject<T>(json));
                        }
                    }
                }
                
                return items;
            }
        }

        #endregion

        #region Machine History Cache Operations

        /// <summary>
        /// Saves machine history to local cache.
        /// </summary>
        public void SaveHistoryToCache<T>(IEnumerable<T> history, Func<T, long> getTicketId)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Clear existing cache
                    ExecuteNonQuery(connection, "DELETE FROM CachedMachineHistory;");
                    
                    var sql = @"
                        INSERT INTO CachedMachineHistory (TicketId, DataJson, CachedAt)
                        VALUES (@TicketId, @DataJson, @CachedAt);";
                    
                    // Workaround: Add DataJson column if missing (for schema migration)
                    try
                    {
                        ExecuteNonQuery(connection, "ALTER TABLE CachedMachineHistory ADD COLUMN DataJson TEXT;");
                    }
                    catch { /* Column already exists */ }
                    
                    foreach (var item in history)
                    {
                        using (var cmd = new SQLiteCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@TicketId", getTicketId(item));
                            cmd.Parameters.AddWithValue("@DataJson", JsonConvert.SerializeObject(item));
                            cmd.Parameters.AddWithValue("@CachedAt", DateTime.UtcNow.ToString("o"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("[OfflineRepo] Machine history cached successfully");
        }

        /// <summary>
        /// Gets machine history from local cache.
        /// </summary>
        public List<T> GetHistoryFromCache<T>()
        {
            lock (_lock)
            {
                var items = new List<T>();
                
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var cmd = new SQLiteCommand("SELECT DataJson FROM CachedMachineHistory WHERE DataJson IS NOT NULL;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var json = reader["DataJson"].ToString();
                            if (!string.IsNullOrEmpty(json))
                            {
                                items.Add(JsonConvert.DeserializeObject<T>(json));
                            }
                        }
                    }
                }
                
                return items;
            }
        }

        #endregion

        #region Generic Cache Operations

        public void SetCache(string key, object value, TimeSpan expiry)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var sql = @"
                        INSERT OR REPLACE INTO LocalCache (Key, ValueJson, ExpiresAt)
                        VALUES (@Key, @ValueJson, @ExpiresAt);";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Key", key);
                        cmd.Parameters.AddWithValue("@ValueJson", JsonConvert.SerializeObject(value));
                        cmd.Parameters.AddWithValue("@ExpiresAt", DateTime.UtcNow.Add(expiry).ToString("o"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public T GetCache<T>(string key) where T : class
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var cmd = new SQLiteCommand("SELECT ValueJson, ExpiresAt FROM LocalCache WHERE Key = @Key;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Key", key);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var expiresAt = DateTime.Parse(reader["ExpiresAt"].ToString());
                                if (expiresAt > DateTime.UtcNow)
                                {
                                    return JsonConvert.DeserializeObject<T>(reader["ValueJson"].ToString());
                                }
                            }
                        }
                    }
                }
                
                return null;
            }
        }

        #endregion

        #region User Cache Operations

        /// <summary>
        /// Saves users to local cache for offline authentication.
        /// </summary>
        public void SaveUsersToCache<T>(IEnumerable<T> users, 
            Func<T, long> userIdSelector,
            Func<T, string> usernameSelector,
            Func<T, string> passwordSelector,
            Func<T, string> fullNameSelector,
            Func<T, string> nikSelector,
            Func<T, int> roleIdSelector,
            Func<T, string> roleNameSelector,
            Func<T, bool> isActiveSelector)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    foreach (var user in users)
                    {
                        string sql = @"
                            INSERT OR REPLACE INTO CachedUsers 
                            (UserId, Username, PasswordHash, FullName, Nik, RoleId, RoleName, IsActive, LastSyncedAt)
                            VALUES (@UserId, @Username, @Password, @FullName, @Nik, @RoleId, @RoleName, @IsActive, @SyncedAt);";
                        
                        using (var cmd = new SQLiteCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userIdSelector(user));
                            cmd.Parameters.AddWithValue("@Username", usernameSelector(user) ?? "");
                            cmd.Parameters.AddWithValue("@Password", passwordSelector(user) ?? "");
                            cmd.Parameters.AddWithValue("@FullName", fullNameSelector(user) ?? "");
                            cmd.Parameters.AddWithValue("@Nik", nikSelector(user) ?? "");
                            cmd.Parameters.AddWithValue("@RoleId", roleIdSelector(user));
                            cmd.Parameters.AddWithValue("@RoleName", roleNameSelector(user) ?? "");
                            cmd.Parameters.AddWithValue("@IsActive", isActiveSelector(user) ? 1 : 0);
                            cmd.Parameters.AddWithValue("@SyncedAt", DateTime.UtcNow.ToString("o"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates user credentials against local cache for offline login.
        /// Returns UserDto with IsOfflineLogin=true if valid, null otherwise.
        /// </summary>
        public mtc_app.shared.data.dtos.UserDto ValidateOfflineLogin(string username, string password)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    string sql = @"
                        SELECT UserId, Username, PasswordHash, FullName, Nik, RoleId, RoleName, IsActive
                        FROM CachedUsers
                        WHERE Username = @Username;";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Check if user is active
                                int isActive = Convert.ToInt32(reader["IsActive"]);
                                if (isActive != 1)
                                {
                                    return null; // User is not active
                                }
                                
                                // Verify password (plain-text comparison to match current DB)
                                string storedPassword = reader["PasswordHash"]?.ToString() ?? "";
                                if (storedPassword != password)
                                {
                                    return null; // Password mismatch
                                }
                                
                                // Return user with offline flag
                                return new mtc_app.shared.data.dtos.UserDto
                                {
                                    UserId = Convert.ToInt64(reader["UserId"]),
                                    Username = reader["Username"]?.ToString(),
                                    FullName = reader["FullName"]?.ToString(),
                                    Nik = reader["Nik"]?.ToString(),
                                    RoleId = Convert.ToInt32(reader["RoleId"]),
                                    RoleName = reader["RoleName"]?.ToString(),
                                    IsOfflineLogin = true
                                };
                            }
                        }
                    }
                }
            }
            
            return null; // User not found
        }

        public List<string> GetUsersByRole(int roleId)
        {
            var result = new List<string>();
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT Nik FROM CachedUsers WHERE RoleId = @RoleId AND Nik IS NOT NULL ORDER BY Nik;", connection))
                    {
                        cmd.Parameters.AddWithValue("@RoleId", roleId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(reader["Nik"].ToString());
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets count of cached users.
        /// </summary>
        public int GetCachedUserCount()
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM CachedUsers;", connection))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
        }

        #endregion

        #region Master Data Cache Operations

        // ─────────────────────────────────────────────────────────────────────
        // Machines
        // ─────────────────────────────────────────────────────────────────────

        public void SaveMachinesToCache(IEnumerable<dynamic> machines)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    foreach (var m in machines)
                    {
                        string sql = @"
                            INSERT OR REPLACE INTO CachedMachines 
                            (MachineId, Code, MachineType, MachineArea, MachineNumber, StatusId, CachedAt)
                            VALUES (@MachineId, @Code, @Type, @Area, @Number, @StatusId, @CachedAt);";
                        
                        using (var cmd = new SQLiteCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@MachineId", (int)m.MachineId);
                            cmd.Parameters.AddWithValue("@Code", $"{m.MachineType}-{m.MachineArea}-{m.MachineNumber}");
                            cmd.Parameters.AddWithValue("@Type", m.MachineType ?? "");
                            cmd.Parameters.AddWithValue("@Area", m.MachineArea ?? "");
                            cmd.Parameters.AddWithValue("@Number", m.MachineNumber ?? "");
                            cmd.Parameters.AddWithValue("@StatusId", (int)(m.StatusId ?? 1));
                            cmd.Parameters.AddWithValue("@CachedAt", DateTime.UtcNow.ToString("o"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public List<CachedMachineDto> GetMachinesFromCache()
        {
            var result = new List<CachedMachineDto>();
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM CachedMachines;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new CachedMachineDto
                            {
                                MachineId = Convert.ToInt32(reader["MachineId"]),
                                Code = reader["Code"]?.ToString(),
                                MachineType = reader["MachineType"]?.ToString(),
                                MachineArea = reader["MachineArea"]?.ToString(),
                                MachineNumber = reader["MachineNumber"]?.ToString(),
                                StatusId = Convert.ToInt32(reader["StatusId"])
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Shifts
        // ─────────────────────────────────────────────────────────────────────

        public void SaveShiftsToCache(IEnumerable<dynamic> shifts)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    foreach (var s in shifts)
                    {
                        string sql = @"
                            INSERT OR REPLACE INTO CachedShifts 
                            (ShiftId, ShiftName, CachedAt)
                            VALUES (@ShiftId, @ShiftName, @CachedAt);";
                        
                        using (var cmd = new SQLiteCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@ShiftId", (int)s.ShiftId);
                            cmd.Parameters.AddWithValue("@ShiftName", s.ShiftName ?? "");
                            cmd.Parameters.AddWithValue("@CachedAt", DateTime.UtcNow.ToString("o"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public List<CachedShiftDto> GetShiftsFromCache()
        {
            var result = new List<CachedShiftDto>();
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM CachedShifts;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new CachedShiftDto
                            {
                                ShiftId = Convert.ToInt32(reader["ShiftId"]),
                                ShiftName = reader["ShiftName"]?.ToString()
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Problem Types
        // ─────────────────────────────────────────────────────────────────────

        public void SaveProblemTypesToCache(IEnumerable<dynamic> types)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    foreach (var t in types)
                    {
                        string sql = @"
                            INSERT OR REPLACE INTO CachedProblemTypes 
                            (TypeId, TypeName, CachedAt)
                            VALUES (@TypeId, @TypeName, @CachedAt);";
                        
                        using (var cmd = new SQLiteCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@TypeId", (int)t.TypeId);
                            cmd.Parameters.AddWithValue("@TypeName", t.TypeName ?? "");
                            cmd.Parameters.AddWithValue("@CachedAt", DateTime.UtcNow.ToString("o"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public List<CachedProblemTypeDto> GetProblemTypesFromCache()
        {
            var result = new List<CachedProblemTypeDto>();
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM CachedProblemTypes;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new CachedProblemTypeDto
                            {
                                TypeId = Convert.ToInt32(reader["TypeId"]),
                                TypeName = reader["TypeName"]?.ToString()
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Failures
        // ─────────────────────────────────────────────────────────────────────

        public void SaveFailuresToCache(IEnumerable<dynamic> failures)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    foreach (var f in failures)
                    {
                        string sql = @"
                            INSERT OR REPLACE INTO CachedFailures 
                            (FailureId, FailureName, CachedAt)
                            VALUES (@FailureId, @FailureName, @CachedAt);";
                        
                        using (var cmd = new SQLiteCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@FailureId", (int)f.FailureId);
                            cmd.Parameters.AddWithValue("@FailureName", f.FailureName ?? "");
                            cmd.Parameters.AddWithValue("@CachedAt", DateTime.UtcNow.ToString("o"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public List<CachedFailureDto> GetFailuresFromCache()
        {
            var result = new List<CachedFailureDto>();
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT FailureId, FailureName FROM CachedFailures ORDER BY FailureName;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new CachedFailureDto
                            {
                                FailureId = Convert.ToInt32(reader["FailureId"]),
                                FailureName = reader["FailureName"]?.ToString()
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Causes (NEW)
        // ─────────────────────────────────────────────────────────────────────

        public void SaveCausesToCache(IEnumerable<dynamic> causes)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    foreach (var c in causes)
                    {
                        string sql = @"INSERT OR REPLACE INTO CachedCauses (CauseId, CauseName, CachedAt) VALUES (@Id, @Name, @Date);";
                        using (var cmd = new SQLiteCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@Id", (int)c.cause_id);
                            cmd.Parameters.AddWithValue("@Name", (string)c.cause_name);
                            cmd.Parameters.AddWithValue("@Date", DateTime.UtcNow.ToString("o"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public List<CachedCauseDto> GetCausesFromCache()
        {
            var result = new List<CachedCauseDto>();
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT CauseId, CauseName FROM CachedCauses ORDER BY CauseName;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new CachedCauseDto
                            {
                                CauseId = Convert.ToInt32(reader["CauseId"]),
                                CauseName = reader["CauseName"]?.ToString()
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Actions (NEW)
        // ─────────────────────────────────────────────────────────────────────

        public void SaveActionsToCache(IEnumerable<dynamic> actions)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    foreach (var a in actions)
                    {
                        string sql = @"INSERT OR REPLACE INTO CachedActions (ActionId, ActionName, CachedAt) VALUES (@Id, @Name, @Date);";
                        using (var cmd = new SQLiteCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@Id", (int)a.action_id);
                            cmd.Parameters.AddWithValue("@Name", (string)a.action_name);
                            cmd.Parameters.AddWithValue("@Date", DateTime.UtcNow.ToString("o"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public List<CachedActionDto> GetActionsFromCache()
        {
            var result = new List<CachedActionDto>();
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT ActionId, ActionName FROM CachedActions ORDER BY ActionName;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new CachedActionDto
                            {
                                ActionId = Convert.ToInt32(reader["ActionId"]),
                                ActionName = reader["ActionName"]?.ToString()
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Pending Tickets (Queue)
        // ─────────────────────────────────────────────────────────────────────

        public int SavePendingTicket(CreateTicketRequest request)
        {
            int newId = 0;
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string json = JsonConvert.SerializeObject(request);
                    
                    string sql = @"
                        INSERT INTO PendingTickets (TicketData, CreatedAt, IsSynced)
                        VALUES (@Data, @Date, 0);
                        SELECT last_insert_rowid();";

                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Data", json);
                        cmd.Parameters.AddWithValue("@Date", DateTime.UtcNow.ToString("o"));
                        newId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            return newId;
        }

        public CreateTicketRequest GetPendingTicketById(int id)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT TicketData FROM PendingTickets WHERE Id = @Id;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        var json = cmd.ExecuteScalar()?.ToString();
                        if (string.IsNullOrEmpty(json)) return null;
                        return JsonConvert.DeserializeObject<CreateTicketRequest>(json);
                    }
                }
            }
        }

        public void UpdatePendingTicket(int id, CreateTicketRequest request)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string json = JsonConvert.SerializeObject(request);
                    string sql = "UPDATE PendingTickets SET TicketData = @Data WHERE Id = @Id";
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Data", json);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public CachedUserDto GetUserByNik(string nik)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT * FROM CachedUsers WHERE Nik = @Nik LIMIT 1";
                    return connection.QueryFirstOrDefault<CachedUserDto>(sql, new { Nik = nik });
                }
            }
        }

        public List<(int Id, CreateTicketRequest Request)> GetPendingTickets()
        {
            var result = new List<(int Id, CreateTicketRequest Request)>();
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT Id, TicketData FROM PendingTickets WHERE IsSynced = 0 ORDER BY Id;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = Convert.ToInt32(reader["Id"]);
                            string json = reader["TicketData"].ToString();
                            var request = JsonConvert.DeserializeObject<CreateTicketRequest>(json);
                            if (request != null)
                            {
                                result.Add((id, request));
                            }
                        }
                    }
                }
            }
            return result;
        }

        public void DeletePendingTicket(int id)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM PendingTickets WHERE Id = @Id;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
