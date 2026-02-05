using System.Collections.Generic;
using System.Threading.Tasks;

namespace mtc_app.features.authentication.data.repositories
{
    public interface ISetupRepository
    {
        Task<IEnumerable<string>> GetMachineTypesAsync();
        Task<IEnumerable<string>> GetMachineAreasAsync();
        Task<int> RegisterMachineAsync(string type, string area, string number);
    }
}
