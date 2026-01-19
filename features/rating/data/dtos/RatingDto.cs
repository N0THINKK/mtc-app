using System;

namespace mtc_app.features.rating.data.dtos
{
    public class RatingDto
    {
        public long TicketId { get; set; }
        public int Score { get; set; } // 1-5
        public string Comment { get; set; }
        public int RaterId { get; set; } // User ID of who is rating
        public DateTime CreatedAt { get; set; }
    }
}
