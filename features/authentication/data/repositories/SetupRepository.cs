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
                // New Schema: Fetch from lookup table
                return await conn.QueryAsync<string>("SELECT type_name FROM machine_types ORDER BY type_name");
            }
        }

        public async Task<IEnumerable<string>> GetMachineAreasAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                // New Schema: Fetch from lookup table
                return await conn.QueryAsync<string>("SELECT area_name FROM machine_areas ORDER BY area_name");
            }
        }

        public async Task<int> RegisterMachineAsync(string type, string area, string number)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                // 1. Get or Create Type ID
                int typeId = await GetOrCreateLookupId(conn, "machine_types", "type_id", "type_name", type);

                // 2. Get or Create Area ID
                int areaId = await GetOrCreateLookupId(conn, "machine_areas", "area_id", "area_name", area);

                // 3. Check existing Machine
                string sqlCheck = @"SELECT machine_id FROM machines 
                                  WHERE type_id = @TypeId 
                                  AND area_id = @AreaId 
                                  AND machine_number = @Number";
                
                var machineId = await conn.QueryFirstOrDefaultAsync<int?>(sqlCheck, new { TypeId = typeId, AreaId = areaId, Number = number });

                if (machineId.HasValue && machineId.Value > 0)
                {
                    return machineId.Value;
                }

                // 4. Insert new Machine with Foreign Keys
                string sqlInsert = @"INSERT INTO machines (type_id, area_id, machine_number, current_status_id) 
                                   VALUES (@TypeId, @AreaId, @Number, 1);
                                   SELECT LAST_INSERT_ID();";
                
                return await conn.QuerySingleAsync<int>(sqlInsert, new { TypeId = typeId, AreaId = areaId, Number = number });
            }
        }

        private async Task<int> GetOrCreateLookupId(System.Data.IDbConnection conn, string tableName, string idCol, string nameCol, string value)
        {
            string sqlFind = $"SELECT {idCol} FROM {tableName} WHERE {nameCol} = @Value";
            var id = await conn.QueryFirstOrDefaultAsync<int?>(sqlFind, new { Value = value });

            if (id.HasValue) return id.Value;

            string sqlInsert = $"INSERT INTO {tableName} ({nameCol}) VALUES (@Value); SELECT LAST_INSERT_ID();";
            return await conn.QuerySingleAsync<int>(sqlInsert, new { Value = value });
        }
    }
}
