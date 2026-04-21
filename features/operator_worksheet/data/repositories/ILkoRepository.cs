using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.operator_worksheet.data.dtos;

namespace mtc_app.features.operator_worksheet.data.repositories
{
    public interface ILkoRepository
    {
        /// <summary>
        /// Simpan record LKO operator ke database MySQL.
        /// </summary>
        Task<int> SaveLkoRecordAsync(LkoRecordDto record);

        /// <summary>
        /// Ambil semua record LKO hari ini untuk mesin tertentu.
        /// </summary>
        Task<List<LkoRecordDto>> GetTodayRecordsAsync(string noMesin);
    }
}
