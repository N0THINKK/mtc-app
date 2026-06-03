using System.Collections.Generic;

namespace mtc_app.features.admin.presentation.controllers
{
    public interface IMasterDataEditorView
    {
        void SetBusyState(bool isBusy);
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Peringatan");
        void ShowSuccess(string message, string title = "Sukses");
        void CloseForm(bool success);
    }
}
