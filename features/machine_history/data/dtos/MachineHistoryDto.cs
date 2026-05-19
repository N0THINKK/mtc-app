using System;

namespace mtc_app.features.machine_history.data.dtos
{
    public class MachineHistoryDto
    {
        public long TicketId { get; set; }
        public Guid TicketUuid { get; set; } // Added for Offline Matching
        public string TicketCode { get; set; }
        public string MachineName { get; set; }
        public string TechnicianName { get; set; }
        public string OperatorName { get; set; }
        public string Issue { get; set; } // Combined failure details
        public string ActionDetails { get; set; } // Tindakan yang dilakukan
        public string SparepartUsed { get; set; } // Part yang diganti
        public string Resolution { get; set; } // Combined action details (legacy)
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; } // Added
        public DateTime? FinishedAt { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        
        // Added for GL Detail View
        public int? TechRatingScore { get; set; }
        public string TechRatingNote { get; set; }
        public int? GlRatingScore { get; set; }
        public string GlRatingNote { get; set; }
        public int CounterStroke { get; set; }
        public DateTime? ProductionResumedAt { get; set; }

        // Computed: formatted start time
        public string ReportedTime => CreatedAt.ToString("HH:mm");

        // Computed: formatted finish time
        public string FinishedTime => FinishedAt?.ToString("HH:mm") ?? "-";

        // Computed: duration from report to finish
        public string Duration
        {
            get
            {
                if (!FinishedAt.HasValue) return "-";
                var span = FinishedAt.Value - CreatedAt;
                if (span.TotalMinutes < 1) return "< 1m";
                if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m";
                return $"{(int)span.TotalHours}j {span.Minutes}m";
            }
        }
    }
}
