using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions; // [PENTING] Tambah ini untuk Regex
using System.Threading.Tasks;
using Dapper;
using mtc_app.shared.presentation.styles; 

namespace mtc_app.features.technician.logic
{
    public class MachineDataLogger
    {
        // 1. [UPDATE] Gunakan Regex agar konsisten dengan MachineMonitorControl
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

        // 2. [UPDATE] Gunakan Regex untuk mengambil angka bersih
        private static long ParseLineValue(string path, int lineIndex)
        {
            try {
                var lines = File.ReadAllLines(path, Encoding.Default);
                if (lineIndex < lines.Length) {
                    string line = lines[lineIndex];
                    // Ambil hanya digit angka, abaikan huruf Jepang/karakter lain
                    var match = Regex.Match(line, @"\d+");
                    if (match.Success && long.TryParse(match.Value, out long val)) 
                    {
                        return val;
                    }
                }
            } catch { }
            return 0;
        }

        // 3. [UPDATE] Gunakan Regex juga disini
        private static long FindNumericValue(string path, int skipCount)
        {
            try {
                int found = 0;
                foreach (var line in File.ReadAllLines(path, Encoding.Default)) {
                    if (line.Contains("=")) {
                        string valPart = line.Split('=')[1].Trim();
                        // Bersihkan string dulu dengan Regex sebelum parse
                        var match = Regex.Match(valPart, @"\d+");
                        if (match.Success && long.TryParse(match.Value, out long val) && val > 0) {
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
                using (var conn = DatabaseHelper.GetConnection())
                {
                    // Ambil daftar semua mesin
                    var machines = await conn.QueryAsync(@"
                        SELECT m.machine_id, m.machine_number, 
                               t.type_name, a.area_name 
                        FROM machines m 
                        LEFT JOIN machine_types t ON m.type_id = t.type_id
                        LEFT JOIN machine_areas a ON m.area_id = a.area_id");

                    foreach (var m in machines)
                    {
                        long lots = 0, pcs = 0;
                        double auto = 0, mon = 0;
                        bool fileFound = false;
                        
                        // Null safety
                        string typeName = m.type_name?.ToString().ToUpper() ?? "";

                        // --- LOGIKA BACA FILE (SAMA PERSIS DENGAN MONITOR) ---

                        // AC90 Logic
                        if (typeName.Contains("AC90"))
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
                        else if (typeName.Contains("AC95"))
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
                        else if (typeName.Contains("AC80") || typeName.Contains("AC81"))
                        {
                            string folder = typeName.Contains("81") ? "AC81" : "AC80";
                            string path = $@"C:\{folder}HMI\{folder}\{folder}";
                            if (!File.Exists(path) && File.Exists(path + ".ini")) path += ".ini";
                            
                            if (File.Exists(path)) {
                                lots = FindNumericValue(path, 0); // Angka pertama = Lots
                                pcs = FindNumericValue(path, 1);  // Angka kedua = Pcs
                                fileFound = true;
                            }
                        }

                        // 4. SIMPAN KE DATABASE (Hanya jika file ditemukan dan ada isinya)
                        //    Agar database tidak penuh dengan log kosong (0)
                        if (fileFound && (lots > 0 || pcs > 0 || auto > 0 || mon > 0))
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
                // Optional: Log error ke file teks atau console debug
                System.Diagnostics.Debug.WriteLine($"Logger Error: {ex.Message}");
            }
        }
    }
}