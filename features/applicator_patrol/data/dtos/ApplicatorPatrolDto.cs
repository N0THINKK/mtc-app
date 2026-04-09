using System;
using System.Collections.Generic;

namespace mtc_app.features.applicator_patrol.data.dtos
{
    public class ApplicatorPatrolLogDto
    {
        public int LogId { get; set; }
        public DateTime PatrolDate { get; set; }
        public int ShiftId { get; set; }
        public int? UserId { get; set; }   // null untuk operator yang tidak ada di tabel users
        public string OperatorNik { get; set; } // NIK/username operator
        public int MachineId { get; set; }
        public string Side { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApplicatorPatrolDetailDto
    {
        public int DetailId { get; set; }
        public int LogId { get; set; }
        public string ApplicatorCode { get; set; }
        public string Judgment { get; set; }
        /// <summary>Nomor item yang NG, dipisah koma. Contoh: "1,3,5". Null/kosong jika OK atau NA.</summary>
        public string NgItems { get; set; }
    }

    // ── DTO untuk tampilan history (JOIN log + master) ──────────────────
    public class ApplicatorPatrolHistoryDto
    {
        public int LogId { get; set; }
        public DateTime PatrolDate { get; set; }
        public string ShiftName { get; set; }
        public string MachineCode { get; set; }
        public string Side { get; set; }
        public string OperatorNik { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalAplikator { get; set; }
        public int TotalNg { get; set; }
        public List<ApplicatorPatrolDetailDto> Details { get; set; } = new List<ApplicatorPatrolDetailDto>();
    }
}
