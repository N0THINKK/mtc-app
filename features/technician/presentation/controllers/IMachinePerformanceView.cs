using System;
using System.Collections.Generic;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.presentation.controllers
{
    public interface IMachinePerformanceView
    {
        string SelectedArea { get; }
        bool SortAscending { get; }
        
        void UpdateGrid(List<MachinePerformanceDto> data);
        void ShowError(string message);
    }
}
