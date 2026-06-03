using System.Collections.Generic;
using mtc_app.features.stock.data.dtos;
using mtc_app.features.stock.data.enums;

namespace mtc_app.features.stock.presentation.controllers
{
    public interface IStockDashboardView
    {
        RequestStatus CurrentFilter { get; }
        SortOrder CurrentSort { get; }
        
        void UpdateStats(StockStatsDto stats);
        void DisplayRequests(List<PartRequestDto> requests);
        void ShowNotification(string partName);
        void UpdateEmptyStateMessage();
        void ShowError(string message);
        void ShowSuccess(string message);
        bool ShowConfirmation(string title, string message);
        void UpdateLastUpdateTime(string timeString);
    }
}
