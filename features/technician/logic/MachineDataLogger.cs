using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using mtc_app.shared.presentation.styles; // For DB Helper access if valid, or standard Namespace

namespace mtc_app.features.technician.logic
{
    public class MachineDataLogger
    {
        // Parsing Helpers (Reused from MonitorControl logic)
        private static double ParseIniValue(string path, string key)
        {
            try {
                foreach (var line in File.ReadAllLines(path, Encoding.Default)) {
                    if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase)) {
                        var parts = line.Split('=');
                        if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out double val)) return val;
                    }
                }
            } catch { }
            return 0;
        }

        private static long ParseLineValue(string path, int lineIndex)
        {
            try {
                var lines = File.ReadAllLines(path, Encoding.Default);
                if (lineIndex < lines.Length) {
                    string valPart = lines[lineIndex].Contains("=") ? lines[lineIndex].Split('=')[1].Trim() : lines[lineIndex].Trim();
                    if (long.TryParse(valPart, out long val)) return val;
                }
            } catch { }
            return 0;
        }

        private static long FindNumericValue(string path, int skipCount)
        {
            try {
                int found = 0;
                foreach (var line in File.ReadAllLines(path, Encoding.Default)) {
                    if (line.Contains("=")) {
                        string valPart = line.Split('=')[1].Trim();
                        if (long.TryParse(valPart, out long val) && val > 0) {
                            if (found == skipCount) return val;
                            found++;
                        }
                    }
                }
            } catch { }
            return 0;
        }

        public async Task LogMachineDataAsync()
        {
            try
            {
                // 1. Identify Local Machine
                // We assume appsettings or DB has mapping. For now, let's TRY to log ALL machines 
                // that have valid files on THIS PC. This handles the "Server" scenario too.
                
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var machines = await conn.QueryAsync(@"
                        SELECT m.machine_id, t.type_name as machine_type 
                        FROM machines m 
                        LEFT JOIN machine_types t ON m.type_id = t.type_id");

                    foreach (var m in machines)
                    {
                        long lots = 0, pcs = 0;
                        double auto = 0, mon = 0;
                        bool fileFound = false;
                        string type = m.machine_type.ToString().ToUpper();

                        // AC90 Logic
                        if (type.Contains("AC90"))
                        {
                            string pathProd = @"C:\AC90HMI\prg\INI\HmiProcess.ini";
                            string pathEff = @"C:\AC90HMI\prg\INI\HmiProcess2.ini";
                            if (File.Exists(pathProd)) {
                                lots = ParseLineValue(pathProd, 2);
                                pcs = ParseLineValue(pathProd, 3);
                                fileFound = true;
                            }
                            if (File.Exists(pathEff)) {
                                auto = ParseIniValue(pathEff, "AutoTime");
                                mon = ParseIniValue(pathEff, "MonitorTime");
                                fileFound = true;
                            }
                        }
                        // AC95 Logic
                        else if (type.Contains("AC95"))
                        {
                            string path = @"D:\AC95\Product\Information.ini";
                            if (File.Exists(path)) {
                                lots = (long)ParseIniValue(path, "ProducedLots");
                                pcs = (long)ParseIniValue(path, "ProducedPieces");
                                auto = ParseIniValue(path, "AutoTime");
                                mon = ParseIniValue(path, "MonitorTime");
                                fileFound = true;
                            }
                        }
                        // AC80/81 Logic
                        else if (type.Contains("AC80") || type.Contains("AC81"))
                        {
                            string folder = type.Contains("81") ? "AC81" : "AC80";
                            string path = $@"C:\{folder}HMI\{folder}\{folder}";
                            if (!File.Exists(path) && File.Exists(path + ".ini")) path += ".ini";
                            
                            if (File.Exists(path)) {
                                lots = FindNumericValue(path, 0); // 1st number
                                pcs = FindNumericValue(path, 1);  // 2nd number
                                fileFound = true;
                            }
                        }

                        // 2. Insert to DB if data found (Rolling Window Requirement: Always Insert)
                        if (fileFound && (lots > 0 || pcs > 0 || auto > 0))
                        {
                            string sql = @"INSERT INTO machine_process_logs 
                                           (machine_id, produced_lots, produced_pieces, auto_time, monitor_time, created_at) 
                                           VALUES (@Mid, @Lots, @Pcs, @Auto, @Mon, NOW())";
                            
                            await conn.ExecuteAsync(sql, new { 
                                Mid = m.machine_id, 
                                Lots = lots, 
                                Pcs = pcs, 
                                Auto = auto, 
                                Mon = mon 
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log silently or to debug console
                Console.WriteLine($"Logger Error: {ex.Message}");
            }
        }
    }
}