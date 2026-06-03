using System;
using System.Collections.Generic;
using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.technician.presentation.controllers
{
    public interface ITechnicianWorkQueueView
    {
        int SelectedStatusFilterIndex { get; }
        int SelectedSortIndex { get; }
        
        void UpdateStatusIndicator(bool isActive, string timestampText);
        void UpdateStats(int openCount, int processCount, int doneCount, int machineRunning, int machineTotal);
        void RenderTickets(List<TicketDto> tickets);
        void ShowEmptyState(string title, string message);
        void ShowError(string message);
    }
}
