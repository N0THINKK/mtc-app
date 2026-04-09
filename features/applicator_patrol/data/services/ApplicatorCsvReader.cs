using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace mtc_app.features.applicator_patrol.data.services
{
    /// <summary>
    /// Membaca file prdmst.csv dan mengekstrak nomor aplikator unik.
    /// Kolom index 11 (0-based) = Aplikator Sisi A
    /// Kolom index 12 (0-based) = Aplikator Sisi B
    /// Nilai "STRIP ONLY" dan kosong dilewati.
    /// </summary>
    public static class ApplicatorCsvReader
    {
        private const int COL_APPLICATOR_A = 11;
        private const int COL_APPLICATOR_B = 12;
        private static readonly HashSet<string> SKIP_VALUES = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "STRIP ONLY", "STRIP_ONLY", ""
        };

        public static (List<string> SideA, List<string> SideB) ReadApplicators(string csvFilePath)
        {
            var sideA = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sideB = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(csvFilePath))
                return (new List<string>(), new List<string>());

            try
            {
                // Baca dengan shared read agar tidak lock file yang sedang ditulis mesin
                using (var stream = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var cols = line.Split(',');

                        if (cols.Length > COL_APPLICATOR_A)
                        {
                            string valA = cols[COL_APPLICATOR_A].Trim();
                            if (!SKIP_VALUES.Contains(valA) && valA.Length > 0)
                                sideA.Add(valA);
                        }

                        if (cols.Length > COL_APPLICATOR_B)
                        {
                            string valB = cols[COL_APPLICATOR_B].Trim();
                            if (!SKIP_VALUES.Contains(valB) && valB.Length > 0)
                                sideB.Add(valB);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Jika file sedang dikunci, return kosong - akan di-retry oleh FileWatcher
                return (new List<string>(), new List<string>());
            }

            return (sideA.OrderBy(x => x).ToList(), sideB.OrderBy(x => x).ToList());
        }
    }
}
