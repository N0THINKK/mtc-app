using System.Collections.Generic;

namespace mtc_app.features.authentication.presentation.controllers
{
    public interface ISetupView
    {
        string MachineType { get; }
        string MachineArea { get; }
        string MachineNumber { get; }
        string SelectedTemplateName { get; }
        bool IsTemplateVisible { get; }

        void ShowTemplates(List<string> templates);
        void HideTemplates();
        void PopulateDropdowns(string[] types, string[] areas);
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Peringatan");
        void ShowSuccess(string message, string title = "Sukses");
        void CloseForm(bool success);
    }
}
