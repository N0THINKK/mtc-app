using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;

namespace mtc_app.features.technician.presentation.controllers
{
    public class TechnicianWorkQueueController
    {
        private readonly ITechnicianWorkQueueView _view;
        private readonly ITechnicianRepository _repository;
        private List<TicketDto> _allTickets = new List<TicketDto>();
        private string _lastDataFingerprint = "";
        private bool _isLoading = false;

        public TechnicianWorkQueueController(ITechnicianWorkQueueView view, ITechnicianRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        private string BuildDataFingerprint(List<TicketDto> tickets)
        {
            if (tickets == null || tickets.Count == 0) return "EMPTY";
            var parts = tickets.Select(t =>
                $"{t.TicketId}|{t.StatusId}|{t.IsMachineRunning}|{t.ArrivalSeconds}|{t.RepairSeconds}|{t.InspectionSeconds}|{t.TechnicianName}");
            return string.Join(";", parts);
        }

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var ticketsRaw = await _repository.GetActiveTicketsAsync(start, end);
                var newTickets = ticketsRaw.ToList();
                
                int openCount = newTickets.Count(t => t.StatusId == 1);
                int processCount = newTickets.Count(t => t.StatusId == 2);
                int doneCount = newTickets.Count(t => t.StatusId == 3);
                
                var machineStats = await _repository.GetMachineRunStatsAsync();
                _view.UpdateStats(openCount, processCount, doneCount, machineStats.Running, machineStats.Total);
                
                string timestampText = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";
                
                var newFingerprint = BuildDataFingerprint(newTickets);
                if (newFingerprint == _lastDataFingerprint)
                {
                    _view.UpdateStatusIndicator(true, timestampText);
                    return;
                }

                _allTickets = newTickets;
                _lastDataFingerprint = newFingerprint;
                ApplyFiltersAndRender(_allTickets);
                _view.UpdateStatusIndicator(true, timestampText);
            }
            catch (Exception ex)
            {
                _view.UpdateStatusIndicator(false, "Terakhir diperbarui: Gagal");
                _view.ShowError(ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        public void ForceReRender()
        {
            ApplyFiltersAndRender(_allTickets);
        }

        private void ApplyFiltersAndRender(List<TicketDto> allTickets)
        {
            var filtered = allTickets.AsEnumerable();

            int statusIndex = _view.SelectedStatusFilterIndex;
            if (statusIndex == 1) filtered = filtered.Where(t => t.StatusId == 1);
            else if (statusIndex == 2) filtered = filtered.Where(t => t.StatusId == 2);
            else if (statusIndex == 3) filtered = filtered.Where(t => t.StatusId == 3);
            else if (statusIndex == 4) filtered = filtered.Where(t => t.StatusId == 4);
            else if (statusIndex == 5) filtered = filtered.Where(t => t.IsMachineRunning == 0);

            int sortIndex = _view.SelectedSortIndex;
            List<TicketDto> sortedList;

            if (sortIndex == 0)
            {
                sortedList = filtered.OrderByDescending(t => t.StatusId).ThenByDescending(t => t.CreatedAt).ToList();
            }
            else if (sortIndex == 1)
            {
                sortedList = filtered.OrderBy(t => t.CreatedAt).ToList();
            }
            else
            {
                sortedList = filtered.OrderByDescending(t => t.CreatedAt).ToList();
            }

            if (sortedList.Count == 0)
            {
                _view.ShowEmptyState("Tidak Ada Tiket", "Semua tiket telah diproses.");
            }
            else
            {
                _view.RenderTickets(sortedList);
            }
        }
    }
}
