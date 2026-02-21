using System;
using mtc_app.features.stock.data.enums;

namespace mtc_app.features.stock.data.dtos
{
    public class PartRequestDto
    {
        public int RequestId { get; set; }
        public long TicketId { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public string PartName { get; set; }
        public string PartCode { get; set; } // NEW
        public string MachineName { get; set; }
        public string TechnicianName { get; set; }
        public int Qty { get; set; }
        public string StatusName { get; set; }
        public int StatusId { get; set; }

        public RequestStatus Status => (RequestStatus)StatusId;

        // Display Properties
        public string PartDisplayName => 
            !string.IsNullOrEmpty(PartCode) ? $"{PartCode} - {PartName}" : PartName;

        public string FormattedRequestTime => 
            RequestedAt.Date == DateTime.Today 
                ? RequestedAt.ToString("HH:mm") 
                : RequestedAt.ToString("dd/MM/yy HH:mm");

        public string FormattedReadyTime =>
            ReadyAt.HasValue 
                ? (ReadyAt.Value.Date == DateTime.Today 
                    ? ReadyAt.Value.ToString("HH:mm") 
                    : ReadyAt.Value.ToString("dd/MM/yy HH:mm"))
                : "-";
    }
}
