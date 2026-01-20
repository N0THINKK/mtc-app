using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.stock.data.dtos;
using mtc_app.features.stock.data.enums;

namespace mtc_app.features.stock.data.repositories
{
    public interface IStockRepository
    {
        Task<IEnumerable<PartRequestDto>> GetRequestsAsync(RequestStatus? filter, SortOrder sort);
        Task<StockStatsDto> GetStatsAsync();
        Task<bool> MarkAsReadyAsync(int requestId);
    }
}
