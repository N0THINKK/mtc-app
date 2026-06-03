using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.group_leader.data.dtos;
using mtc_app.features.group_leader.data.repositories;

namespace mtc_app.features.group_leader.presentation.controllers
{
    public class GroupLeaderDashboardController
    {
        private readonly IGroupLeaderDashboardView _view;
        private readonly IGroupLeaderRepository _repository;
        private List<GroupLeaderTicketDto> _allTickets = new List<GroupLeaderTicketDto>();

        public GroupLeaderDashboardController(IGroupLeaderDashboardView view, IGroupLeaderRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var tickets = await _repository.GetTicketsAsync();
                _allTickets = tickets?.ToList() ?? new List<GroupLeaderTicketDto>();

                UpdateStats();
                PopulateAreas();
                ApplyFiltersAndRender();
                _view.UpdateSystemStatus(true);
            }
            catch (Exception ex)
            {
                _view.UpdateSystemStatus(false);
                _view.ShowError($"Gagal memuat data: {ex.Message}");
            }
        }

        public void ApplyFiltersAndRender()
        {
            var filtered = _allTickets.AsEnumerable();

            // Status Filter
            int statusIndex = _view.SelectedStatusIndex;
            if (statusIndex == 1) // Sudah Direview
            {
                filtered = filtered.Where(t => t.GlValidatedAt.HasValue || (t.GlRatingScore.HasValue && t.GlRatingScore > 0));
            }
            else if (statusIndex == 2) // Belum Direview
            {
                filtered = filtered.Where(t => !t.GlValidatedAt.HasValue && (!t.GlRatingScore.HasValue || t.GlRatingScore == 0));
            }

            // Area Filter
            if (_view.SelectedAreaIndex > 0)
            {
                string selectedArea = _view.SelectedAreaName;
                filtered = filtered.Where(t => t.AreaName == selectedArea);
            }

            // Month Filter
            if (_view.SelectedMonthIndex > 0)
            {
                int selectedMonth = _view.SelectedMonthIndex;
                filtered = filtered.Where(t => t.CreatedAt.Month == selectedMonth);
            }

            // Sort Time
            int sortIndex = _view.SelectedSortIndex;
            if (sortIndex == 0) // Terbaru
            {
                filtered = filtered.OrderByDescending(t => t.CreatedAt); // Fix: Terbaru = Descending
            }
            else // Terlama
            {
                filtered = filtered.OrderBy(t => t.CreatedAt); // Fix: Terlama = Ascending
            }

            _view.UpdateGrid(filtered.ToList());
        }

        private void UpdateStats()
        {
            int totalTickets = _allTickets.Count;
            int reviewedTickets = _allTickets.Count(t => t.GlValidatedAt.HasValue || (t.GlRatingScore.HasValue && t.GlRatingScore > 0));
            int pendingTickets = totalTickets - reviewedTickets;
            _view.UpdateStats(totalTickets, reviewedTickets, pendingTickets);
        }

        private void PopulateAreas()
        {
            var areas = _allTickets
                .Where(t => !string.IsNullOrEmpty(t.AreaName))
                .Select(t => t.AreaName)
                .Distinct()
                .OrderBy(a => a)
                .ToList();
            
            _view.PopulateAreaFilter(areas);
        }
    }
}
