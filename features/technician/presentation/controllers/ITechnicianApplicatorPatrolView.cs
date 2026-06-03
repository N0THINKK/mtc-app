using System;
using System.Collections.Generic;
using mtc_app.features.applicator_patrol.data.dtos;

namespace mtc_app.features.technician.presentation.controllers
{
    public interface ITechnicianApplicatorPatrolView
    {
        string CurrentSort { get; }
        
        void UpdateStats(int totalNgCount);
        void UpdateGrid(List<ApplicatorNgDto> patrols);
        void ShowEmptyState();
        void HideEmptyState();
        void ShowError(string message);
    }
}
