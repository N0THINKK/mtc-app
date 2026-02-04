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

        public async Task<List<CachedCauseDto>> GetCausesAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var result = await conn.QueryAsync<CachedCauseDto>(@"
                    SELECT cause_id AS CauseId, cause_name AS CauseName 
                    FROM failure_causes 
                    ORDER BY cause_name");
                var list = result.ToList();
                
                // Fallback if database is empty
                if (list.Count == 0)
                {
                    list = GetDefaultCauses();
                }
                return list;
            }
        }

        public async Task<List<CachedActionDto>> GetActionsAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var result = await conn.QueryAsync<CachedActionDto>(@"
                    SELECT action_id AS ActionId, action_name AS ActionName 
                    FROM actions 
                    ORDER BY action_name");
                var list = result.ToList();
                
                // Fallback if database is empty
                if (list.Count == 0)
                {
                    list = GetDefaultActions();
                }
                return list;
            }
        }

        public async Task<List<CachedPartDto>> GetPartsAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var result = await conn.QueryAsync<CachedPartDto>(@"
                    SELECT part_id AS PartId, part_code AS PartCode, part_name AS PartName
                    FROM parts
                    ORDER BY part_name");
                var list = result.ToList();
                
                // Fallback if database is empty
                if (list.Count == 0)
                {
                    list = GetDefaultParts();
                }
                return list;
            }
        }

        #region Default Fallback Data
        
        private List<CachedCauseDto> GetDefaultCauses()
        {
            return new List<CachedCauseDto>
            {
                new CachedCauseDto { CauseId = 1, CauseName = "Aus/Wear" },
                new CachedCauseDto { CauseId = 2, CauseName = "Kotor" },
                new CachedCauseDto { CauseId = 3, CauseName = "Longgar/Kendor" },
                new CachedCauseDto { CauseId = 4, CauseName = "Patah/Pecah" },
                new CachedCauseDto { CauseId = 5, CauseName = "Korosi/Karat" },
                new CachedCauseDto { CauseId = 6, CauseName = "Overheat" },
                new CachedCauseDto { CauseId = 7, CauseName = "Short Circuit" },
                new CachedCauseDto { CauseId = 8, CauseName = "Kesalahan Operator" },
                new CachedCauseDto { CauseId = 9, CauseName = "Lainnya" }
            };
        }

        private List<CachedActionDto> GetDefaultActions()
        {
            return new List<CachedActionDto>
            {
                new CachedActionDto { ActionId = 1, ActionName = "Ganti Komponen" },
                new CachedActionDto { ActionId = 2, ActionName = "Perbaiki/Repair" },
                new CachedActionDto { ActionId = 3, ActionName = "Bersihkan" },
                new CachedActionDto { ActionId = 4, ActionName = "Kencangkan" },
                new CachedActionDto { ActionId = 5, ActionName = "Lubrikasi" },
                new CachedActionDto { ActionId = 6, ActionName = "Kalibrasi" },
                new CachedActionDto { ActionId = 7, ActionName = "Reset/Restart" },
                new CachedActionDto { ActionId = 8, ActionName = "Setting Ulang" },
                new CachedActionDto { ActionId = 9, ActionName = "Lainnya" }
            };
        }

        private List<CachedPartDto> GetDefaultParts()
        {
            return new List<CachedPartDto>
            {
                new CachedPartDto { PartId = 1, PartCode = "SPR-001", PartName = "Bearing" },
                new CachedPartDto { PartId = 2, PartCode = "SPR-002", PartName = "Belt" },
                new CachedPartDto { PartId = 3, PartCode = "SPR-003", PartName = "Blade" },
                new CachedPartDto { PartId = 4, PartCode = "SPR-004", PartName = "Sensor" },
                new CachedPartDto { PartId = 5, PartCode = "SPR-005", PartName = "Motor" },
                new CachedPartDto { PartId = 6, PartCode = "SPR-006", PartName = "Relay" },
                new CachedPartDto { PartId = 7, PartCode = "SPR-007", PartName = "Fuse" },
                new CachedPartDto { PartId = 8, PartCode = "SPR-008", PartName = "Contactor" },
                new CachedPartDto { PartId = 9, PartCode = "LAINNYA", PartName = "Lainnya (Tulis Manual)" }
            };
        }
        
        #endregion
    }
}
