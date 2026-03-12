using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.micrometer_patrol.data.dtos;
using mtc_app.shared.infrastructure;

namespace mtc_app.features.micrometer_patrol.data.repositories
{
    public class MicrometerPatrolRepository : IMicrometerPatrolRepository
    {
        public async Task<bool> SavePatrolAsync(MicrometerPatrolDto data)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string query = @"
                    INSERT INTO micrometer_patrols 
                    (patrol_date, shift_id, user_id, machine_id, point_1, point_2, point_3, point_4, point_5, notes) 
                    VALUES 
                    (@PatrolDate, @ShiftId, @UserId, @MachineId, @Point1, @Point2, @Point3, @Point4, @Point5, @Notes)";

                var result = await connection.ExecuteAsync(query, data);
                return result > 0;
            }
        }

        public async Task<IEnumerable<MicrometerPatrolDto>> GetTodayPatrolsAsync(DateTime date)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string query = @"
                    SELECT 
                        mp.id as Id, 
                        mp.patrol_date as PatrolDate, 
                        mp.shift_id as ShiftId, 
                        s.shift_name as ShiftName,
                        mp.user_id as UserId, 
                        u.full_name as UserName, 
                        mp.machine_id as MachineId,
                        m.machine_number as MachineNumber,
                        mp.point_1 as Point1,
                        mp.point_2 as Point2,
                        mp.point_3 as Point3,
                        mp.point_4 as Point4,
                        mp.point_5 as Point5,
                        mp.notes as Notes
                    FROM micrometer_patrols mp
                    JOIN users u ON mp.user_id = u.user_id
                    JOIN shifts s ON mp.shift_id = s.shift_id
                    JOIN machines m ON mp.machine_id = m.machine_id
                    WHERE DATE(mp.patrol_date) = DATE(@Date)
                    ORDER BY mp.created_at DESC";

                return await connection.QueryAsync<MicrometerPatrolDto>(query, new { Date = date });
            }
        }
    }
}
