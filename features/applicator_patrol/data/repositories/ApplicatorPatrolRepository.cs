using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.applicator_patrol.data.dtos;
using mtc_app.shared.infrastructure;

namespace mtc_app.features.applicator_patrol.data.repositories
{
    public class ApplicatorPatrolRepository : IApplicatorPatrolRepository
    {
        public async Task<int> SavePatrolAsync(ApplicatorPatrolLogDto log, List<ApplicatorPatrolDetailDto> details)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert header log
                        string insertLog = @"
                            INSERT INTO applicator_patrol_logs 
                                (patrol_date, shift_id, user_id, operator_nik, machine_id, side, notes)
                            VALUES 
                                (@PatrolDate, @ShiftId, @UserId, @OperatorNik, @MachineId, @Side, @Notes);
                            SELECT LAST_INSERT_ID();";

                        int logId = await connection.ExecuteScalarAsync<int>(insertLog, new
                        {
                            log.PatrolDate,
                            log.ShiftId,
                            UserId = log.UserId > 0 ? (object)log.UserId : null,
                            log.OperatorNik,
                            log.MachineId,
                            log.Side,
                            log.Notes
                        }, transaction);

                        if (details != null && details.Count > 0)
                        {
                            string insertDetail = @"
                                INSERT INTO applicator_patrol_details (log_id, applicator_code, judgment, ng_items)
                                VALUES (@LogId, @ApplicatorCode, @Judgment, @NgItems)";

                            foreach (var detail in details)
                            {
                                detail.LogId = logId;
                                await connection.ExecuteAsync(insertDetail, detail, transaction);
                            }
                        }

                        transaction.Commit();
                        return logId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<List<ApplicatorPatrolHistoryDto>> GetHistoryAsync(int machineId, DateTime? date = null)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT 
                        l.log_id          AS LogId,
                        l.patrol_date     AS PatrolDate,
                        s.shift_name      AS ShiftName,
                        m.machine_number  AS MachineCode,
                        l.side            AS Side,
                        l.notes           AS Notes,
                        l.created_at      AS CreatedAt,
                        COALESCE(u.nik, u.username) AS OperatorNik,
                        COUNT(d.detail_id)          AS TotalAplikator,
                        SUM(CASE WHEN d.judgment = 'NG' THEN 1 ELSE 0 END) AS TotalNg
                    FROM applicator_patrol_logs l
                    LEFT JOIN shifts          s ON s.shift_id   = l.shift_id
                    LEFT JOIN machines        m ON m.machine_id = l.machine_id
                    LEFT JOIN users           u ON u.user_id    = l.user_id
                    LEFT JOIN applicator_patrol_details d ON d.log_id = l.log_id
                    WHERE (@MachineId = 0 OR l.machine_id = @MachineId)
                      AND (@Date IS NULL OR l.patrol_date = @Date)
                    GROUP BY l.log_id
                    ORDER BY l.created_at DESC
                    LIMIT 100";

                var rows = await connection.QueryAsync<ApplicatorPatrolHistoryDto>(sql, new
                {
                    MachineId = machineId,
                    Date = date.HasValue ? (object)date.Value.Date : null
                });

                return rows.ToList();
            }
        }

        public async Task<List<ApplicatorPatrolDetailDto>> GetDetailsAsync(int logId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                string sql = @"
                    SELECT detail_id AS DetailId, log_id AS LogId,
                           applicator_code AS ApplicatorCode, judgment AS Judgment,
                           ng_items AS NgItems
                    FROM applicator_patrol_details
                    WHERE log_id = @LogId
                    ORDER BY detail_id";

                var rows = await connection.QueryAsync<ApplicatorPatrolDetailDto>(sql, new { LogId = logId });
                return rows.ToList();
            }
        }
    }
}
