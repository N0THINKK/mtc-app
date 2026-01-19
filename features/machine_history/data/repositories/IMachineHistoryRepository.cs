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
    }
}
