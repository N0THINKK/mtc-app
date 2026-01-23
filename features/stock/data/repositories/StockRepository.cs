using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.stock.data.dtos;
using mtc_app.features.stock.data.enums;

namespace mtc_app.features.stock.data.repositories
{
    public class StockRepository : IStockRepository
    {
        public async Task<IEnumerable<PartRequestDto>> GetRequestsAsync(RequestStatus? filter, SortOrder sort)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string sql = @"
                SELECT 
                    pr.request_id AS RequestId,
                    pr.requested_at AS RequestedAt,
                    COALESCE(p.part_name, pr.part_name_manual) AS PartName,
                    u.full_name AS TechnicianName,
                    pr.qty AS Qty,
                    rs.status_name AS StatusName,
                    pr.status_id AS StatusId,
                    CONCAT(m.machine_type, '-', m.machine_area, '.', m.machine_number) AS MachineName
                FROM part_requests pr
                LEFT JOIN parts p ON pr.part_id = p.part_id
                LEFT JOIN tickets t ON pr.ticket_id = t.ticket_id
                LEFT JOIN users u ON t.technician_id = u.user_id
                LEFT JOIN request_statuses rs ON pr.status_id = rs.status_id
                LEFT JOIN machines m ON t.machine_id = m.machine_id";

                var parameters = new DynamicParameters();

                if (filter.HasValue && filter.Value != RequestStatus.None)
                {
                    sql += " WHERE pr.status_id = @StatusId";
                    parameters.Add("StatusId", (int)filter.Value);
                }

                string sortDirection = sort == SortOrder.Ascending ? "ASC" : "DESC";
                sql += $" ORDER BY pr.requested_at {sortDirection}";

                return await connection.QueryAsync<PartRequestDto>(sql, parameters);
            }
        }

        public async Task<StockStatsDto> GetStatsAsync()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                // Optimized single query for all stats
                const string sql = @"
                    SELECT 
                        COUNT(*) AS TotalRequests,
                        COUNT(CASE WHEN status_id = 1 THEN 1 END) AS PendingCount,
                        COUNT(CASE WHEN status_id = 2 THEN 1 END) AS ReadyCount
                    FROM part_requests";

                return await connection.QuerySingleAsync<StockStatsDto>(sql);
            }
        }

        public async Task<bool> MarkAsReadyAsync(int requestId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    UPDATE part_requests 
                    SET status_id = 2, ready_at = NOW() 
                    WHERE request_id = @Id";

                var affectedRows = await connection.ExecuteAsync(sql, new { Id = requestId });
                return affectedRows > 0;
            }
        }
    }
}
