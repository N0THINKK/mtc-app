using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.data.utils;

namespace mtc_app.shared.data.services
{
    public class MachineMonitorService
    {
        private const string INI_FILE_PATH = @"C:\AC90HMI\prg\INI\HmiProcess.Ini";
        private const int UPDATE_INTERVAL_MS = 60000; // Update every 1 minute
        
        private Timer _timer;
        private int _machineId;
        private bool _isRunning;

        public MachineMonitorService()
        {
            _timer = new Timer();
            _timer.Interval = UPDATE_INTERVAL_MS;
            _timer.Tick += async (s, e) => await PushDataToDatabase();
        }

        public void Initialize(int machineId)
        {
            _machineId = machineId;
            if (_machineId > 0 && !_isRunning)
            {
                _isRunning = true;
                _timer.Start();
                // Run once immediately
                Task.Run(() => PushDataToDatabase());
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _isRunning = false;
        }

        private async Task PushDataToDatabase()
        {
            try
            {
                if (!File.Exists(INI_FILE_PATH)) 
                {
                    Console.WriteLine($"[Monitor] File not found: {INI_FILE_PATH}");
                    return;
                }

                // Read Values from INI
                string section = "ActionCount";
                
                long allProd = ParseLong(IniFileHelper.ReadValue(section, "AllProduct", INI_FILE_PATH, "0"));
                long cutter = ParseLong(IniFileHelper.ReadValue(section, "CountCutter", INI_FILE_PATH, "0"));
                long stripA = ParseLong(IniFileHelper.ReadValue(section, "CountStripA", INI_FILE_PATH, "0"));
                long stripB = ParseLong(IniFileHelper.ReadValue(section, "CountStripB", INI_FILE_PATH, "0"));
                long pressA = ParseLong(IniFileHelper.ReadValue(section, "CountPressA", INI_FILE_PATH, "0"));
                long pressB = ParseLong(IniFileHelper.ReadValue(section, "CountPressB", INI_FILE_PATH, "0"));

                Console.WriteLine($"[Monitor] Read: AllProd={allProd}, Cutter={cutter}"); // Debug Log

                // Update Database
                // Using INSERT ... ON DUPLICATE KEY UPDATE to ensure we either create or update the single row for this machine
                string sql = @"
                    INSERT INTO machine_process_logs 
                    (machine_id, all_product, count_cutter, count_strip_a, count_strip_b, count_press_a, count_press_b, last_updated)
                    VALUES 
                    (@MachineId, @AllProd, @Cutter, @StripA, @StripB, @PressA, @PressB, NOW())
                    ON DUPLICATE KEY UPDATE
                    all_product = VALUES(all_product),
                    count_cutter = VALUES(count_cutter),
                    count_strip_a = VALUES(count_strip_a),
                    count_strip_b = VALUES(count_strip_b),
                    count_press_a = VALUES(count_press_a),
                    count_press_b = VALUES(count_press_b),
                    last_updated = NOW();
                ";

                using (var conn = DatabaseHelper.GetConnection())
                {
                    await conn.ExecuteAsync(sql, new 
                    { 
                        MachineId = _machineId,
                        AllProd = allProd,
                        Cutter = cutter,
                        StripA = stripA,
                        StripB = stripB,
                        PressA = pressA,
                        PressB = pressB
                    });
                    
                    Console.WriteLine("[Monitor] Database updated successfully.");
                }
            }
            catch (Exception ex)
            {
                // Silently fail or log to local file
                Console.WriteLine($"[Monitor] Error: {ex.Message}");
            }
        }

        private long ParseLong(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return 0;
            // Clean string from potential nulls or spaces
            val = val.Trim().Replace("\0", "");
            
            if (long.TryParse(val, out long result)) return result;
            return 0;
        }
    }
}
