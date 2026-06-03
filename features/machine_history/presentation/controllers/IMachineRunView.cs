namespace mtc_app.features.machine_history.presentation.controllers
{
    public interface IMachineRunView
    {
        void UpdateStopwatchDisplay(string timeString);
        void ShowError(string message, string title = "Error");
        void CloseForm(bool success);
        void OpenRatingForm(long ticketId);
    }
}
