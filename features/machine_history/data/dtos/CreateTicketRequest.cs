using System;
using System.Collections.Generic;

namespace mtc_app.features.machine_history.data.dtos
{
    public class CreateTicketRequest
    {
        public string OperatorNik { get; set; }
        public string ShiftName { get; set; }
        public string ApplicatorCode { get; set; }
        public int MachineId { get; set; }
        public List<TicketProblemRequest> Problems { get; set; } = new List<TicketProblemRequest>();

        // Offline Lifecycle Fields
        public string TechnicianNik { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int StatusId { get; set; } = 1; // Default Open
    }

    public class TicketProblemRequest
    {
        public string ProblemTypeName { get; set; }
        public string FailureName { get; set; }
    }
}
