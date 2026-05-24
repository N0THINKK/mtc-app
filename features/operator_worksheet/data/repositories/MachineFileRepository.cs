using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using mtc_app.features.operator_worksheet.data.dtos;

namespace mtc_app.features.operator_worksheet.data.repositories
{
    public class MachineFileRepository
    {
        private readonly string _baseDir;

        public MachineFileRepository(string baseDir = null)
        {
            if (baseDir != null)
            {
                _baseDir = baseDir;
            }
            else
            {
                if (Directory.Exists(@"C:\AC90HMI\prg\"))
                    _baseDir = @"C:\AC90HMI\prg\";
                else if (Directory.Exists(@"C:\AC80HMI\"))
                    _baseDir = @"C:\AC80HMI\";
                else
                    _baseDir = @"C:\AC90HMI\prg\";
            }
        }

        /// <summary>
        /// Flag apakah data PrdLog terakhir yang dibaca berasal dari XML (AC95).
        /// Digunakan oleh UI untuk mengganti label kolom "Urutan" → "Waktu".
        /// </summary>
        public bool IsXmlSource { get; private set; } = false;

        public List<PrdLogDto> GetPrdLogs()
        {
            var result = new List<PrdLogDto>();
            string filePath = Path.Combine(_baseDir, "PrdLog.csv");

            // Jika PrdLog.csv ada (AC90), baca dari CSV
            if (File.Exists(filePath))
            {
                IsXmlSource = false;
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading PrdLog.csv: {ex.Message}");
                }
                return result;
            }

            // Fallback: cek ProductionLog.xml untuk AC95
            string[] xmlFallbacks = new[]
            {
                @"D:\AC95\prg\HMI\RelationalData\ProductionLog.xml",
                @"C:\AC95\prg\HMI\RelationalData\ProductionLog.xml"
            };

            string xmlPath = null;
            foreach (var fb in xmlFallbacks)
            {
                if (File.Exists(fb)) { xmlPath = fb; break; }
            }

            if (xmlPath == null) return result;

            IsXmlSource = true;
            try
            {
                XDocument doc;
                using (var fs = new FileStream(xmlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    doc = XDocument.Load(fs);
                }

                // Namespace dari XML: http://schemas.datacontract.org/2004/07/AWP_HMI.Models.Production.Private
                XNamespace ns = doc.Root.GetDefaultNamespace();

                foreach (var el in doc.Root.Elements(ns + "ProductionLog"))
                {
                    string jobNo = el.Element(ns + "_jobNo")?.Value?.Trim() ?? "";
                    string endDt = el.Element(ns + "_endDateTime")?.Value?.Trim() ?? "";
                    string startDt = el.Element(ns + "_startDateTime")?.Value?.Trim() ?? "";
                    string goodPiece = el.Element(ns + "_goodPiece")?.Value?.Trim() ?? "0";
                    string missPiece = el.Element(ns + "_missPiece")?.Value?.Trim() ?? "0";

                    // Format datetime agar ringkas untuk kolom "Waktu"
                    string displayEnd = endDt;
                    if (DateTime.TryParse(endDt, out DateTime dtEnd))
                        displayEnd = dtEnd.ToString("HH:mm:ss");

                    string displayStart = startDt;
                    if (DateTime.TryParse(startDt, out DateTime dtStart))
                        displayStart = dtStart.ToString("HH:mm:ss");

                    result.Add(new PrdLogDto
                    {
                        Sequen = jobNo,
                        UrutanPengerjaan = displayEnd,          // Di AC95: kolom "Urutan" diganti "Waktu" (end time)
                        WaktuMulaiPengerjaan = displayStart,
                        WaktuSelesaiPengerjaan = displayEnd,
                        QtyProduk = goodPiece,
                        QtyDefect = missPiece
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading ProductionLog.xml: {ex.Message}");
            }

            return result;
        }

        public List<PrdmstDto> GetPrdMst()
        {
            var result = new List<PrdmstDto>();
            string filePath = Path.Combine(_baseDir, "prdmst.csv");
            
            if (!File.Exists(filePath))
            {
                string[] fallbacks = new[]
                {
                    @"D:\AC95\Kanban\prdmst.csv",
                    @"C:\AC95\Kanban\prdmst.csv",
                    @"C:\AC80HMI\prdmst.csv",
                    @"C:\AC90HMI\prg\prdmst.csv",
                    @"C:\AC90 Master Paper\prdmst.csv"
                };

                bool found = false;
                foreach (var fb in fallbacks)
                {
                    if (File.Exists(fb))
                    {
                        filePath = fb;
                        found = true;
                        break;
                    }
                }

                if (!found) return result;
            }

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

            // Fallback: cek di beberapa path alternatif
            if (!File.Exists(filePath))
            {
                string[] fallbacks = new[]
                {
                    @"C:\AC90HMI\Jissk.dat",
                    @"D:\AC95\Backup Jissk\jissk.dat",
                    @"C:\AC95\Backup Jissk\jissk.dat",
                    @"C:\AC80HMI\Jissk.dat"
                };

                bool found = false;
                foreach (var fb in fallbacks)
                {
                    System.Diagnostics.Debug.WriteLine($"[JISSK] Trying fallback: {fb}, exists: {File.Exists(fb)}");
                    if (File.Exists(fb))
                    {
                        filePath = fb;
                        found = true;
                        break;
                    }
                }

                if (!found) return result;
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

                        // Ambil sequence number.
                        // File Jissk.dat menggunakan fixed-width format.
                        // Karakter index 7 s.d 11 adalah sequence number (panjang 5).
                        // Contoh AC90: "**2399 80030" -> "80030"
                        // Contoh AC95: "10  49 7 247" -> "7 247" -> "7247", "10  49 79000" -> "79000"
                        string rawSeq = "";
                        if (line.Length >= 12)
                        {
                            // Area sequence ada di index 7 dengan panjang 5.
                            // Contoh AC90: "80030" -> split last: "80030"
                            // Contoh AC95: "7 247" -> split last: "247"
                            // Contoh AC95: "79000" -> split last: "79000"
                            string seqArea = line.Substring(7, 5);
                            var areaTokens = seqArea.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (areaTokens.Length > 0)
                            {
                                rawSeq = areaTokens.Last();
                            }
                        }
                        
                        if (string.IsNullOrEmpty(rawSeq))
                        {
                            // Fallback jika baris terlalu pendek atau seqArea kosong
                            var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (tokens.Length < 2) continue;
                            rawSeq = tokens[1].Trim();
                        }

                        // Ambil 4 digit terakhir dan pastikan terformat 4 digit (contoh: "247" menjadi "0247")
                        string seq4 = rawSeq;
                        if (seq4.Length > 4) 
                        {
                            seq4 = seq4.Substring(seq4.Length - 4);
                        }
                        seq4 = seq4.PadLeft(4, '0');

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

        public List<ProductDto> GetProductSequences()
        {
            var result = new List<ProductDto>();
            string filePath = Path.Combine(_baseDir, "Product.csv");
            
            if (!File.Exists(filePath))
            {
                // Coba path lain per tipe mesin
                string[] searchPaths = new[]
                {
                    Path.Combine(_baseDir, "product.csv"),              // AC80 (huruf kecil)
                    @"D:\AC95\Product\Product.csv",                     // AC95 (drive D)
                    @"C:\AC95\Product\Product.csv",                     // AC95 (drive C fallback)
                    @"C:\AC80HMI\product.csv",                          // AC80
                    @"C:\AC80HMI\Product.csv"                           // AC80 (huruf besar)
                };
                
                foreach (var sp in searchPaths)
                {
                    if (File.Exists(sp)) { filePath = sp; break; }
                }
            }
            
            if (!File.Exists(filePath)) return result;

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
                            // Kolom ke-37 (indeks 36) menentukan status: 0/1 = tampilkan, 2 = sudah masuk prdlog/prdmst
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
                Console.WriteLine($"Error reading Product.csv: {ex.Message}");
            }
            return result;
        }
    }
}
