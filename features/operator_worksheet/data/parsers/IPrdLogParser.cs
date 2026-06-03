using System.Collections.Generic;
using mtc_app.features.operator_worksheet.data.dtos;

namespace mtc_app.features.operator_worksheet.data.parsers
{
    public interface IPrdLogParser
    {
        List<PrdLogDto> Parse(string filePath);
        bool IsXmlSource { get; }
    }
}
