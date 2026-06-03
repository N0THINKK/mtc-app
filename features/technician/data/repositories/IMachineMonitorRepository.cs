using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.data.repositories
{
    public interface IMachineMonitorRepository
    {
        Task<ShiftBreakDto> GetShiftBreaksAsync(string shiftName, DateTime date, int dayId);
        Task<IEnumerable<MachineMonitorDto>> GetMachineListAsync(string area);
        Task<IEnumerable<string>> GetAreasAsync();
        Task<IEnumerable<MachineProcessLogAggregateDto>> GetProcessLogsAsync(DateTime shiftStart, DateTime shiftEnd, List<int> machineIds);
        Task<Dictionary<int, int>> GetMachineTargetsAsync();
        Task<IEnumerable<MachineDowntimeDto>> GetMachineDowntimeAsync(DateTime shiftStart, DateTime shiftEnd, List<int> machineIds);
    }
}
