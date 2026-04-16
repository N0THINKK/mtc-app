using System;
using System.Collections.Generic;
using System.IO;
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
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split(',');
                    if (parts.Length >= 11)
                    {
                        result.Add(new PrdmstDto
                        {
                            UrutanSequen = parts[0]?.Trim(),
                            Sequen = parts[1]?.Trim(),
                            CutLength = parts[2]?.Trim(),
                            Qty = parts[3]?.Trim(),
                            PanjangStripSisiA = parts[4]?.Trim(),
                            PanjangStripSisiB = parts[5]?.Trim(),
                            TerminalA = parts[6]?.Trim(),
                            TerminalB = parts[7]?.Trim(),
                            MunculkanGambarTermA = parts[8]?.Trim(),
                            MunculkanGambarTermB = parts[9]?.Trim(),
                            KombinasiWire = parts[10]?.Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading prdmst.csv: {ex.Message}");
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
