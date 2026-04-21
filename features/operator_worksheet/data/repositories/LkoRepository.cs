using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.operator_worksheet.data.dtos;
using mtc_app.shared.infrastructure;

namespace mtc_app.features.operator_worksheet.data.repositories
{
    public class LkoRepository : ILkoRepository
    {
        public async Task<int> SaveLkoRecordAsync(LkoRecordDto record)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Check if record already exists for this sequen+urutan+mesin today
                string checkSql = @"
                    SELECT id FROM lko_records 
                    WHERE sequen = @Sequen 
                      AND urutan_kanban = @UrutanKanban
                      AND no_mesin = @NoMesin 
                      AND DATE(waktu_simpan) = CURDATE()
                    LIMIT 1";

                var existingId = await connection.QueryFirstOrDefaultAsync<int?>(checkSql, new
                {
                    record.Sequen,
                    record.UrutanKanban,
                    record.NoMesin
                });

                if (existingId.HasValue)
                {
                    // Update existing record
                    string updateSql = @"
                        UPDATE lko_records SET
                            waktu_simpan = @WaktuSimpan,
                            shift_name = @ShiftName,
                            nik = @Nik,
                            qty_defect_operator = @QtyDefectOperator,
                            kode_defect = @KodeDefect,
                            qty_product = @QtyProduct
                        WHERE id = @Id";

                    await connection.ExecuteAsync(updateSql, new
                    {
                        record.WaktuSimpan,
                        record.ShiftName,
                        record.Nik,
                        record.QtyDefectOperator,
                        record.KodeDefect,
                        record.QtyProduct,
                        Id = existingId.Value
                    });
                    return existingId.Value;
                }
                else
                {
                    // Insert new record
                    string insertSql = @"
                        INSERT INTO lko_records 
                            (waktu_simpan, no_mesin, shift_name, nik, sequen, urutan_kanban, qty_defect_operator, kode_defect, qty_product)
                        VALUES 
                            (@WaktuSimpan, @NoMesin, @ShiftName, @Nik, @Sequen, @UrutanKanban, @QtyDefectOperator, @KodeDefect, @QtyProduct);
                        SELECT LAST_INSERT_ID();";

                    return await connection.ExecuteScalarAsync<int>(insertSql, new
                    {
                        record.WaktuSimpan,
                        record.NoMesin,
                        record.ShiftName,
                        record.Nik,
                        record.Sequen,
                        record.UrutanKanban,
                        record.QtyDefectOperator,
                        record.KodeDefect,
                        record.QtyProduct
                    });
                }
            }
        }

        public async Task<List<LkoRecordDto>> GetTodayRecordsAsync(string noMesin)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string sql = @"
                    SELECT 
                        id AS Id,
                        waktu_simpan AS WaktuSimpan,
                        no_mesin AS NoMesin,
                        shift_name AS ShiftName,
                        nik AS Nik,
                        sequen AS Sequen,
                        urutan_kanban AS UrutanKanban,
                        qty_defect_operator AS QtyDefectOperator,
                        kode_defect AS KodeDefect,
                        qty_product AS QtyProduct
                    FROM lko_records
                    WHERE no_mesin = @NoMesin
                      AND DATE(waktu_simpan) = CURDATE()
                    ORDER BY waktu_simpan DESC";

                var results = await connection.QueryAsync<LkoRecordDto>(sql, new { NoMesin = noMesin });
                return results.ToList();
            }
        }
    }
}
