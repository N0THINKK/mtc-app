using System;
using System.Collections.Generic;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.presentation.controllers
{
    public interface IMachineMonitorView
    {
        string SelectedArea { get; }
        string SelectedMetric { get; }
        string SelectedSort { get; }
        int SelectedShiftIndex { get; }
        DateTime SelectedDate { get; }

        void UpdateStatus(string text);
        void UpdateChart(List<MachineMonitorDto> data, string metric, int currentHourCount, int maxBreakMinutes);
        void SetLoadingState(bool isLoading);
        
        // Background cache operations
        void PreloadAllAreasBackground(DateTime shiftStart, DateTime shiftEnd);
        void NotifyCacheReady(DateTime shiftStart, DateTime shiftEnd, Dictionary<int, List<MachineProcessLogAggregateDto>> cache, Dictionary<int, List<MachineDowntimeDto>> downtimeCache);
        bool IsBackgroundCacheReady(DateTime shiftStart, DateTime shiftEnd);
        Dictionary<int, List<MachineProcessLogAggregateDto>> GetBackgroundCache();
        Dictionary<int, List<MachineDowntimeDto>> GetBackgroundDowntimeCache();
    }
}
