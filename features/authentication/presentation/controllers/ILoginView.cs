using mtc_app.shared.data.dtos;

namespace mtc_app.features.authentication.presentation.controllers
{
    public interface ILoginView
    {
        string SelectedRole { get; }
        string Identity { get; }
        string Password { get; }
        
        void SetBusyState(bool isBusy);
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Peringatan");
        void ShowSuccess(string message, string title = "Sukses");
        void HideForm();
        void ShowForm();
        void ProceedToDashboard(UserDto user);
        void SaveOperatorNikToHistory(string nik);
    }
}
