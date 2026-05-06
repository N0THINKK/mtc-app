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

                // Jika sudah punya Id (edit dari Tersimpan), langsung update
                int? existingId = null;
                if (record.Id > 0)
                {
                    existingId = record.Id;
                }
                else
                {
                    // Check if record already exists for this sequen+urutan+mesin today
                    string checkSql = @"
                        SELECT id FROM lko_records 
                        WHERE sequen = @Sequen 
                          AND urutan_kanban = @UrutanKanban
                          AND no_mesin = @NoMesin 
                          AND DATE(waktu_simpan) = CURDATE()
                        LIMIT 1";

                    existingId = await connection.QueryFirstOrDefaultAsync<int?>(checkSql, new
                    {
                        record.Sequen,
                        record.UrutanKanban,
                        record.NoMesin
                    });
                }

                if (existingId.HasValue)
                {
                    // Update existing record
                    string updateSql = @"
                        UPDATE lko_records SET
                            waktu_simpan = @WaktuSimpan,
                            id_mesin = @IdMesin,
                            shift_name = @ShiftName,
                            nik = @Nik,
                            qty_product = @QtyProduct,
                            no_4m = @No4m,
                            qty_defect_mesin = @QtyDefectMesin,
                            qty_defect_operator = @QtyDefectOperator,
                            kode_defect = @KodeDefect,
                            lot_id_wire = @LotIdWire,
                            lot_id_terminal_a = @LotIdTerminalA,
                            lot_id_terminal_b = @LotIdTerminalB,
                            issue_kanban = @IssueKanban,
                            cut_length = @CutLength,
                            kombinasi_wire = @KombinasiWire,
                            terminal_a = @TerminalA,
                            terminal_b = @TerminalB,
                            seal_a = @SealA,
                            seal_b = @SealB,
                            qty_master = @QtyMaster,
                            front_ch_a = @FrontChA,
                            front_cw_a = @FrontCwA,
                            rear_ch_a = @RearChA,
                            rear_cw_a = @RearCwA,
                            front_ch_b = @FrontChB,
                            front_cw_b = @FrontCwB,
                            rear_ch_b = @RearChB,
                            rear_cw_b = @RearCwB,
                            waktu_mulai = @WaktuMulai,
                            waktu_selesai = @WaktuSelesai
                        WHERE id = @Id";

                    await connection.ExecuteAsync(updateSql, new
                    {
                        record.WaktuSimpan,
                        record.IdMesin,
                        record.ShiftName,
                        record.Nik,
                        record.QtyProduct,
                        record.No4m,
                        record.QtyDefectMesin,
                        record.QtyDefectOperator,
                        record.KodeDefect,
                        record.LotIdWire,
                        record.LotIdTerminalA,
                        record.LotIdTerminalB,
                        record.IssueKanban,
                        record.CutLength,
                        record.KombinasiWire,
                        record.TerminalA,
                        record.TerminalB,
                        record.SealA,
                        record.SealB,
                        record.QtyMaster,
                        record.FrontChA,
                        record.FrontCwA,
                        record.RearChA,
                        record.RearCwA,
                        record.FrontChB,
                        record.FrontCwB,
                        record.RearChB,
                        record.RearCwB,
                        record.WaktuMulai,
                        record.WaktuSelesai,
                        Id = existingId.Value
                    });
                    return existingId.Value;
                }
                else
                {
                    // Insert new record
                    string insertSql = @"
                        INSERT INTO lko_records 
                            (waktu_simpan, no_mesin, id_mesin, shift_name, nik, sequen, urutan_kanban,
                             qty_product, no_4m, qty_defect_mesin, qty_defect_operator, kode_defect,
                             lot_id_wire, lot_id_terminal_a, lot_id_terminal_b, issue_kanban, cut_length,
                             kombinasi_wire, terminal_a, terminal_b, seal_a, seal_b, qty_master,
                             front_ch_a, front_cw_a, rear_ch_a, rear_cw_a,
                             front_ch_b, front_cw_b, rear_ch_b, rear_cw_b,
                             waktu_mulai, waktu_selesai)
                        VALUES 
                            (@WaktuSimpan, @NoMesin, @IdMesin, @ShiftName, @Nik, @Sequen, @UrutanKanban,
                             @QtyProduct, @No4m, @QtyDefectMesin, @QtyDefectOperator, @KodeDefect,
                             @LotIdWire, @LotIdTerminalA, @LotIdTerminalB, @IssueKanban, @CutLength,
                             @KombinasiWire, @TerminalA, @TerminalB, @SealA, @SealB, @QtyMaster,
                             @FrontChA, @FrontCwA, @RearChA, @RearCwA,
                             @FrontChB, @FrontCwB, @RearChB, @RearCwB,
                             @WaktuMulai, @WaktuSelesai);
                        SELECT LAST_INSERT_ID();";

                    return await connection.ExecuteScalarAsync<int>(insertSql, new
                    {
                        record.WaktuSimpan,
                        record.NoMesin,
                        record.IdMesin,
                        record.ShiftName,
                        record.Nik,
                        record.Sequen,
                        record.UrutanKanban,
                        record.QtyProduct,
                        record.No4m,
                        record.QtyDefectMesin,
                        record.QtyDefectOperator,
                        record.KodeDefect,
                        record.LotIdWire,
                        record.LotIdTerminalA,
                        record.LotIdTerminalB,
                        record.IssueKanban,
                        record.CutLength,
                        record.KombinasiWire,
                        record.TerminalA,
                        record.TerminalB,
                        record.SealA,
                        record.SealB,
                        record.QtyMaster,
                        record.FrontChA,
                        record.FrontCwA,
                        record.RearChA,
                        record.RearCwA,
                        record.FrontChB,
                        record.FrontCwB,
                        record.RearChB,
                        record.RearCwB,
                        record.WaktuMulai,
                        record.WaktuSelesai
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
                        id_mesin AS IdMesin,
                        shift_name AS ShiftName,
                        nik AS Nik,
                        sequen AS Sequen,
                        urutan_kanban AS UrutanKanban,
                        qty_product AS QtyProduct,
                        no_4m AS No4m,
                        qty_defect_mesin AS QtyDefectMesin,
                        qty_defect_operator AS QtyDefectOperator,
                        kode_defect AS KodeDefect,
                        lot_id_wire AS LotIdWire,
                        lot_id_terminal_a AS LotIdTerminalA,
                        lot_id_terminal_b AS LotIdTerminalB,
                        issue_kanban AS IssueKanban,
                        cut_length AS CutLength,
                        kombinasi_wire AS KombinasiWire,
                        terminal_a AS TerminalA,
                        terminal_b AS TerminalB,
                        seal_a AS SealA,
                        seal_b AS SealB,
                        qty_master AS QtyMaster,
                        front_ch_a AS FrontChA,
                        front_cw_a AS FrontCwA,
                        rear_ch_a AS RearChA,
                        rear_cw_a AS RearCwA,
                        front_ch_b AS FrontChB,
                        front_cw_b AS FrontCwB,
                        rear_ch_b AS RearChB,
                        rear_cw_b AS RearCwB,
                        waktu_mulai AS WaktuMulai,
                        waktu_selesai AS WaktuSelesai
                    FROM lko_records
                    WHERE no_mesin = @NoMesin
                    ORDER BY waktu_simpan DESC";

                var results = await connection.QueryAsync<LkoRecordDto>(sql, new { NoMesin = noMesin });
                return results.ToList();
            }
        }

        public async Task<List<LkoRecordDto>> GetRecordsByDateAsync(string noMesin, DateTime date)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string sql = @"
                    SELECT 
                        id AS Id,
                        waktu_simpan AS WaktuSimpan,
                        no_mesin AS NoMesin,
                        id_mesin AS IdMesin,
                        shift_name AS ShiftName,
                        nik AS Nik,
                        sequen AS Sequen,
                        urutan_kanban AS UrutanKanban,
                        qty_product AS QtyProduct,
                        no_4m AS No4m,
                        qty_defect_mesin AS QtyDefectMesin,
                        qty_defect_operator AS QtyDefectOperator,
                        kode_defect AS KodeDefect,
                        lot_id_wire AS LotIdWire,
                        lot_id_terminal_a AS LotIdTerminalA,
                        lot_id_terminal_b AS LotIdTerminalB,
                        issue_kanban AS IssueKanban,
                        cut_length AS CutLength,
                        kombinasi_wire AS KombinasiWire,
                        terminal_a AS TerminalA,
                        terminal_b AS TerminalB,
                        seal_a AS SealA,
                        seal_b AS SealB,
                        qty_master AS QtyMaster,
                        front_ch_a AS FrontChA,
                        front_cw_a AS FrontCwA,
                        rear_ch_a AS RearChA,
                        rear_cw_a AS RearCwA,
                        front_ch_b AS FrontChB,
                        front_cw_b AS FrontCwB,
                        rear_ch_b AS RearChB,
                        rear_cw_b AS RearCwB,
                        waktu_mulai AS WaktuMulai,
                        waktu_selesai AS WaktuSelesai
                    FROM lko_records
                    WHERE no_mesin = @NoMesin
                      AND DATE(waktu_simpan) = DATE(@TargetDate)
                    ORDER BY waktu_simpan DESC";

                var results = await connection.QueryAsync<LkoRecordDto>(sql, new { NoMesin = noMesin, TargetDate = date });
                return results.ToList();
            }
        }
    }
}
