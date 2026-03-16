using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace mtc_app.features.applicator_patrol.data.services
{
    /// <summary>
    /// Memantau file prdmst.csv dengan FileSystemWatcher.
    /// Jika file berubah, membaca ulang dan memicu event OnApplicatorsChanged.
    /// Menggunakan debounce 800ms untuk menghindari multi-fire.
    /// </summary>
    public class CsvFileWatcherService : IDisposable
    {
        public event Action<List<string>, List<string>> OnApplicatorsChanged;

        private readonly string _filePath;
        private FileSystemWatcher _watcher;
        private System.Timers.Timer _debounceTimer;
        private bool _disposed = false;

        public CsvFileWatcherService(string csvFilePath)
        {
            _filePath = csvFilePath;
        }

        public void Start()
        {
            if (!File.Exists(_filePath)) return;

            string dir = Path.GetDirectoryName(_filePath);
            string fileName = Path.GetFileName(_filePath);

            _debounceTimer = new System.Timers.Timer(800);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += OnDebounceElapsed;

            _watcher = new FileSystemWatcher(dir, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Reset debounce timer setiap ada perubahan
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void OnDebounceElapsed(object sender, ElapsedEventArgs e)
        {
            var (sideA, sideB) = ApplicatorCsvReader.ReadApplicators(_filePath);
            OnApplicatorsChanged?.Invoke(sideA, sideB);
        }

        /// <summary>
        /// Baca state awal saat form dibuka (sinkron).
        /// </summary>
        public (List<string> SideA, List<string> SideB) ReadInitial()
        {
            return ApplicatorCsvReader.ReadApplicators(_filePath);
        }

        public void Stop()
        {
            _watcher?.Dispose();
            _debounceTimer?.Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }
}
