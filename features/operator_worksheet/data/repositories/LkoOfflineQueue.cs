using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using mtc_app.features.operator_worksheet.data.dtos;

namespace mtc_app.features.operator_worksheet.data.repositories
{
    /// <summary>
    /// Manages offline queue for LKO records when MySQL is unavailable.
    /// Records are stored as JSON in a local file and synced when connection is restored.
    /// </summary>
    public static class LkoOfflineQueue
    {
        private static readonly string QueueDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MTC_App", "offline_queue");

        private static readonly string QueueFile = Path.Combine(QueueDir, "lko_pending.json");

        private static readonly object _lock = new object();

        /// <summary>
        /// Enqueue a record for later sync to MySQL.
        /// </summary>
        public static void Enqueue(LkoRecordDto record)
        {
            lock (_lock)
            {
                var queue = LoadQueue();
                queue.Add(record);
                SaveQueue(queue);
            }
        }

        /// <summary>
        /// Get all pending (unsynced) records.
        /// </summary>
        public static List<LkoRecordDto> GetPending()
        {
            lock (_lock)
            {
                return LoadQueue();
            }
        }

        /// <summary>
        /// Get pending records filtered by machine number and today's date.
        /// </summary>
        public static List<LkoRecordDto> GetPendingForMachine(string noMesin)
        {
            var all = GetPending();
            var today = DateTime.Today;
            return all.Where(r =>
                r.NoMesin == noMesin &&
                r.WaktuSimpan.Date == today
            ).ToList();
        }

        /// <summary>
        /// Remove a specific record from the queue (after successful sync).
        /// Matches by WaktuSimpan + Sequen + UrutanKanban + NoMesin.
        /// </summary>
        public static void Remove(LkoRecordDto record)
        {
            lock (_lock)
            {
                var queue = LoadQueue();
                queue.RemoveAll(r =>
                    r.WaktuSimpan == record.WaktuSimpan &&
                    r.Sequen == record.Sequen &&
                    r.UrutanKanban == record.UrutanKanban &&
                    r.NoMesin == record.NoMesin);
                SaveQueue(queue);
            }
        }

        /// <summary>
        /// Remove all records from the queue.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                SaveQueue(new List<LkoRecordDto>());
            }
        }

        /// <summary>
        /// Check if there are any pending records.
        /// </summary>
        public static bool HasPending()
        {
            return GetPending().Count > 0;
        }

        /// <summary>
        /// Get count of pending records.
        /// </summary>
        public static int PendingCount()
        {
            return GetPending().Count;
        }

        private static List<LkoRecordDto> LoadQueue()
        {
            try
            {
                if (!File.Exists(QueueFile))
                    return new List<LkoRecordDto>();

                string json = File.ReadAllText(QueueFile);
                return JsonConvert.DeserializeObject<List<LkoRecordDto>>(json) ?? new List<LkoRecordDto>();
            }
            catch
            {
                return new List<LkoRecordDto>();
            }
        }

        private static void SaveQueue(List<LkoRecordDto> queue)
        {
            try
            {
                if (!Directory.Exists(QueueDir))
                    Directory.CreateDirectory(QueueDir);

                string json = JsonConvert.SerializeObject(queue, Formatting.Indented);
                File.WriteAllText(QueueFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LkoOfflineQueue.SaveQueue error: {ex.Message}");
            }
        }
    }
}
