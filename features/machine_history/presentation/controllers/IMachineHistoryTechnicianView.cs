using System;
using System.Collections.Generic;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public interface IMachineHistoryTechnicianView
    {
        // State Properties
        long CurrentTicketId { get; set; }
        bool IsVerified { get; set; }
        
        // UI Updates
        void UpdateMachineStateIndicator(int isMachineRunning);
        void UpdateTimerDisplay(int arrivalSeconds, int repairSeconds);
        void ShowPreviousSessions(List<string> sessionLines);
        void AddProblemItem(long problemId, string problemType, string problemDetail, bool isVerified, int index);
        
        // Notifications / Dialogs
        void ShowError(string message, string title = "Error");
        void ShowInfo(string message, string title = "Info");
        bool ShowConfirm(string message, string title = "Konfirmasi");
        
        // Threading
        void RunOnUIThread(Action action);
    }
}
