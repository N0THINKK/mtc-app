using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExcelDataReader;

namespace mtc_app.features.applicator_patrol.data.services
{
    public static class ApplicatorExcelReader
    {
        static ApplicatorExcelReader()
        {
            // Dibutuhkan untuk membaca file Excel jadul di .NET Core / .NET 5+
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public static (List<string> SideA, List<string> SideB) ReadApplicators(string excelPath, string machineCode)
        {
            var sideA = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sideB = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(excelPath))
                return (new List<string>(), new List<string>());

            try
            {
                using (var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // Mendeteksi otomatis apakah ini .xls atau .xlsx
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });

                        if (result.Tables.Count > 0)
                        {
                            var table = result.Tables[0];
                            
                            // Ambil nama kolom yang relevan dari baris pertama (header)
                            int colNoMesin = GetColumnIndex(table, "NoMesin");
                            int colNoApplikator = GetColumnIndex(table, "NoApplikator");
                            int colSisi = GetColumnIndex(table, "Sisi");

                            if (colNoMesin != -1 && colNoApplikator != -1 && colSisi != -1)
                            {
                                foreach (System.Data.DataRow row in table.Rows)
                                {
                                    string rowMesin = row[colNoMesin]?.ToString() ?? "";
                                    
                                    // Hanya filter nomor mesin yang sesuai persis dengan machineCode yang dicari di dropdown
                                    if (rowMesin.Equals(machineCode, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string sisi = row[colSisi]?.ToString()?.Trim().ToUpper() ?? "";
                                        string applicator = row[colNoApplikator]?.ToString()?.Trim() ?? "";

                                        if (!string.IsNullOrEmpty(applicator))
                                        {
                                            if (sisi == "A") sideA.Add(applicator);
                                            else if (sisi == "B") sideB.Add(applicator);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading Excel: {ex.Message}");
            }

            return (sideA.OrderBy(x => x).ToList(), sideB.OrderBy(x => x).ToList());
        }

        private static int GetColumnIndex(System.Data.DataTable table, string columnName)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (table.Columns[i].ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase) || 
                    table.Columns[i].ColumnName.Contains(columnName))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
