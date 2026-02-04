using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtc_app.shared.data.dtos;

namespace mtc_app.shared.data.repositories
{
    /// <summary>
    /// Repository for fetching master data from remote database.
    /// </summary>
    public class MasterDataRepository : IMasterDataRepository
    {
        public async Task<List<CachedMachineDto>> GetMachinesAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var result = await conn.QueryAsync<CachedMachineDto>(@"
                    SELECT 
                        machine_id AS MachineId,
                        CONCAT(machine_type, '-', machine_area, '-', machine_number) AS Code,
                        machine_type AS MachineType,
                        machine_area AS MachineArea,
                        machine_number AS MachineNumber,
                        COALESCE(current_status_id, 1) AS StatusId
                    FROM machines
                    ORDER BY machine_type, machine_area, machine_number");
                return result.ToList();
            }
        }

        public async Task<List<CachedShiftDto>> GetShiftsAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var result = await conn.QueryAsync<CachedShiftDto>(@"
                    SELECT shift_id AS ShiftId, shift_name AS ShiftName
                    FROM shifts
                    ORDER BY shift_id");
                return result.ToList();
            }
        }

        public async Task<List<CachedProblemTypeDto>> GetProblemTypesAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var result = await conn.QueryAsync<CachedProblemTypeDto>(@"
                    SELECT type_id AS TypeId, type_name AS TypeName
                    FROM problem_types
                    ORDER BY type_id");
                return result.ToList();
            }
        }

        public async Task<List<string>> GetOperatorsAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var result = await conn.QueryAsync<string>(@"
                    SELECT nik FROM users WHERE role_id = 1 AND nik IS NOT NULL ORDER BY nik");
                return result.ToList();
            }
        }

        public async Task<List<CachedFailureDto>> GetFailuresAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var result = await conn.QueryAsync<CachedFailureDto>(@"
                    SELECT failure_id AS FailureId, failure_name AS FailureName 
                    FROM failures 
                    ORDER BY failure_name");
                return result.ToList();
            }
        }
    }
}
