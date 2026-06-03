using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.operator_worksheet.services;
using mtc_app.shared.data.dtos;
using mtc_app.features.operator_worksheet.data.dtos;
using mtc_app.features.operator_worksheet.data.repositories;
using mtc_app.features.operator_worksheet.data.providers;

namespace mtc_app.features.operator_worksheet.presentation.controllers
{
    public interface IOperatorWorksheetView
    {
        string GetEffectiveMachineNumber();
        void UpdateSequenGrid(List<LkoService.LkoAggregatedData> data);
        void UpdateProductGrid(List<ProductDto> pendingProducts);
        void UpdateTersimpanGrid(List<LkoRecordDto> tersimpanData);
        void UpdateHeaderQty(int grossSum, int netSum, int defectSum);
        void SetIsXmlSource(bool isXml);
        void RunOnUIThread(Action action);
    }

    public class OperatorWorksheetController
    {
        private readonly IOperatorWorksheetView _view;
        private readonly LkoService _lkoService;
        private List<LkoService.LkoAggregatedData> _worksheetData = new List<LkoService.LkoAggregatedData>();

        public OperatorWorksheetController(IOperatorWorksheetView view)
        {
            _view = view;
            _lkoService = new LkoService();
        }

        public async Task LoadAllDataAsync()
        {
            try
            {
                string machineNumber = _view.GetEffectiveMachineNumber();

                var data = await Task.Run(() => _lkoService.GetAllWorksheetData(machineNumber));
                
                // Reverse urutan dari file: baris terbawah di file muncul paling atas di UI
                data.Reverse();
                _worksheetData = data;

                var pendingProducts = await Task.Run(() => _lkoService.GetPendingProductSequences());

                var allRecords = await FetchTersimpanDataAsync(machineNumber);

                // Merge DbRecords ke WorksheetData
                MergeDbRecordsToWorksheetData(allRecords);

                // Update View
                _view.RunOnUIThread(() =>
                {
                    _view.SetIsXmlSource(_lkoService.IsXmlSource);
                    _view.UpdateSequenGrid(_worksheetData);
                    _view.UpdateProductGrid(pendingProducts.Cast<ProductDto>().ToList());
                    _view.UpdateTersimpanGrid(allRecords);
                    CalculateAndUpdateQty();
                });

                // Sync offline records
                await SyncOfflineRecordsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadAllDataAsync error: {ex.Message}");
            }
        }

        public async Task LoadProductDataOnlyAsync()
        {
            try
            {
                var pendingProducts = await Task.Run(() => _lkoService.GetPendingProductSequences());
                _view.RunOnUIThread(() =>
                {
                    _view.UpdateProductGrid(pendingProducts.Cast<ProductDto>().ToList());
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadProductDataOnlyAsync error: {ex.Message}");
            }
        }

        private async Task<List<LkoRecordDto>> FetchTersimpanDataAsync(string effMesin)
        {
            var allRecords = new List<LkoRecordDto>();

            // 1) Coba ambil dari MySQL
            try
            {
                var repo = new LkoRepository();
                var dbRecords = await repo.GetTodayRecordsAsync(effMesin);
                allRecords.AddRange(dbRecords);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FetchTersimpan (MySQL) error: {ex.Message}");
            }

            // 2) Tambahkan offline records yang belum ada di MySQL
            try
            {
                var offlineRecords = LkoOfflineQueue.GetPendingForMachine(effMesin);
                foreach (var offRec in offlineRecords)
                {
                    bool alreadyInDb = allRecords.Any(r =>
                        r.Sequen == offRec.Sequen &&
                        (r.UrutanKanban ?? "") == (offRec.UrutanKanban ?? "") &&
                        (r.WaktuMulai ?? "") == (offRec.WaktuMulai ?? ""));
                    if (!alreadyInDb) allRecords.Add(offRec);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FetchTersimpan (offline) error: {ex.Message}");
            }

            return allRecords;
        }

        private void MergeDbRecordsToWorksheetData(List<LkoRecordDto> allRecords)
        {
            if (_worksheetData == null || allRecords.Count == 0) return;

            var consumedIds = new HashSet<int>();
            foreach (var item in _worksheetData)
            {
                if (string.IsNullOrWhiteSpace(item.DisplaySequen)) continue;
                var match = allRecords.FirstOrDefault(r =>
                    !consumedIds.Contains(r.Id) &&
                    !string.IsNullOrWhiteSpace(r.Sequen) &&
                    r.Sequen == item.DisplaySequen &&
                    (r.UrutanKanban ?? "") == (item.DisplayUrutanPengerjaan ?? "") &&
                    (r.WaktuMulai ?? "") == (item.Log?.WaktuMulaiPengerjaan ?? ""));
                
                if (match != null)
                {
                    item.DbRecord = match;
                    consumedIds.Add(match.Id);
                }
            }
        }

        private async Task SyncOfflineRecordsAsync()
        {
            try
            {
                int synced = await _lkoService.SyncOfflineRecordsAsync();
                if (synced > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Synced {synced} offline records to MySQL");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SyncOffline error: {ex.Message}");
            }
        }

        private void CalculateAndUpdateQty()
        {
            if (_worksheetData == null) return;
            
            int grossSum = 0;
            int defectSum = 0;

            int shiftStartIndex = -1;
            for (int i = 0; i < _worksheetData.Count; i++)
            {
                var seq = _worksheetData[i].DisplaySequen ?? "";
                if (seq.StartsWith("9"))
                {
                    shiftStartIndex = i;
                    break;
                }
            }

            if (shiftStartIndex >= 0)
            {
                for (int i = 0; i <= shiftStartIndex; i++)
                {
                    if (_worksheetData[i].DbRecord != null)
                    {
                        grossSum += _worksheetData[i].DbRecord.QtyProduct;
                        defectSum += _worksheetData[i].DbRecord.QtyDefectOperator;
                    }
                }
            }
            
            int netSum = grossSum - defectSum;
            _view.UpdateHeaderQty(grossSum, netSum, defectSum);
        }
    }
}
