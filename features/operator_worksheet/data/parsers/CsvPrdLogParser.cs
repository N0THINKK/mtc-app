using System;
using System.Collections.Generic;
using System.IO;
using mtc_app.features.operator_worksheet.data.dtos;

namespace mtc_app.features.operator_worksheet.data.parsers
{
    public class CsvPrdLogParser : IPrdLogParser
    {
        public bool IsXmlSource => false;

        public List<PrdLogDto> Parse(string filePath)
        {
            var result = new List<PrdLogDto>();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return result;

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
                System.Diagnostics.Debug.WriteLine($"[CsvPrdLogParser] Error reading PrdLog.csv: {ex.Message}");
            }
            return result;
        }
    }
}
