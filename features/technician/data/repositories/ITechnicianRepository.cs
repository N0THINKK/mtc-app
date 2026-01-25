using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.data.repositories
{
    public interface ITechnicianRepository
    {
        IEnumerable<TicketDto> GetActiveTickets();
        TechnicianStatsDto GetTechnicianStatistics(long technicianId);
        Task<TechnicianTicketDetailDto> GetTicketDetailAsync(long ticketId);
        Task<IEnumerable<TechnicianPerformanceDto>> GetLeaderboardAsync();
        Task<IEnumerable<MachinePerformanceDto>> GetMachinePerformanceAsync(); // NEW
    }
}
