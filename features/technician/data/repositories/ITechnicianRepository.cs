using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.data.repositories
{
    public interface ITechnicianRepository
    {
        Task<IEnumerable<TicketDto>> GetActiveTicketsAsync(DateTime start, DateTime end);
        Task<TechnicianStatsDto> GetTechnicianStatisticsAsync(long technicianId);
        Task<TechnicianTicketDetailDto> GetTicketDetailAsync(long ticketId);
        Task<IEnumerable<TechnicianPerformanceDto>> GetLeaderboardAsync(DateTime start, DateTime end);
        Task<IEnumerable<MachinePerformanceDto>> GetMachinePerformanceAsync(DateTime start, DateTime end, string area = null);
        Task UpdateOperatorRatingAsync(long ticketId, int rating, string note);
        
        // [BARU] Metode untuk mengambil status Run mesin berdasarkan efisiensi
        Task<(int Running, int Total)> GetMachineRunStatsAsync();
        
        Task<IEnumerable<PatrolNgDto>> GetPatrolNgListAsync(string filterStatus, string sortOrder, DateTime start, DateTime end, string roleFilter = "Semua", string itemFilter = "Semua");
        Task<PatrolNgStatsDto> GetPatrolNgStatsAsync(DateTime start, DateTime end);
        Task<bool> MarkPatrolNgAsResolvedAsync(int detailId);
        Task<IEnumerable<string>> GetPatrolNgItemNamesAsync(DateTime start, DateTime end);
    }
}