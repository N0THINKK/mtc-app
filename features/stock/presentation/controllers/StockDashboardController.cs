using System;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.stock.data.repositories;

namespace mtc_app.features.stock.presentation.controllers
{
    public class StockDashboardController
    {
        private readonly IStockDashboardView _view;
        private readonly IStockRepository _repository;
        private int _previousPendingCount = 0;
        private bool _isNotificationShowing = false;
        private bool _isInitialLoad = true;

        public StockDashboardController(IStockDashboardView view, IStockRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var statsTask = _repository.GetStatsAsync();
                var requestsTask = _repository.GetRequestsAsync(_view.CurrentFilter, _view.CurrentSort);

                await Task.WhenAll(statsTask, requestsTask);
                
                var newStats = statsTask.Result;
                var requests = requestsTask.Result.ToList();
                
                // NOTIFICATION LOGIC
                if (!_isInitialLoad && newStats.PendingCount > _previousPendingCount && !_isNotificationShowing)
                {
                    _isNotificationShowing = true;
                    var latestRequest = requests.FirstOrDefault();
                    string partName = latestRequest != null ? latestRequest.PartDisplayName : "Barang Tidak Dikenal";
                    
                    _view.ShowNotification(partName);
                    
                    _isNotificationShowing = false;
                }
                
                _previousPendingCount = newStats.PendingCount;
                _isInitialLoad = false;

                _view.UpdateStats(newStats);
                _view.DisplayRequests(requests);
                _view.UpdateLastUpdateTime(DateTime.Now.ToString("HH:mm:ss"));
            }
            catch (Exception ex)
            {
                _view.ShowError($"Error memuat data: {ex.Message}");
            }
        }

        public async Task MarkAsReadyAsync(int requestId)
        {
            if (_view.ShowConfirmation("Konfirmasi", "Tandai barang sebagai SIAP?"))
            {
                bool success = await _repository.MarkAsReadyAsync(requestId);
                if (success)
                {
                    _view.ShowSuccess("Berhasil ditandai siap.");
                    await LoadDataAsync();
                }
            }
        }

        public async Task RejectRequestAsync(int requestId)
        {
            if (_view.ShowConfirmation("Konfirmasi Tolak", "Apakah Anda yakin ingin MENOLAK permintaan part ini?\nTeknisi akan diberitahu."))
            {
                bool success = await _repository.RejectRequestAsync(requestId);
                if (success)
                {
                    _view.ShowSuccess("Permintaan berhasil ditolak.");
                    await LoadDataAsync();
                }
            }
        }
    }
}
