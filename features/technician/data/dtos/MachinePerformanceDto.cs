using System;

namespace mtc_app.features.technician.data.dtos
{
    public class MachinePerformanceDto
    {
        public string MachineName { get; set; }
        
        // Durations in seconds for easy aggregation
        public double TotalDowntimeSeconds { get; set; }
        public double ResponseDurationSeconds { get; set; }
        public double RepairDurationSeconds { get; set; }
        public double OperatorWaitDurationSeconds { get; set; }
        public double PartWaitDurationSeconds { get; set; }

        public int RepairCount { get; set; }
    }
}
