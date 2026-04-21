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
        public string ShiftName { get; set; } = string.Empty;
        public string Nik { get; set; } = string.Empty;
        public string Sequen { get; set; } = string.Empty;
        public string UrutanKanban { get; set; } = string.Empty;
        public int QtyDefectOperator { get; set; } = 0;
        public string KodeDefect { get; set; } = string.Empty;
        public int QtyProduct { get; set; } = 0;
    }
}
