using System;

namespace mtc_app.features.group_leader.data.dtos
{
    public class GroupLeaderTicketDetailDto
    {
        public Guid TicketId { get; set; } // Using Guid as requested (likely maps to ticket_uuid)
        public string TicketCode { get; set; }
        public string MachineName { get; set; }
        public string TechnicianName { get; set; }
        public string OperatorName { get; set; }
        public string FailureDetails { get; set; } // Combined failure info
        public string ActionDetails { get; set; } // Combined action info
        public int CounterStroke { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int? GlRatingScore { get; set; }
        public string GlRatingNote { get; set; }
    }
}
