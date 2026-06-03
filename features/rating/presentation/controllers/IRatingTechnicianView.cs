using mtc_app.features.technician.data.dtos;

namespace mtc_app.features.rating.presentation.controllers
{
    public interface IRatingTechnicianView
    {
        string RatingNote { get; }
        
        void DisplayTicketData(TechnicianTicketDetailDto data);
        void DisplayPatrolData(PatrolNgDto patrol);
        void ShowError(string message, string title = "Error");
        void ShowSuccess(string message, string title = "Sukses");
        void SetReadOnlyMode();
        void CloseForm(bool success);
    }
}
