using System;

namespace mtc_app.features.micrometer_patrol.data.dtos
{
    public class MicrometerPatrolDto
    {
        public int Id { get; set; }
        public DateTime PatrolDate { get; set; }
        public string FormattedPatrolDate => PatrolDate.ToString("dd/MM/yyyy");
        public int ShiftId { get; set; }
        public string ShiftName { get; set; } 
        public int UserId { get; set; }
        
        // Untuk keperluan tampilan di Grid/Tabel
        public string UserName { get; set; } 
        
        public int MachineId { get; set; }
        public string MachineNumber { get; set; }
        
        public string Point1 { get; set; }
        public string Point2 { get; set; }
        public string Point3 { get; set; }
        public string Point4 { get; set; }
        public string Point5 { get; set; }
        public string Notes { get; set; }
    }
}