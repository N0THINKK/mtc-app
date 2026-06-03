using System.Data;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public interface IChecksheetHistoryView
    {
        void ShowLoading();
        void HideLoading();
        void SetStatusMessage(string message, bool isError = false);
        void DisplayData(DataTable data);
    }
}
