using System;

namespace mtc_app.features.technician.data.dtos
{
    public class TechnicianTicketDetailDto
    {
        public long TicketId { get; set; }
        public string MachineName { get; set; }
        public string OperatorName { get; set; }
        public string FailureDetails { get; set; }
        public string ActionDetails { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        
        // Rating given by Technician to Operator
        public int? TechRatingScore { get; set; }
        public string TechRatingNote { get; set; }
    }
}
