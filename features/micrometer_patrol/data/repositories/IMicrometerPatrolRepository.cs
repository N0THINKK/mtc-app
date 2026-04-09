using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.micrometer_patrol.data.dtos;

namespace mtc_app.features.micrometer_patrol.data.repositories
{
    public interface IMicrometerPatrolRepository
    {
        Task<bool> SavePatrolAsync(MicrometerPatrolDto patrolData);
        Task<IEnumerable<MicrometerPatrolDto>> GetTodayPatrolsAsync(DateTime date);
    }
}