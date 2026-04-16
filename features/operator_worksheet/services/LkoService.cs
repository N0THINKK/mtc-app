using System;
using System.Collections.Generic;
using System.Linq;
using mtc_app.features.operator_worksheet.data.dtos;
using mtc_app.features.operator_worksheet.data.repositories;

namespace mtc_app.features.operator_worksheet.services
{
    public class LkoService
    {
        private readonly MachineFileRepository _repository;

        public LkoService()
        {
            _repository = new MachineFileRepository();
        }

        public class LkoAggregatedData
        {
            public PrdLogDto Log { get; set; }
            public PrdmstDto Master { get; set; }
            
            // Flat properties for UI Databinding
            public string DisplaySequen => Master?.Sequen ?? Log?.Sequen ?? string.Empty;
            public string DisplayUrutanPengerjaan => Log?.UrutanPengerjaan ?? string.Empty;
            public string DisplayKombinasi => Master?.KombinasiWire ?? string.Empty;
            public string DisplayTermA => Master?.TerminalA ?? string.Empty;
            public string DisplayTermB => Master?.TerminalB ?? string.Empty;
            public string DisplayQty => Master?.Qty ?? string.Empty;
            public string DisplayQtyProduk => Log?.QtyProduk ?? string.Empty;
            public string DisplayQtyDefect => Log?.QtyDefect ?? string.Empty;
            public string DisplayWaktuMulai => Log?.WaktuMulaiPengerjaan ?? string.Empty;
            public string DisplayWaktuSelesai => Log?.WaktuSelesaiPengerjaan ?? string.Empty;
        }

        public List<LkoAggregatedData> GetAllWorksheetData()
        {
            var logs = _repository.GetPrdLogs();
            var masters = _repository.GetPrdMst();

            var result = new List<LkoAggregatedData>();

            // Join based on Sequen
            foreach (var master in masters)
            {
                var matchingLog = logs.FirstOrDefault(l => l.Sequen == master.Sequen) 
                    ?? new PrdLogDto { Sequen = master.Sequen }; // create empty log if not found

                result.Add(new LkoAggregatedData
                {
                    Master = master,
                    Log = matchingLog
                });
            }

            return result;
        }

        public void SaveWorksheet(PrdLogDto log)
        {
            _repository.UpdatePrdLog(log);
        }
    }
}
