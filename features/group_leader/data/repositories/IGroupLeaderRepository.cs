using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.group_leader.data.dtos;

namespace mtc_app.features.group_leader.data.repositories
{
    public interface IGroupLeaderRepository
    {
        Task<IEnumerable<GroupLeaderTicketDto>> GetTicketsAsync(string statusFilter = null, string machineFilter = null);
        Task ValidateTicketAsync(long ticketId, int rating, string note);
    }
}
