using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.data.repositories
{
    public class TicketStatusDto
    {
        public int StatusId { get; set; }
        public int ArrivalSeconds { get; set; }
        public int RepairSeconds { get; set; }
        public int InspectionSeconds { get; set; }
        public int IsMachineRunning { get; set; }
    }

    public class TechnicianSessionDto
    {
        public string TechName { get; set; }
        public int Elapsed { get; set; }
        public int IsCompleting { get; set; }
    }

    public class TicketProblemDto
    {
        public long ProblemId { get; set; }
        public string ProblemType { get; set; }
        public string ProblemDetail { get; set; }
    }

    public interface ITechnicianTicketRepository
    {
        Task<long?> ResolveSyncedTicketIdAsync();
        Task<TicketStatusDto> LoadTicketStatusAsync(long ticketId);
        Task UpdateMachineRunningStateAsync(long ticketId, int state);
        Task SaveTicketTimersAsync(long ticketId, int arrival, int repair, int inspect);
        Task<long> CreateTechnicianSessionAsync(long ticketId, int technicianId);
        Task SaveSessionElapsedAsync(long sessionId, int elapsedSeconds);
        Task CompleteSessionAsync(long sessionId, int elapsedSeconds);
        Task<List<TechnicianSessionDto>> LoadPreviousSessionsAsync(long ticketId);
        Task<List<TicketProblemDto>> LoadTicketProblemsAsync(long ticketId);
    }
}
