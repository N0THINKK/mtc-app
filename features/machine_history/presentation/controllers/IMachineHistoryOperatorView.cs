using System;
using System.Collections.Generic;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public interface IMachineHistoryOperatorView
    {
        // Inputs
        string OperatorNik { get; }
        string Shift { get; }
        string Applicator { get; }
        List<(string ProblemType, string ProblemDetail)> GetProblems();
        
        // Filter Inputs
        DateTime FilterStartDate { get; }
        DateTime FilterEndDate { get; }
        string FilterArea { get; }

        // UI Updates
        void PopulateShifts(string[] shifts);
        void PopulateApplicators(string[] applicators);
        void PopulateAreas(string[] areas);
        void SetHistoryData(List<MachineHistoryDto> history);
        
        // Pending Ticket Indicator
        void ShowPendingTicket(string statusName);
        void HidePendingTicket();
        
        // Navigation / Interaction
        void OpenTechnicianForm(long ticketId);
        void ShowError(string message, string title = "Error");
        void ShowSuccess(string message, string title = "Sukses", int autoCloseMs = 2000);
        void ShowWarning(string message, string title = "Peringatan");
    }
}
