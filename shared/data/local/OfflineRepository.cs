using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Newtonsoft.Json;

namespace mtc_app.shared.data.local
{
    /// <summary>
    /// Thread-safe repository for managing the local SQLite offline database.
    /// Implements Store-and-Forward pattern for offline operation support.
    /// </summary>
    public class OfflineRepository : IDisposable
    {
        private static readonly object _lock = new object();
        private readonly string _dbPath;
        private readonly string _connectionString;
        private bool _disposed;

        public OfflineRepository()
        {
            // Store in user's local app data folder
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
                    
                    // Create SyncQueue table
                    var createQueueSql = @"
                        CREATE TABLE IF NOT EXISTS SyncQueue (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ActionType TEXT NOT NULL,
                            TableName TEXT NOT NULL,
                            PayloadJson TEXT NOT NULL,
                            CreatedAt TEXT NOT NULL,
                            RetryCount INTEGER DEFAULT 0
                        );";
                    
                    // Create LocalCache table
                    var createCacheSql = @"
                        CREATE TABLE IF NOT EXISTS LocalCache (
                            Key TEXT PRIMARY KEY,
                            ValueJson TEXT NOT NULL,
                            ExpiresAt TEXT NOT NULL
                        );";
                    
                    using (var cmd = new SQLiteCommand(createQueueSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    
                    using (var cmd = new SQLiteCommand(createCacheSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        #region Sync Queue Operations

        /// <summary>
        /// Adds an operation to the sync queue for later processing.
        /// </summary>
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

        /// <summary>
        /// Retrieves all pending items from the sync queue.
        /// </summary>
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

        /// <summary>
        /// Removes an item from the sync queue after successful processing.
        /// </summary>
        public void RemoveFromQueue(long id)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var sql = "DELETE FROM SyncQueue WHERE Id = @Id;";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Increments the retry count for an item.
        /// </summary>
        public void IncrementRetryCount(long id)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var sql = "UPDATE SyncQueue SET RetryCount = RetryCount + 1 WHERE Id = @Id;";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the count of pending items in the queue.
        /// </summary>
        public int GetQueueCount()
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var sql = "SELECT COUNT(*) FROM SyncQueue;";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
        }

        #endregion

        #region Local Cache Operations

        /// <summary>
        /// Stores or updates a cached value.
        /// </summary>
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

        /// <summary>
        /// Retrieves a cached value if it exists and hasn't expired.
        /// </summary>
        public T GetCache<T>(string key) where T : class
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var sql = "SELECT ValueJson, ExpiresAt FROM LocalCache WHERE Key = @Key;";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Key", key);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var expiresAt = DateTime.Parse(reader["ExpiresAt"].ToString());
                                if (expiresAt > DateTime.UtcNow)
                                {
                                    var json = reader["ValueJson"].ToString();
                                    return JsonConvert.DeserializeObject<T>(json);
                                }
                            }
                        }
                    }
                }
                
                return null;
            }
        }

        /// <summary>
        /// Removes a specific cache entry.
        /// </summary>
        public void RemoveCache(string key)
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var sql = "DELETE FROM LocalCache WHERE Key = @Key;";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Key", key);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Clears all expired cache entries.
        /// </summary>
        public void ClearExpiredCache()
        {
            lock (_lock)
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var sql = "DELETE FROM LocalCache WHERE ExpiresAt < @Now;";
                    
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("o"));
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
