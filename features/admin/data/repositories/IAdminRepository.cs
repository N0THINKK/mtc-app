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

        Task<IEnumerable<dynamic>> GetMasterUsersAsync();
        Task<IEnumerable<dynamic>> GetMasterMachinesAsync();
        Task<IEnumerable<dynamic>> GetMasterSparepartsAsync();

        Task<IEnumerable<dynamic>> GetMasterProblemTypesAsync();
        Task<IEnumerable<dynamic>> GetMasterFailuresAsync();
        Task<IEnumerable<dynamic>> GetMasterCausesAsync();
        Task<IEnumerable<dynamic>> GetMasterActionsAsync();
        Task<IEnumerable<dynamic>> GetMasterChecksheetsAsync(string roleTarget);
        Task<IEnumerable<string>> GetChecksheetTemplatesAsync();
        
        // Master UI Populate helpers
        Task<IEnumerable<string>> GetMachineTypesAsync();
        Task<IEnumerable<string>> GetMachineAreasAsync();

        Task<bool> SaveMasterDataAsync(string category, string subCategory, bool isEdit, IDictionary<string, object> data);
        Task<bool> DeleteMasterDataAsync(string category, string subCategory, int id);

        // Output Target CRUD
        Task<IEnumerable<dynamic>> GetOutputTargetsAsync();
        Task<bool> SaveOutputTargetAsync(int? targetId, int typeId, int areaId, string machineNumber, int targetPerHour);
        Task<bool> DeleteOutputTargetAsync(int targetId);

        // Shift Breaks
        Task<IEnumerable<dynamic>> GetShiftBreaksAsync(string shiftName);
        Task<bool> SaveShiftBreakAsync(int? breakId, string shiftName, int dayId, int nonOtMinutes, int otMinutes);
        Task<bool> DeleteShiftBreakAsync(int breakId);
    }
}