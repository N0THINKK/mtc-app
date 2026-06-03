using System;

namespace mtc_app.features.technician.data.dtos
{
    public class MachineMonitorDto
    {
        public int MachineId { get; set; }
        public string MachineName { get; set; }
        public string MachineNum { get; set; }
        public int TypeId { get; set; }
        public int AreaId { get; set; }
        
        // Arrays for 14 hours (safety margin)
        public long[] HourlyPieces { get; set; } = new long[14];
        
        // Results
        public long TotalPieces { get; set; }
        public double AveragePerHour { get; set; }
        public int TargetPerHour { get; set; }
        
        public double AutoTime { get; set; }
        public double MonitorTime { get; set; }
        public double PlannedStopMinutes { get; set; }
        public double SuddenStopMinutes { get; set; }
        
        // Calculated efficiency
        public double Efficiency => MonitorTime > 0 ? (AutoTime / MonitorTime) * 100 : 0;
    }

    public class ShiftBreakDto
    {
        public int NonOtMinutes { get; set; }
        public int OtMinutes { get; set; }
    }

    public class MachineProcessLogAggregateDto
    {
        public int MachineId { get; set; }
        public int HourIndex { get; set; }
        public long FirstPieces { get; set; }
        public long LastPieces { get; set; }
        public long MaxPieces { get; set; }
        public double MaxAuto { get; set; }
        public double MaxMonitor { get; set; }
    }

    public class MachineDowntimeDto
    {
        public int MachineId { get; set; }
        public double PlannedMin { get; set; }
        public double SuddenMin { get; set; }
    }
}
