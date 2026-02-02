using System;

namespace mtc_app.shared.data.local
{
    /// <summary>
    /// Represents a queued operation that failed to sync to the main database.
    /// </summary>
    public class SyncQueueItem
    {
        public long Id { get; set; }
        public string ActionType { get; set; }  // "INSERT", "UPDATE", "DELETE"
        public string TableName { get; set; }   // Target table name
        public string PayloadJson { get; set; } // Serialized data
        public DateTime CreatedAt { get; set; }
        public int RetryCount { get; set; }
    }

    /// <summary>
    /// Represents a cached item for offline read access.
    /// </summary>
    public class LocalCacheItem
    {
        public string Key { get; set; }
        public string ValueJson { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
