using mtc_app.features.group_leader.data.dtos;

namespace mtc_app.features.rating.presentation.controllers
{
    public interface IRatingGlView
    {
        int RatingScore { get; }
        string RatingNote { get; }
        
        void DisplayTicketData(GroupLeaderTicketDetailDto data);
        void ShowError(string message, string title = "Error");
        void ShowSuccess(string message, string title = "Sukses");
        void CloseForm(bool success);
    }
}
