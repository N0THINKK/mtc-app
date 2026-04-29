using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using mtc_app.features.operator_worksheet.data.dtos;

namespace mtc_app.features.operator_worksheet.data.repositories
{
    public class MachineFileRepository
    {
        private readonly string _baseDir;

        public MachineFileRepository(string baseDir = @"C:\AC90HMI\prg\")
        {
            _baseDir = baseDir;
        }

        public List<PrdLogDto> GetPrdLogs()
        {
            var result = new List<PrdLogDto>();
            string filePath = Path.Combine(_baseDir, "PrdLog.csv");
            
            if (!File.Exists(filePath)) return result;

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split(',');
                    if (parts.Length >= 6)
                    {
                        result.Add(new PrdLogDto
                        {
                            Sequen = parts[0]?.Trim(),
                            UrutanPengerjaan = parts[1]?.Trim(),
                            WaktuMulaiPengerjaan = parts[2]?.Trim(),
                            WaktuSelesaiPengerjaan = parts[3]?.Trim(),
                            QtyProduk = parts[4]?.Trim(),
                            QtyDefect = parts[5]?.Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading PrdLog.csv: {ex.Message}");
            }

            return result;
        }

        public List<PrdmstDto> GetPrdMst()
        {
            var result = new List<PrdmstDto>();
            string filePath = Path.Combine(_baseDir, "prdmst.csv");
            
            if (!File.Exists(filePath)) return result;

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        var parts = line.Split(',');
                        if (parts.Length >= 45) // KombinasiWire ada di indeks 44
                        {
                            result.Add(new PrdmstDto
                            {
                                UrutanSequen = parts[0]?.Trim(),
                                Sequen = parts[1]?.Trim(),
                                CutLength = parts[5]?.Trim(), // Kolom ke-6
                                Qty = parts[3]?.Trim(),
                                TerminalA = parts[11]?.Trim(),     // Kolom ke-12
                                TerminalB = parts[12]?.Trim(),     // Kolom ke-13
                                HasTerminalA = parts[13]?.Trim(),  // Kolom ke-14
                                HasTerminalB = parts[14]?.Trim(),  // Kolom ke-15
                                SealA = parts[15]?.Trim(),         // Kolom ke-16
                                SealB = parts[16]?.Trim(),         // Kolom ke-17
                                KombinasiWire = !string.IsNullOrWhiteSpace(parts[43]?.Trim()) ? parts[43].Trim() : parts[44]?.Trim()  // Kolom ke-44 (nama wire), fallback ke kolom ke-45
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading prdmst.csv: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Parse Jissk.dat — file fixed-width dari mesin.
        /// Menggunakan regex untuk menemukan nilai desimal X.XXX (posisi-independen).
        /// Urutan nilai selalu: FrontChA, FrontCwA, RearChA, RearCwA, FrontChB, FrontCwB, RearChB, RearCwB.
        /// </summary>
        public List<JisskDto> GetJisskData()
        {
            var result = new List<JisskDto>();
            string filePath = Path.Combine(_baseDir, "Jissk.dat");

            System.Diagnostics.Debug.WriteLine($"[JISSK] Looking for file: {filePath}");
            System.Diagnostics.Debug.WriteLine($"[JISSK] File exists: {File.Exists(filePath)}");

            // Fallback: cek langsung di C:\AC90HMI jika tidak ada di prg
            if (!File.Exists(filePath))
            {
                string fallback = @"C:\AC90HMI\Jissk.dat";
                System.Diagnostics.Debug.WriteLine($"[JISSK] Trying fallback: {fallback}, exists: {File.Exists(fallback)}");
                if (File.Exists(fallback))
                    filePath = fallback;
                else
                    return result;
            }

            // Regex: angka desimal format X.XXX (3 digit di belakang titik)
            var decimalPattern = new Regex(@"\d\.\d{3}", RegexOptions.Compiled);

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Ambil kolom kedua (sequence number) dari whitespace-separated tokens
                        var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length < 2) continue;

                        string rawSeq = tokens[1].Trim();

                        // Ambil 4 digit terakhir
                        string seq4 = rawSeq.Length >= 4 ? rawSeq.Substring(rawSeq.Length - 4) : rawSeq;

                        // Cari semua angka desimal X.XXX di baris ini
                        var matches = decimalPattern.Matches(line);

                        var dto = new JisskDto
                        {
                            RawSequen = rawSeq,
                            Sequen4 = seq4
                        };

                        // 8 nilai berurutan: FchA, FcwA, RchA, RcwA, FchB, FcwB, RchB, RcwB
                        if (matches.Count >= 1) dto.FrontChA = matches[0].Value;
                        if (matches.Count >= 2) dto.FrontCwA = matches[1].Value;
                        if (matches.Count >= 3) dto.RearChA = matches[2].Value;
                        if (matches.Count >= 4) dto.RearCwA = matches[3].Value;
                        if (matches.Count >= 5) dto.FrontChB = matches[4].Value;
                        if (matches.Count >= 6) dto.FrontCwB = matches[5].Value;
                        if (matches.Count >= 7) dto.RearChB = matches[6].Value;
                        if (matches.Count >= 8) dto.RearCwB = matches[7].Value;

                        result.Add(dto);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JISSK] Error reading Jissk.dat: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine($"[JISSK] Total records loaded: {result.Count}");
            if (result.Count > 0)
            {
                var sample = result[0];
                System.Diagnostics.Debug.WriteLine($"[JISSK] Sample: RawSeq={sample.RawSequen}, Seq4={sample.Sequen4}, FchA={sample.FrontChA}, FcwA={sample.FrontCwA}");
            }
            return result;
        }

        // TBD: Saving data back to PrdLog
        public void UpdatePrdLog(PrdLogDto updatedLog)
        {
            string filePath = Path.Combine(_baseDir, "PrdLog.csv");
            if (!File.Exists(filePath)) return;

            try
            {
                var lines = File.ReadAllLines(filePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length > 0 && parts[0].Trim() == updatedLog.Sequen)
                    {
                        lines[i] = $"{updatedLog.Sequen},{updatedLog.UrutanPengerjaan},{updatedLog.WaktuMulaiPengerjaan},{updatedLog.WaktuSelesaiPengerjaan},{updatedLog.QtyProduk},{updatedLog.QtyDefect}";
                        break;
                    }
                }
                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating PrdLog.csv: {ex.Message}");
            }
        }
    }
}
