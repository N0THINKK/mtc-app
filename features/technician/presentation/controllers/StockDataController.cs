using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.stock.data.dtos;
using mtc_app.features.stock.data.repositories;

namespace mtc_app.features.technician.presentation.controllers
{
    public class StockDataController
    {
        private readonly IStockDataView _view;
        private readonly IStockRepository _repository;

        public StockDataController(IStockDataView view, IStockRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            try
            {
                var statsTask = _repository.GetStatsByDateAsync(start, end);
                var requestsTask = _repository.GetRequestsByDateAsync(start, end);
                await Task.WhenAll(statsTask, requestsTask);

                _view.UpdateStats(statsTask.Result);
                _view.DisplayRequests(requestsTask.Result.ToList());
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal memuat data part: {ex.Message}");
            }
        }
    }
}
