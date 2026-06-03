using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using mtc_app.features.operator_worksheet.data.dtos;
using mtc_app.features.operator_worksheet.data.providers;
using mtc_app.features.operator_worksheet.data.parsers;

namespace mtc_app.features.operator_worksheet.data.repositories
{
    public class MachineFileRepository
    {
        private readonly MachineConfigurationProvider _config;

        public MachineFileRepository(string baseDir = null)
        {
            _config = new MachineConfigurationProvider(baseDir);
        }

        public bool IsXmlSource { get; private set; } = false;

        public List<PrdLogDto> GetPrdLogs()
        {
            string csvPath = Path.Combine(_config.BaseDirectory, "PrdLog.csv");
            if (File.Exists(csvPath))
            {
                var parser = new CsvPrdLogParser();
                IsXmlSource = parser.IsXmlSource;
                return parser.Parse(csvPath);
            }

            string[] xmlFallbacks = new[]
            {
                @"D:\AC95\prg\HMI\RelationalData\ProductionLog.xml",
                @"C:\AC95\prg\HMI\RelationalData\ProductionLog.xml"
            };

            string xmlPath = _config.GetFilePath("ProductionLog.xml", xmlFallbacks);
            if (xmlPath != null)
            {
                var parser = new XmlPrdLogParser();
                IsXmlSource = parser.IsXmlSource;
                return parser.Parse(xmlPath);
            }

            return new List<PrdLogDto>();
        }

        public List<PrdmstDto> GetPrdMst()
        {
            var result = new List<PrdmstDto>();
            string[] fallbacks = new[]
            {
                @"D:\AC95\Kanban\prdmst.csv",
                @"C:\AC95\Kanban\prdmst.csv",
                @"C:\AC80HMI\prdmst.csv",
                @"C:\AC90HMI\prg\prdmst.csv",
                @"C:\AC90 Master Paper\prdmst.csv"
            };
            
            string filePath = _config.GetFilePath("prdmst.csv", fallbacks);
            if (filePath == null) return result;

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
                System.Diagnostics.Debug.WriteLine($"[MachineFileRepository] Error reading prdmst.csv: {ex.Message}");
            }

            return result;
        }

        public List<JisskDto> GetJisskData()
        {
            var result = new List<JisskDto>();
            string[] fallbacks = new[]
            {
                @"C:\AC90HMI\Jissk.dat",
                @"D:\AC95\Backup Jissk\jissk.dat",
                @"C:\AC95\Backup Jissk\jissk.dat",
                @"C:\AC80HMI\Jissk.dat"
            };

            string filePath = _config.GetFilePath("Jissk.dat", fallbacks);
            if (filePath == null) return result;

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

                        string rawSeq = "";
                        if (line.Length >= 12)
                        {
                            string seqArea = line.Substring(7, 5);
                            var areaTokens = seqArea.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (areaTokens.Length > 0)
                            {
                                rawSeq = areaTokens.Last();
                            }
                        }
                        
                        if (string.IsNullOrEmpty(rawSeq))
                        {
                            var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (tokens.Length < 2) continue;
                            rawSeq = tokens[1].Trim();
                        }

                        string seq4 = rawSeq;
                        if (seq4.Length > 4) 
                        {
                            seq4 = seq4.Substring(seq4.Length - 4);
                        }
                        seq4 = seq4.PadLeft(4, '0');

                        var matches = decimalPattern.Matches(line);

                        var dto = new JisskDto
                        {
                            RawSequen = rawSeq,
                            Sequen4 = seq4
                        };

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
                System.Diagnostics.Debug.WriteLine($"[MachineFileRepository] Error reading Jissk.dat: {ex.Message}");
            }

            return result;
        }

        public void UpdatePrdLog(PrdLogDto updatedLog)
        {
            string filePath = Path.Combine(_config.BaseDirectory, "PrdLog.csv");
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
                System.Diagnostics.Debug.WriteLine($"[MachineFileRepository] Error updating PrdLog.csv: {ex.Message}");
            }
        }

        public List<ProductDto> GetProductSequences()
        {
            var result = new List<ProductDto>();
            string[] searchPaths = new[]
            {
                Path.Combine(_config.BaseDirectory, "product.csv"),              // AC80 (huruf kecil)
                @"D:\AC95\Product\Product.csv",                     // AC95 (drive D)
                @"C:\AC95\Product\Product.csv",                     // AC95 (drive C fallback)
                @"C:\AC80HMI\product.csv",                          // AC80
                @"C:\AC80HMI\Product.csv"                           // AC80 (huruf besar)
            };
            
            string filePath = _config.GetFilePath("Product.csv", searchPaths);
            if (filePath == null) return result;

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string line;
                    var seenSequences = new HashSet<string>();
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(',');
                        if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                        {
                            string status = parts.Length > 36 ? parts[36].Trim() : "";
                            if (status != "0" && status != "1") continue;

                            var dto = new ProductDto { Sequen = parts[0].Trim() };
                            if (seenSequences.Contains(dto.Sequen)) continue;
                            seenSequences.Add(dto.Sequen);

                            if (parts.Length > 4) dto.CutLength = parts[4].Trim();
                            if (parts.Length > 10) dto.TerminalA = parts[10].Trim();
                            if (parts.Length > 11) dto.TerminalB = parts[11].Trim();
                            if (parts.Length > 14) dto.SealA = parts[14].Trim();
                            if (parts.Length > 15) dto.SealB = parts[15].Trim();
                            if (parts.Length > 42) dto.KombinasiWire = parts[42].Trim();
                            
                            result.Add(dto);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MachineFileRepository] Error reading Product.csv: {ex.Message}");
            }
            return result;
        }
    }
}
