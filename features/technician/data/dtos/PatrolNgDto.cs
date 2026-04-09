using System;

namespace mtc_app.features.technician.data.dtos
{
    public class PatrolNgDto
    {
        public int DetailId { get; set; }
        public int LogId { get; set; }
        public int MachineId { get; set; }
        public string MachineName { get; set; }
        public DateTime PatrolDate { get; set; }
        public string FormattedPatrolDate => PatrolDate.ToString("dd/MM/yyyy HH:mm");
        public string RoleTarget { get; set; }
        public string ItemName { get; set; }
        public string ActionNote { get; set; }
        public string Status { get; set; }
        public bool IsTicketCreated { get; set; }
        
        public long? TicketId { get; set; } 
    }
}