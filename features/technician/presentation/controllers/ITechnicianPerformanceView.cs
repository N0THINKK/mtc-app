using System;
using System.Collections.Generic;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.presentation.controllers
{
    public interface ITechnicianPerformanceView
    {
        string CurrentMetric { get; }
        bool SortAscending { get; }
        
        void UpdateStats(int totalRepairs, decimal avgRating);
        void UpdateGrid(List<TechnicianPerformanceDto> data);
        void ShowError(string message);
    }
}
