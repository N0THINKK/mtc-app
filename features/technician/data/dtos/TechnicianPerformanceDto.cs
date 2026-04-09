namespace mtc_app.features.technician.data.dtos
{
    /// <summary>
    /// DTO for Technician Leaderboard data.
    /// Groups performance by Technician (not by time period).
    /// </summary>
    public class TechnicianPerformanceDto
    {
        public long TechnicianId { get; set; }
        public string TechnicianName { get; set; }
        public int TotalRepairs { get; set; }
        public double AverageRating { get; set; }
        public int TotalStars { get; set; }
    }
}
