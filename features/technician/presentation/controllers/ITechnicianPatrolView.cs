using System;
using System.Collections.Generic;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.presentation.controllers
{
    public interface ITechnicianPatrolView
    {
        string CurrentFilter { get; }
        string CurrentSort { get; }
        string CurrentRoleFilter { get; }
        string CurrentItemFilter { get; }
        
        void UpdateStats(int pendingCount, int resolvedCount);
        void UpdateGrid(List<PatrolNgDto> patrols);
        void UpdateItemFilterList(List<string> items, string previousSelection);
        void ShowEmptyState(string title, string description);
        void HideEmptyState();
        void ShowError(string message);
        void ShowSuccess(string message);
        void ShowWarning(string message);
        bool ConfirmAction(string title, string message);
    }
}
