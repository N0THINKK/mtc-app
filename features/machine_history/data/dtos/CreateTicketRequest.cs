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
        public DateTime? ProductionResumedAt { get; set; }
        public int StatusId { get; set; } = 1; // Default Open

        // Technician Completion Fields
        public int CounterStroke { get; set; }
        public bool Is4M { get; set; }
        public int TechRatingScore { get; set; }
        public string TechRatingNote { get; set; }
        
        // Sparepart Requests (for offline sync)
        public List<string> SparepartRequests { get; set; } = new List<string>();
    }

    public class TicketProblemRequest
    {
        public string ProblemTypeName { get; set; }
        public string FailureName { get; set; }
        
        // Technician Analysis Fields
        public string CauseName { get; set; }
        public string ActionName { get; set; }
    }
}
