using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.data.repositories
{
    public interface IMachineHistoryRepository
    {
        /// <summary>
        /// Mengambil riwayat tiket mesin dengan filter tanggal, pencarian teks, dan area.
        /// </summary>
        /// <param name="startDate">Tanggal awal filter</param>
        /// <param name="endDate">Tanggal akhir filter</param>
        /// <param name="search">Kata kunci pencarian (opsional)</param>
        /// <param name="areaFilter">Nama area untuk filter spesifik (opsional)</param>
        /// <param name="machineId">ID mesin untuk filter history spesifik (opsional)</param>
        Task<IEnumerable<MachineHistoryDto>> GetHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, string search = null, string areaFilter = null, int? machineId = null);

        /// <summary>
        /// Membuat tiket baru (Lapor Kerusakan).
        /// </summary>
        Task<(long TicketId, string TicketCode)> CreateTicketAsync(CreateTicketRequest request);

        /// <summary>
        /// Mengecek apakah ada tiket yang sedang aktif (Open/Repairing) untuk mesin tertentu.
        /// Digunakan untuk fitur "Continue Problem".
        /// </summary>
        Task<MachineHistoryDto> GetActiveTicketForMachineAsync(int machineId);
    }
}