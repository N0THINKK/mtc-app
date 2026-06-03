using System;
using System.Collections.Generic;
using mtc_app.features.stock.data.dtos;

namespace mtc_app.features.technician.presentation.controllers
{
    public interface IStockDataView
    {
        void UpdateStats(StockStatsDto stats);
        void DisplayRequests(List<PartRequestDto> requests);
        void ShowError(string message);
    }
}
