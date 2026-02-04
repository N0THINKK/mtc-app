using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
