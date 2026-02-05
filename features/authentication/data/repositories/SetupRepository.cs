using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

namespace mtc_app.features.authentication.data.repositories
{
    public class SetupRepository : ISetupRepository
    {
        public async Task<IEnumerable<string>> GetMachineTypesAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                return await conn.QueryAsync<string>("SELECT DISTINCT machine_type FROM machines ORDER BY machine_type");
            }
        }

        public async Task<IEnumerable<string>> GetMachineAreasAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                return await conn.QueryAsync<string>("SELECT DISTINCT machine_area FROM machines ORDER BY machine_area");
            }
        }

        public async Task<int> RegisterMachineAsync(string type, string area, string number)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                // 1. Check existing
                string sqlCheck = @"SELECT machine_id FROM machines 
                                  WHERE machine_type = @Type 
                                  AND machine_area = @Area 
                                  AND machine_number = @Number";
                
                var machineId = await conn.QueryFirstOrDefaultAsync<int?>(sqlCheck, new { Type = type, Area = area, Number = number });

                if (machineId.HasValue && machineId.Value > 0)
                {
                    return machineId.Value;
                }

                // 2. Insert new
                string sqlInsert = @"INSERT INTO machines (machine_type, machine_area, machine_number, current_status_id) 
                                   VALUES (@Type, @Area, @Number, 1);
                                   SELECT LAST_INSERT_ID();";
                
                return await conn.QuerySingleAsync<int>(sqlInsert, new { Type = type, Area = area, Number = number });
            }
        }
    }
}
