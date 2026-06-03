using System.Data;

namespace mtc_app.features.micrometer_patrol.presentation.controllers
{
    public interface IMicrometerPatrolHistoryView
    {
        void ShowLoading();
        void HideLoading();
        void SetStatusMessage(string message, bool isError = false);
        void DisplayData(DataTable data);
    }
}
