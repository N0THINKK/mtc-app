using System;

namespace mtc_app.features.machine_history.data.dtos
{
    public class MachineHistoryDto
    {
        public long TicketId { get; set; }
        public string TicketCode { get; set; }
        public string MachineName { get; set; }
        public string TechnicianName { get; set; }
        public string OperatorName { get; set; }
        public string Issue { get; set; } // Combined failure details
        public string Resolution { get; set; } // Combined action details
        public DateTime CreatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
    }
}
