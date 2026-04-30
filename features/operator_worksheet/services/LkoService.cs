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
            public JisskDto Jissk { get; set; }
            
            // === Data dari DB (Operator input) ===
            public LkoRecordDto DbRecord { get; set; }

            // === Offline flag ===
            public bool IsOffline { get; set; } = false;
            
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
            var jisskData = _fileRepository.GetJisskData();
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

            // Pre-build dictionary untuk lookup O(1) alih-alih O(n) per baris
            var masterDict = new Dictionary<string, PrdmstDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in masters)
            {
                if (!string.IsNullOrWhiteSpace(m.UrutanSequen))
                {
                    string key = m.UrutanSequen.Trim();
                    if (!masterDict.ContainsKey(key)) masterDict[key] = m;
                }
            }

            // Pre-build dictionary untuk Jissk lookup
            var jisskDict = new Dictionary<string, List<JisskDto>>();
            foreach (var j in jisskData)
            {
                if (!jisskDict.ContainsKey(j.Sequen4))
                    jisskDict[j.Sequen4] = new List<JisskDto>();
                jisskDict[j.Sequen4].Add(j);
            }

            foreach (var log in logs)
            {
                PrdmstDto matchingMaster = null;

                if (expectedPrefix != '\0' && !string.IsNullOrWhiteSpace(log.Sequen))
                {
                    string logDigits = new string(log.Sequen.Where(char.IsDigit).ToArray());
                    
                    if (int.TryParse(logDigits, out int seqNumber))
                    {
                        string targetUrutan = $"{expectedPrefix}{seqNumber:D4}";
                        masterDict.TryGetValue(targetUrutan, out matchingMaster);
                    }
                }

                JisskDto matchingJissk = null;
                if (!string.IsNullOrWhiteSpace(log.Sequen))
                {
                    string seqDigits = new string(log.Sequen.Where(char.IsDigit).ToArray());
                    if (int.TryParse(seqDigits, out int seqNum))
                    {
                        string padded = seqNum.ToString("D4");
                        if (jisskDict.TryGetValue(padded, out var jList))
                        {
                            matchingJissk = jList.FirstOrDefault(j => j.FrontChA != "0" && j.FrontChA != "0.000")
                                         ?? jList.FirstOrDefault();
                        }
                    }
                }

                result.Add(new LkoAggregatedData
                {
                    Log = log,
                    Master = matchingMaster ?? new PrdmstDto { Sequen = log.Sequen },
                    Jissk = matchingJissk
                });
            }

            // Juga tambahkan master yang tidak ada di log (belum dikerjakan)
            foreach (var master in masters)
            {
                bool alreadyExists = result.Any(r => r.Master?.Sequen == master.Sequen);
                if (!alreadyExists)
                {
                    JisskDto jForMaster = null;
                    string mSeqDigits = new string((master.Sequen ?? "").Where(char.IsDigit).ToArray());
                    if (int.TryParse(mSeqDigits, out int mSeqNum))
                    {
                        string mPadded = mSeqNum.ToString("D4");
                        if (jisskDict.TryGetValue(mPadded, out var jList))
                        {
                            jForMaster = jList.FirstOrDefault(j => j.FrontChA != "0" && j.FrontChA != "0.000")
                                       ?? jList.FirstOrDefault();
                        }
                    }

                    result.Add(new LkoAggregatedData
                    {
                        Log = new PrdLogDto { Sequen = master.Sequen },
                        Master = master,
                        Jissk = jForMaster
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Muat data DB records hari ini dan gabungkan ke data worksheet.
        /// Juga merge data offline jika ada.
        /// </summary>
        public async System.Threading.Tasks.Task MergeDbRecordsAsync(List<LkoAggregatedData> data, string noMesin)
        {
            // 1) Try to merge from MySQL
            bool isOnline = false;
            try
            {
                var dbRecords = await _dbRepository.GetTodayRecordsAsync(noMesin);
                isOnline = true;

                var consumedIds = new HashSet<int>();
                
                foreach (var item in data)
                {
                    if (string.IsNullOrWhiteSpace(item.DisplaySequen)) continue;
                    
                    var dbMatch = dbRecords.FirstOrDefault(r =>
                        !consumedIds.Contains(r.Id) &&
                        !string.IsNullOrWhiteSpace(r.Sequen) &&
                        r.Sequen == item.DisplaySequen &&
                        (r.UrutanKanban ?? "") == (item.DisplayUrutanPengerjaan ?? ""));

                    if (dbMatch != null)
                    {
                        item.DbRecord = dbMatch;
                        item.IsOffline = false;
                        consumedIds.Add(dbMatch.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MergeDbRecords (MySQL) error: {ex.Message}");
            }

            // 2) Also merge offline records (for items not yet matched from DB)
            try
            {
                var offlineRecords = LkoOfflineQueue.GetPendingForMachine(noMesin);
                foreach (var offRec in offlineRecords)
                {
                    var match = data.FirstOrDefault(d =>
                        d.DbRecord == null &&
                        d.DisplaySequen == offRec.Sequen &&
                        (d.DisplayUrutanPengerjaan ?? "") == (offRec.UrutanKanban ?? ""));

                    if (match != null)
                    {
                        match.DbRecord = offRec;
                        match.IsOffline = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MergeDbRecords (offline) error: {ex.Message}");
            }

            // 3) If online, try to sync any pending offline records
            if (isOnline)
            {
                await SyncOfflineRecordsAsync();
            }
        }

        /// <summary>
        /// Simpan record: coba MySQL dulu, jika gagal simpan ke antrian offline.
        /// Returns true if saved online, false if saved offline.
        /// </summary>
        public async System.Threading.Tasks.Task<bool> SaveToDatabase(LkoRecordDto record)
        {
            try
            {
                await _dbRepository.SaveLkoRecordAsync(record);
                return true; // Saved to MySQL
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveToDatabase (MySQL) failed: {ex.Message}. Saving offline.");
                LkoOfflineQueue.Enqueue(record);
                return false; // Saved offline
            }
        }

        /// <summary>
        /// Sync semua record offline ke MySQL.
        /// Returns jumlah record yang berhasil di-sync.
        /// </summary>
        public async System.Threading.Tasks.Task<int> SyncOfflineRecordsAsync()
        {
            var pending = LkoOfflineQueue.GetPending();
            if (pending.Count == 0) return 0;

            int synced = 0;
            foreach (var record in pending.ToList())
            {
                try
                {
                    await _dbRepository.SaveLkoRecordAsync(record);
                    LkoOfflineQueue.Remove(record);
                    synced++;
                }
                catch
                {
                    // Still offline, stop trying
                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[LKO] Synced {synced}/{pending.Count} offline records.");
            return synced;
        }
    }
}
