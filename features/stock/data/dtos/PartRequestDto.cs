using System;
using mtc_app.features.stock.data.enums;

namespace mtc_app.features.stock.data.dtos
{
    public class PartRequestDto
    {
        public int RequestId { get; set; }
        public DateTime RequestedAt { get; set; }
        public string PartName { get; set; }
        public string MachineName { get; set; } // NEW
        public string TechnicianName { get; set; }
        public int Qty { get; set; }
        public string StatusName { get; set; }
        public int StatusId { get; set; }

        public RequestStatus Status => (RequestStatus)StatusId;
    }
}
