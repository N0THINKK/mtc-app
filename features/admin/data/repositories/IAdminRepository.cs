using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.admin.data.dtos;

namespace mtc_app.features.admin.data.repositories
{
    public interface IAdminRepository
    {
        Task<AdminStatsDto> GetSummaryStatsAsync();
        Task<IEnumerable<dynamic>> GetMonitoringDataAsync(); 
        Task<IEnumerable<dynamic>> GetReportDataAsync(DateTime start, DateTime end);
        IEnumerable<dynamic> GetMonthlyLogsForExport(int month, int year, string areaName);

        // ==== TAMBAHAN UNTUK CRUD MASTER DATA ====
        Task<IEnumerable<dynamic>> GetMasterUsersAsync();
        Task<IEnumerable<dynamic>> GetMasterMachinesAsync();
        Task<IEnumerable<dynamic>> GetMasterSparepartsAsync();
        Task<IEnumerable<dynamic>> GetMasterProblemsAsync();
    }
}