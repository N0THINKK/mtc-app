using System.Collections.Generic;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public interface IOperatorMainMenuView
    {
        void SetRunState();
        void SetIdleState(string activityName);
        void UpdateQuickCountDisplay(string itemName, int count);
        void UpdateJamDisplay(int viewedHour, int currentTrackedHour, int shiftHourDisplay);
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Peringatan");
        
        void OpenHistoryForm();
        void OpenChecksheetForm();
        void OpenApplicatorPatrolForm();
        void OpenMicrometerPatrolForm();
        void OpenOperatorWorksheetForm();
        void OpenActivitySelectionDialog();
        void TriggerSync();
    }
}
