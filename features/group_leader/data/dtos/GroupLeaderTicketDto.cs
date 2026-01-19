using System;

namespace mtc_app.features.group_leader.data.dtos
{
    public class GroupLeaderTicketDto
    {
        public Guid TicketUuid { get; set; } // Added for Rating Form compatibility
        public long TicketId { get; set; } // Changed from Guid to long to match DB Schema
        public string MachineName { get; set; }
        public string TechnicianName { get; set; }
        public int? GlRatingScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? GlValidatedAt { get; set; }
    }
}
