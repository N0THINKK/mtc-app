using System;

namespace mtc_app.features.technician.data.dtos
{
    public class TicketDto
    {
        public long TicketId { get; set; }
        public string MachineName { get; set; }
        public string FailureDetails { get; set; }
        public DateTime CreatedAt { get; set; }
        public int StatusId { get; set; }
        public int? GlRatingScore { get; set; }
        public DateTime? GlValidatedAt { get; set; }
    }

    public class TechnicianStatsDto
    {
        public int CompletedRepairs { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalStars { get; set; }
    }
}
