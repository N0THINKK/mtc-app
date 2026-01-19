using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.admin.data.dtos;

// Ensure other DTOs (MonitoringDto, ReportDto) are available or reuse existing/anonymous types if simpler for now.
// MonitoringView uses a dynamic list currently. Let's create a DTO for it too or use dynamic.
// Ideally usage of DTO is better.
// Let's assume we will use dynamic or create a DTO. I'll stick to dynamic for the Interface if I haven't created MonitoringDto yet.
// Wait, Implementation Plan mentioned GetMonitoringDataAsync.
// Let's create a class for it inside IAdminRepository.cs or separate file? Separate is better.
// But for speed, I'll use `dynamic` or `object` in interface? No, Clean Code.
// I will create `MonitoringDto` implicitly in the same step/file if small, or just one file.
// I'll create `MonitoringDto.cs` as well.

namespace mtc_app.features.admin.data.repositories
{
    public interface IAdminRepository
    {
        Task<AdminStatsDto> GetSummaryStatsAsync();
        Task<IEnumerable<dynamic>> GetMonitoringDataAsync(); // Keeping dynamic for now to match current grid, can refactor to strict DTO later.
        Task<IEnumerable<dynamic>> GetReportDataAsync(DateTime start, DateTime end);
    }
}
