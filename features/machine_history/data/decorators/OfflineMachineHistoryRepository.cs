using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Threading.Tasks;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.shared.data.local;
using mtc_app.shared.data.services;

namespace mtc_app.features.machine_history.data.decorators
{
    public class OfflineMachineHistoryRepository : IMachineHistoryRepository
    {
        private readonly IMachineHistoryRepository _innerRepository;
        private readonly OfflineRepository _offlineRepo;
        private readonly NetworkMonitor _networkMonitor;

        public OfflineMachineHistoryRepository(
            IMachineHistoryRepository innerRepository,
            OfflineRepository offlineRepo,
            NetworkMonitor networkMonitor)
        {
            _innerRepository = innerRepository;
            _offlineRepo = offlineRepo;
            _networkMonitor = networkMonitor;
        }

        public async Task<IEnumerable<MachineHistoryDto>> GetHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, string search = null, string areaFilter = null, int? machineId = null)
        {
            // Prioritaskan Online
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    // Teruskan semua parameter (termasuk areaFilter dan machineId) ke repository asli (online)
                    return await _innerRepository.GetHistoryAsync(startDate, endDate, search, areaFilter, machineId);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineHistory] Network error fetching history: {ex.Message}");
                    // Jika gagal koneksi, jatuh ke logika offline di bawah
                }
            }

            // Fallback Offline:
            // Saat ini kita mengembalikan list kosong atau data lokal jika diimplementasikan.
            // Fitur sinkronisasi riwayat penuh ke lokal biasanya berat, jadi default list kosong aman.
            return new List<MachineHistoryDto>(); 
        }

        public async Task<(long TicketId, string TicketCode)> CreateTicketAsync(CreateTicketRequest request)
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.CreateTicketAsync(request);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineTicket] Network error, buffering ticket: {ex.Message}");
                }
            }

            // Mode Offline: Simpan ke SQLite Lokal
            // ID dikembalikan sebagai angka negatif untuk menandakan "Disimpan Lokal"
            int pendingId = _offlineRepo.SavePendingTicket(request);
            return (-pendingId, "OFFLINE-QUEUED"); 
        }

        public async Task<MachineHistoryDto> GetActiveTicketForMachineAsync(int machineId)
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetActiveTicketForMachineAsync(machineId);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    // Fallback ke cek offline
                }
            }

            // Mode Offline: 
            // Cek apakah ada tiket pending (status Waiting/Repairing) di SQLite untuk mesin ini.
            // Saat ini kita kembalikan null karena fitur pencarian pending ticket by MachineID
            // perlu ditambahkan di OfflineRepository.
            return null;
        }

        public async Task<DataTable> GetChecksheetHistoryPivotAsync(int machineId, int templateId, int days = 30)
        {
            if (_networkMonitor.IsOnline)
            {
                try
                {
                    return await _innerRepository.GetChecksheetHistoryPivotAsync(machineId, templateId, days);
                }
                catch (Exception ex) when (IsNetworkException(ex))
                {
                    // Fallback to offline
                    System.Diagnostics.Debug.WriteLine($"[OfflineHistory] Network error fetching checksheet pivot: {ex.Message}");
                }
            }

            // Fallback Offline: return empty table
            DataTable emptyTable = new DataTable();
            emptyTable.Columns.Add("Tanggal", typeof(string));
            return emptyTable;
        }

        private bool IsNetworkException(Exception ex)
        {
            if (ex == null) return false;
            if (ex is SocketException) return true;
            if (ex is TimeoutException) return true;
            
            var message = ex.Message?.ToLowerInvariant() ?? "";
            if (message.Contains("unable to connect")) return true;
            if (message.Contains("connection refused")) return true;
            if (message.Contains("timeout")) return true;
            if (message.Contains("host not found")) return true;
            
            return ex.InnerException != null && IsNetworkException(ex.InnerException);
        }
    }
}