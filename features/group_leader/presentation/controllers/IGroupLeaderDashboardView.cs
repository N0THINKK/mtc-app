using System.Collections.Generic;
using mtc_app.features.group_leader.data.dtos;

namespace mtc_app.features.group_leader.presentation.controllers
{
    public interface IGroupLeaderDashboardView
    {
        int SelectedStatusIndex { get; }
        int SelectedSortIndex { get; }
        int SelectedAreaIndex { get; }
        string SelectedAreaName { get; }
        int SelectedMonthIndex { get; }

        void UpdateStats(int totalTickets, int reviewedTickets, int pendingTickets);
        void PopulateAreaFilter(List<string> areas);
        void UpdateGrid(List<GroupLeaderTicketDto> tickets);
        void UpdateSystemStatus(bool isActive);
        void ShowError(string message);
    }
}
