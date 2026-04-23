using System;
using System.Collections.Generic;
using System.Linq;
using mtc_app.features.operator_worksheet.data.dtos;
using mtc_app.features.operator_worksheet.data.repositories;

namespace mtc_app.features.operator_worksheet.services
{
    public class LkoService
    {
        private readonly MachineFileRepository _fileRepository;
        private readonly ILkoRepository _dbRepository;

        public LkoService()
        {
            _fileRepository = new MachineFileRepository();
            _dbRepository = new LkoRepository();
        }

        public class LkoAggregatedData
        {
            public PrdLogDto Log { get; set; }
            public PrdmstDto Master { get; set; }
            
            // === Data dari DB (Operator input) ===
            public LkoRecordDto DbRecord { get; set; }
            
            // Flat properties for UI Databinding
            public string DisplaySequen => Master?.Sequen ?? Log?.Sequen ?? string.Empty;
            public string DisplayUrutanPengerjaan => Log?.UrutanPengerjaan ?? string.Empty;
            public string DisplayKombinasi => Master?.KombinasiWire ?? string.Empty;
            public string DisplayTermA => Master?.TerminalA ?? string.Empty;
            public string DisplayTermB => Master?.TerminalB ?? string.Empty;
            public string DisplayQty => Master?.Qty ?? string.Empty;
            public string DisplayQtyProduk => Log?.QtyProduk ?? string.Empty;
            public string DisplayQtyDefectMesin => Log?.QtyDefect ?? string.Empty;
            public string DisplayQtyDefectOperator => DbRecord?.QtyDefectOperator.ToString() ?? "0";
            public string DisplayKodeDefect => DbRecord?.KodeDefect ?? string.Empty;
            public string DisplayWaktuMulai => Log?.WaktuMulaiPengerjaan ?? string.Empty;
            public string DisplayWaktuSelesai => Log?.WaktuSelesaiPengerjaan ?? string.Empty;
            public string DisplayWaktuSimpan => DbRecord?.WaktuSimpan.ToString("HH:mm:ss") ?? string.Empty;
        }

        /// <summary>
        /// Ambil SEMUA baris dari PrdLog.csv, join dengan PrdMst, dan join dengan DB records.
        /// Tidak ada filter unik - setiap baris PrdLog ditampilkan.
        /// </summary>
        public List<LkoAggregatedData> GetAllWorksheetData(string noMesin = "")
        {
            var logs = _fileRepository.GetPrdLogs();
            var masters = _fileRepository.GetPrdMst();

            var result = new List<LkoAggregatedData>();

            // Tentukan prefix mesin dari noMesin.
            // Format noMesin: "AC90.TRX-10" -> ambil angka setelah '-' -> 10 -> J
            char expectedPrefix = '\0'; // null char = tidak filter
            if (!string.IsNullOrWhiteSpace(noMesin) && noMesin.Contains("-"))
            {
                string afterDash = noMesin.Substring(noMesin.LastIndexOf('-') + 1);
                if (int.TryParse(afterDash, out int mId) && mId >= 1 && mId <= 26)
                {
                    expectedPrefix = (char)('A' + mId - 1);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[LKO] noMesin='{noMesin}', expectedPrefix='{expectedPrefix}'");
            System.Diagnostics.Debug.WriteLine($"[LKO] Total masters: {masters.Count}, Total logs: {logs.Count}");

            foreach (var log in logs)
            {
                PrdmstDto matchingMaster = null;

                if (expectedPrefix != '\0' && !string.IsNullOrWhiteSpace(log.Sequen))
                {
                    // Ambil angka murni dari log.Sequen (misal "50 BU2" jadi "50", " 50 " jadi "50")
                    string logDigits = new string(log.Sequen.Where(char.IsDigit).ToArray());
                    
                    if (int.TryParse(logDigits, out int seqNumber))
                    {
                        // Bangun format yang dicari, misal J0050
                        string targetUrutan = $"{expectedPrefix}{seqNumber:D4}";

                        // Match langsung dengan kolom pertama di PRDMST (m.UrutanSequen)
                        matchingMaster = masters.FirstOrDefault(m => 
                            !string.IsNullOrWhiteSpace(m.UrutanSequen) && 
                            m.UrutanSequen.Trim().Equals(targetUrutan, StringComparison.OrdinalIgnoreCase));
                    }
                }

                if (matchingMaster != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LKO] MATCH: log.Seq={log.Sequen} -> master.UrutSeq={matchingMaster.UrutanSequen}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LKO] NO MATCH: log.Seq={log.Sequen}");
                }

                result.Add(new LkoAggregatedData
                {
                    Log = log,
                    Master = matchingMaster ?? new PrdmstDto { Sequen = log.Sequen }
                });
            }

            // Juga tambahkan master yang tidak ada di log (belum dikerjakan)
            foreach (var master in masters)
            {
                bool alreadyExists = result.Any(r => r.Master?.Sequen == master.Sequen);
                if (!alreadyExists)
                {
                    result.Add(new LkoAggregatedData
                    {
                        Log = new PrdLogDto { Sequen = master.Sequen },
                        Master = master
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Muat data DB records hari ini dan gabungkan ke data worksheet.
        /// </summary>
        public async System.Threading.Tasks.Task MergeDbRecordsAsync(List<LkoAggregatedData> data, string noMesin)
        {
            try
            {
                var dbRecords = await _dbRepository.GetTodayRecordsAsync(noMesin);

                var consumedIds = new HashSet<int>();
                
                foreach (var item in data)
                {
                    // Skip jika sequen kosong
                    if (string.IsNullOrWhiteSpace(item.DisplaySequen)) continue;
                    
                    // Match berdasarkan sequen + urutan (exact match)
                    var dbMatch = dbRecords.FirstOrDefault(r =>
                        !consumedIds.Contains(r.Id) &&
                        !string.IsNullOrWhiteSpace(r.Sequen) &&
                        r.Sequen == item.DisplaySequen &&
                        (r.UrutanKanban ?? "") == (item.DisplayUrutanPengerjaan ?? ""));

                    if (dbMatch != null)
                    {
                        item.DbRecord = dbMatch;
                        consumedIds.Add(dbMatch.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MergeDbRecords error: {ex.Message}");
            }
        }

        /// <summary>
        /// Simpan record dari input operator ke MySQL.
        /// </summary>
        public async System.Threading.Tasks.Task<int> SaveToDatabase(LkoRecordDto record)
        {
            return await _dbRepository.SaveLkoRecordAsync(record);
        }
    }
}
