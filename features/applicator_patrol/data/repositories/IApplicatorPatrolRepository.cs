using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.applicator_patrol.data.dtos;

namespace mtc_app.features.applicator_patrol.data.repositories
{
    public interface IApplicatorPatrolRepository
    {
        Task<int> SavePatrolAsync(ApplicatorPatrolLogDto log, List<ApplicatorPatrolDetailDto> details);
        Task<List<ApplicatorPatrolHistoryDto>> GetHistoryAsync(int machineId, DateTime? date = null);
        Task<List<ApplicatorPatrolDetailDto>> GetDetailsAsync(int logId);
        
        // [BARU] Untuk Technician Dashboard (NG Aplikator)
        Task<IEnumerable<ApplicatorNgDto>> GetApplicatorNgListAsync(DateTime start, DateTime end, string sortOrder = "DESC");
        Task<ApplicatorNgStatsDto> GetApplicatorNgStatsAsync(DateTime start, DateTime end);
    }
}
