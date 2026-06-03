using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.data.repositories
{
    public class MachineMonitorRepository : IMachineMonitorRepository
    {
        public async Task<ShiftBreakDto> GetShiftBreaksAsync(string shiftName, DateTime date, int dayId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                var ovr = await conn.QueryFirstOrDefaultAsync(
                    "SELECT non_ot_minutes as NonOtMinutes, ot_minutes as OtMinutes FROM shift_break_overrides WHERE shift_name = @Shift AND override_date = @Date",
                    new { Shift = shiftName, Date = date.Date });

                if (ovr != null) return new ShiftBreakDto { NonOtMinutes = (int)ovr.NonOtMinutes, OtMinutes = (int)ovr.OtMinutes };

                var b = await conn.QueryFirstOrDefaultAsync(
                    "SELECT non_ot_minutes as NonOtMinutes, ot_minutes as OtMinutes FROM shift_breaks WHERE shift_name = @Shift AND day_id = @Day",
                    new { Shift = shiftName, Day = dayId });

                if (b != null) return new ShiftBreakDto { NonOtMinutes = (int)b.NonOtMinutes, OtMinutes = (int)b.OtMinutes };
                
                return new ShiftBreakDto { NonOtMinutes = 0, OtMinutes = 0 };
            }
        }

        public async Task<IEnumerable<MachineMonitorDto>> GetMachineListAsync(string area)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT m.machine_id as MachineId, m.type_id as TypeId, m.area_id as AreaId,
                           COALESCE(t.type_name, 'UNK') AS type_name,
                           COALESCE(a.area_name, 'UNK') AS area_name,
                           m.machine_number as MachineNum
                    FROM machines m
                    LEFT JOIN machine_types t ON m.type_id = t.type_id
                    LEFT JOIN machine_areas a ON m.area_id = a.area_id
                    WHERE (@Area = 'Semua Area' OR a.area_name = @Area)
                      AND COALESCE(t.type_name, '') != 'Layar'
                    ORDER BY m.machine_id";

                var rows = await conn.QueryAsync(sql, new { Area = area });
                
                return rows.Select(r => new MachineMonitorDto
                {
                    MachineId = (int)r.MachineId,
                    MachineName = $"{r.type_name}.{r.area_name}-{r.MachineNum}",
                    MachineNum = r.MachineNum,
                    TypeId = (int)(r.TypeId ?? 0),
                    AreaId = (int)(r.AreaId ?? 0)
                }).ToList();
            }
        }

        public async Task<IEnumerable<string>> GetAreasAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT DISTINCT a.area_name 
                    FROM machine_areas a 
                    JOIN machines m ON a.area_id = m.area_id 
                    ORDER BY a.area_name";
                return await conn.QueryAsync<string>(sql);
            }
        }

        public async Task<IEnumerable<MachineProcessLogAggregateDto>> GetProcessLogsAsync(DateTime shiftStart, DateTime shiftEnd, List<int> machineIds)
        {
            if (machineIds == null || !machineIds.Any()) return new List<MachineProcessLogAggregateDto>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                string sqlLogs = @"
                    SELECT machine_id as MachineId,
                           TIMESTAMPDIFF(HOUR, @ShiftStart, created_at) AS HourIndex,
                           CAST(SUBSTRING_INDEX(GROUP_CONCAT(produced_pieces ORDER BY created_at ASC), ',', 1) AS SIGNED) AS FirstPieces,
                           CAST(SUBSTRING_INDEX(GROUP_CONCAT(produced_pieces ORDER BY created_at DESC), ',', 1) AS SIGNED) AS LastPieces,
                           MAX(produced_pieces) AS MaxPieces,
                           MAX(auto_time) AS MaxAuto,
                           MAX(monitor_time) AS MaxMonitor
                    FROM machine_process_logs
                    WHERE created_at >= @ShiftStart AND created_at < @ShiftEnd
                      AND produced_pieces > 0
                      AND machine_id IN @MachineIds
                    GROUP BY machine_id, TIMESTAMPDIFF(HOUR, @ShiftStart, created_at)
                    ORDER BY machine_id, HourIndex";

                return await conn.QueryAsync<MachineProcessLogAggregateDto>(sqlLogs, 
                    new { ShiftStart = shiftStart, ShiftEnd = shiftEnd, MachineIds = machineIds }, 
                    commandTimeout: 120);
            }
        }

        public async Task<Dictionary<int, int>> GetMachineTargetsAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                var targets = await conn.QueryAsync("SELECT machine_id, target_per_hour FROM machine_output_targets");
                var dict = new Dictionary<int, int>();
                foreach (var row in targets)
                {
                    dict[Convert.ToInt32(row.machine_id)] = Convert.ToInt32(row.target_per_hour);
                }
                return dict;
            }
        }

        public async Task<IEnumerable<MachineDowntimeDto>> GetMachineDowntimeAsync(DateTime shiftStart, DateTime shiftEnd, List<int> machineIds)
        {
            if (machineIds == null || !machineIds.Any()) return new List<MachineDowntimeDto>();
            
            using (var conn = DatabaseHelper.GetConnection())
            {
                return await conn.QueryAsync<MachineDowntimeDto>(@"
                    SELECT moa.machine_id as MachineId, 
                           SUM(CASE WHEN it.category IN ('Planned Stop', 'Berhenti Terencana') THEN TIMESTAMPDIFF(MINUTE, GREATEST(moa.start_time, @ShiftStart), LEAST(IFNULL(moa.end_time, NOW()), @ShiftEnd)) ELSE 0 END) AS PlannedMin,
                           SUM(CASE WHEN it.category IN ('Sudden Stop', 'Berhenti Tiba Tiba') THEN TIMESTAMPDIFF(MINUTE, GREATEST(moa.start_time, @ShiftStart), LEAST(IFNULL(moa.end_time, NOW()), @ShiftEnd)) ELSE 0 END) AS SuddenMin
                    FROM machine_operator_activities moa
                    LEFT JOIN activity_types it ON moa.activity_id = it.id
                    WHERE moa.start_time < @ShiftEnd AND (moa.end_time IS NULL OR moa.end_time > @ShiftStart)
                      AND moa.machine_id IN @MachineIds
                    GROUP BY moa.machine_id", 
                    new { ShiftStart = shiftStart, ShiftEnd = shiftEnd, MachineIds = machineIds });
            }
        }
    }
}
