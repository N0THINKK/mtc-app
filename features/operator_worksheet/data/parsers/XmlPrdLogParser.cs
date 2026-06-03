using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using mtc_app.features.operator_worksheet.data.dtos;

namespace mtc_app.features.operator_worksheet.data.parsers
{
    public class XmlPrdLogParser : IPrdLogParser
    {
        public bool IsXmlSource => true;

        public List<PrdLogDto> Parse(string filePath)
        {
            var result = new List<PrdLogDto>();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return result;

            try
            {
                XDocument doc;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    doc = XDocument.Load(fs);
                }

                XNamespace ns = doc.Root.GetDefaultNamespace();

                foreach (var el in doc.Root.Elements(ns + "ProductionLog"))
                {
                    string jobNo = el.Element(ns + "_jobNo")?.Value?.Trim() ?? "";
                    string endDt = el.Element(ns + "_endDateTime")?.Value?.Trim() ?? "";
                    string startDt = el.Element(ns + "_startDateTime")?.Value?.Trim() ?? "";
                    string goodPiece = el.Element(ns + "_goodPiece")?.Value?.Trim() ?? "0";
                    string missPiece = el.Element(ns + "_missPiece")?.Value?.Trim() ?? "0";

                    string displayEnd = endDt;
                    if (DateTime.TryParse(endDt, out DateTime dtEnd))
                        displayEnd = dtEnd.ToString("HH:mm:ss");

                    string displayStart = startDt;
                    if (DateTime.TryParse(startDt, out DateTime dtStart))
                        displayStart = dtStart.ToString("HH:mm:ss");

                    result.Add(new PrdLogDto
                    {
                        Sequen = jobNo,
                        UrutanPengerjaan = displayEnd,          
                        WaktuMulaiPengerjaan = displayStart,
                        WaktuSelesaiPengerjaan = displayEnd,
                        QtyProduk = goodPiece,
                        QtyDefect = missPiece
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XmlPrdLogParser] Error reading ProductionLog.xml: {ex.Message}");
            }

            return result;
        }
    }
}
