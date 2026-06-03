using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;

namespace mtc_app.features.technician.presentation.controllers
{
    public class TechnicianPatrolController
    {
        private readonly ITechnicianPatrolView _view;
        private readonly ITechnicianRepository _repository;

        public TechnicianPatrolController(ITechnicianPatrolView view, ITechnicianRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            try
            {
                var statsTask = _repository.GetPatrolNgStatsAsync(start, end);
                var listTask = _repository.GetPatrolNgListAsync(_view.CurrentFilter, _view.CurrentSort, start, end, _view.CurrentRoleFilter, _view.CurrentItemFilter);
                var itemNamesTask = _repository.GetPatrolNgItemNamesAsync(start, end);

                await Task.WhenAll(statsTask, listTask, itemNamesTask);

                var stats = statsTask.Result;
                var list = listTask.Result.ToList();
                var itemNames = itemNamesTask.Result.ToList();

                _view.UpdateItemFilterList(itemNames, _view.CurrentItemFilter);
                _view.UpdateStats(stats?.PendingCount ?? 0, stats?.ResolvedCount ?? 0);

                if (list.Any())
                {
                    _view.HideEmptyState();
                    _view.UpdateGrid(list);
                }
                else
                {
                    _view.UpdateGrid(new List<PatrolNgDto>());
                    if (_view.CurrentFilter == "NG")
                    {
                        _view.ShowEmptyState("Tidak Ada NG Pending", "Semua masalah checksheet telah diselesaikan.");
                    }
                    else if (_view.CurrentFilter == "Selesai")
                    {
                        _view.ShowEmptyState("Belum Ada NG Selesai", "Belum ada riwayat perbaikan NG pada rentang tanggal ini.");
                    }
                    else
                    {
                        _view.ShowEmptyState("Tidak Ada Data", "Tidak ada riwayat NG yang dilaporkan.");
                    }
                }
            }
            catch (Exception ex)
            {
                _view.ShowError("Gagal memuat data Patroli: " + ex.Message);
            }
        }

        public async Task MarkResolvedAsync(PatrolNgDto dto, DateTime start, DateTime end)
        {
            if (dto.Status == "PERBAIKAN_OK")
            {
                _view.ShowWarning("Item ini sudah berstatus Selesai.");
                return;
            }

            bool confirm = _view.ConfirmAction("Konfirmasi Perbaikan", $"Apakah Anda yakin telah memperbaiki masalah ini?\n\nMesin: {dto.MachineName}\nItem: {dto.ItemName}");
            if (confirm)
            {
                bool success = await _repository.MarkPatrolNgAsResolvedAsync(dto.DetailId);
                if (success)
                {
                    _view.ShowSuccess("Berhasil ditandai selesai.");
                    await LoadDataAsync(start, end);
                }
                else
                {
                    _view.ShowError("Gagal memperbarui status. Silakan coba lagi.");
                }
            }
        }
    }
}
