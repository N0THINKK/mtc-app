using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace mtc_app.features.operator_worksheet.services
{
    public class MachineFileWatcherService : IDisposable
    {
        private List<FileSystemWatcher> _fileWatchers = new List<FileSystemWatcher>();
        private Timer _debounceTimerSequen;
        private Timer _debounceTimerProduct;

        public event EventHandler OnSequenDataChanged;
        public event EventHandler OnProductDataChanged;

        private readonly Control _invokeTarget;

        public MachineFileWatcherService(Control invokeTarget)
        {
            _invokeTarget = invokeTarget;

            // Debounce timer untuk PrdLog/PrdMst: tunggu 10 detik sebelum reload grid Sequen
            _debounceTimerSequen = new Timer { Interval = 10000 };
            _debounceTimerSequen.Tick += (s, e) =>
            {
                _debounceTimerSequen.Stop();
                OnSequenDataChanged?.Invoke(this, EventArgs.Empty);
            };

            // Debounce timer untuk Product.csv: tunggu 3 detik sebelum reload grid Barcode saja
            _debounceTimerProduct = new Timer { Interval = 3000 };
            _debounceTimerProduct.Tick += (s, e) =>
            {
                _debounceTimerProduct.Stop();
                OnProductDataChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public void StartWatching()
        {
            StopWatching();

            // Daftar folder yang perlu dipantau
            string[] watchDirs = new[]
            {
                @"C:\AC90HMI\prg",                          // AC90: PrdLog.csv, prdmst.csv, Product.csv
                @"C:\AC80HMI",                               // AC80: PrdLog.csv, prdmst.csv, product.csv
                @"D:\AC95\prg\HMI\RelationalData",           // AC95: ProductionLog.xml
                @"C:\AC95\prg\HMI\RelationalData",           // AC95: ProductionLog.xml (alt)
                @"D:\AC95\Product",                           // AC95: Product.csv (drive D)
                @"C:\AC95\Product"                            // AC95: Product.csv (drive C)
            };

            foreach (var dir in watchDirs)
            {
                if (!Directory.Exists(dir)) continue;

                var watcher = new FileSystemWatcher(dir)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true,
                    Filter = "*.*" // csv dan xml
                };
                watcher.Changed += OnCsvFileChanged;
                _fileWatchers.Add(watcher);
            }
        }

        public void StopWatching()
        {
            foreach (var watcher in _fileWatchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= OnCsvFileChanged;
                watcher.Dispose();
            }
            _fileWatchers.Clear();
        }

        private void OnCsvFileChanged(object sender, FileSystemEventArgs e)
        {
            string name = e.Name?.ToLower() ?? "";
            if (name == "prdlog.csv" || name == "prdmst.csv" || name == "productionlog.xml")
            {
                TriggerTimerSafe(_debounceTimerSequen);
            }
            else if (name == "product.csv")
            {
                TriggerTimerSafe(_debounceTimerProduct);
            }
        }

        private void TriggerTimerSafe(Timer timer)
        {
            if (_invokeTarget != null && _invokeTarget.InvokeRequired)
            {
                _invokeTarget.BeginInvoke(new Action(() => 
                { 
                    timer.Stop(); 
                    timer.Start(); 
                }));
            }
            else
            {
                timer.Stop(); 
                timer.Start();
            }
        }

        public void Dispose()
        {
            StopWatching();
            _debounceTimerSequen?.Dispose();
            _debounceTimerProduct?.Dispose();
        }
    }
}
