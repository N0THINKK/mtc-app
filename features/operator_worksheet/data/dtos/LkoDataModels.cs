using System;

namespace mtc_app.features.operator_worksheet.data.dtos
{
    public class PrdLogDto
    {
        public string Sequen { get; set; } = string.Empty;
        public string UrutanPengerjaan { get; set; } = string.Empty;
        public string WaktuMulaiPengerjaan { get; set; } = string.Empty;
        public string WaktuSelesaiPengerjaan { get; set; } = string.Empty;
        public string QtyProduk { get; set; } = string.Empty;
        public string QtyDefect { get; set; } = string.Empty;
    }

    public class PrdmstDto
    {
        public string UrutanSequen { get; set; } = string.Empty;
        public string Sequen { get; set; } = string.Empty;
        public string CutLength { get; set; } = string.Empty;
        public string Qty { get; set; } = string.Empty;
        public string PanjangStripSisiA { get; set; } = string.Empty;
        public string PanjangStripSisiB { get; set; } = string.Empty;
        public string TerminalA { get; set; } = string.Empty;
        public string TerminalB { get; set; } = string.Empty;
        public string MunculkanGambarTermA { get; set; } = string.Empty;
        public string MunculkanGambarTermB { get; set; } = string.Empty;
        public string KombinasiWire { get; set; } = string.Empty;
        public string HasTerminalA { get; set; } = string.Empty;
        public string HasTerminalB { get; set; } = string.Empty;
        public string SealA { get; set; } = string.Empty;
        public string SealB { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO untuk data dari Jissk.dat — Front/Rear crimp height dan crimp width.
    /// </summary>
    public class JisskDto
    {
        public string RawSequen { get; set; } = string.Empty; // kolom 2, misal "83360"
        public string Sequen4 { get; set; } = string.Empty;   // 4 digit terakhir, misal "3360"

        // Sisi A
        public string FrontChA { get; set; } = "0";
        public string FrontCwA { get; set; } = "0";
        public string RearChA { get; set; } = "0";
        public string RearCwA { get; set; } = "0";

        // Sisi B
        public string FrontChB { get; set; } = "0";
        public string FrontCwB { get; set; } = "0";
        public string RearChB { get; set; } = "0";
        public string RearCwB { get; set; } = "0";
    }

    /// <summary>
    /// DTO untuk menyimpan data LKO operator ke MySQL (Opsi B).
    /// Data ini terpisah dari PrdLog.csv bawaan mesin.
    /// </summary>
    public class LkoRecordDto
    {
        public int Id { get; set; }
        public DateTime WaktuSimpan { get; set; } = DateTime.Now;
        public string NoMesin { get; set; } = string.Empty;
        public int? IdMesin { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public string Nik { get; set; } = string.Empty;

        // Sequence & Kanban
        public string Sequen { get; set; } = string.Empty;
        public string UrutanKanban { get; set; } = string.Empty;

        // Produksi
        public int QtyProduct { get; set; } = 0;
        public int QtyDefectMesin { get; set; } = 0;
        public int QtyDefectOperator { get; set; } = 0;
        public string KodeDefect { get; set; } = string.Empty;
        public string LotIdWire { get; set; } = string.Empty;
        public string LotIdTerminalA { get; set; } = string.Empty;
        public string LotIdTerminalB { get; set; } = string.Empty;
        public string IssueKanban { get; set; } = string.Empty;
        public string CutLength { get; set; } = string.Empty;

        // Master data (prdmst)
        public string KombinasiWire { get; set; } = string.Empty;
        public string TerminalA { get; set; } = string.Empty;
        public string TerminalB { get; set; } = string.Empty;
        public string SealA { get; set; } = string.Empty;
        public string SealB { get; set; } = string.Empty;
        public string QtyMaster { get; set; } = string.Empty;

        // Jissk — Front/Rear Sisi A
        public string FrontChA { get; set; } = "0";
        public string FrontCwA { get; set; } = "0";
        public string RearChA { get; set; } = "0";
        public string RearCwA { get; set; } = "0";

        // Jissk — Front/Rear Sisi B
        public string FrontChB { get; set; } = "0";
        public string FrontCwB { get; set; } = "0";
        public string RearChB { get; set; } = "0";
        public string RearCwB { get; set; } = "0";

        // Waktu dari mesin
        public string WaktuMulai { get; set; } = string.Empty;
        public string WaktuSelesai { get; set; } = string.Empty;
    }
}
