using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.shared.data.dtos;

namespace mtc_app.shared.data.repositories
{
    /// <summary>
    /// Repository interface for master data (machines, shifts, problem types).
    /// </summary>
    public interface IMasterDataRepository
    {
        Task<List<CachedMachineDto>> GetMachinesAsync();
        Task<List<CachedShiftDto>> GetShiftsAsync();
        Task<List<CachedProblemTypeDto>> GetProblemTypesAsync();
        Task<List<string>> GetOperatorsAsync();
        Task<List<CachedFailureDto>> GetFailuresAsync();
    }
}
