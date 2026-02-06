using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.data.repositories
{
    public interface IMachineHistoryRepository
    {
        /// <summary>
        /// Retrieves machine history within a specified date range.
        /// Defaults to the last 30 days if dates are not provided.
        /// </summary>
        /// <param name="startDate">Start date filter (inclusive).</param>
        /// <param name="endDate">End date filter (inclusive).</param>
        /// <param name="machineFilter">Optional search by machine name.</param>
        /// <returns>List of history records.</returns>
        Task<IEnumerable<MachineHistoryDto>> GetHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, string machineFilter = null);

        /// <summary>
        /// Creates a new ticket.
        /// </summary>
        /// <param name="request">Ticket creation details.</param>
        /// <returns>Created ticket details (ID and Display Code).</returns>
        Task<(long TicketId, string TicketCode)> CreateTicketAsync(CreateTicketRequest request);

        /// <summary>
        /// Gets the active (pending) ticket for a specific machine.
        /// Returns null if no active ticket exists.
        /// </summary>
        /// <param name="machineId">The machine ID to check.</param>
        /// <returns>The active ticket or null.</returns>
        Task<MachineHistoryDto> GetActiveTicketForMachineAsync(int machineId);
    }
}
