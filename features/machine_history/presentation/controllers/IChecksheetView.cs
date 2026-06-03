using System.Collections.Generic;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public interface IChecksheetView
    {
        string Shift { get; }
        
        void SetMachineInfo(string info);
        void SetPelaksanaInfo(string label, string value);
        void ClearQuestions();
        void AddQuestion(int number, int itemId, string name, string standard, string method, string inputType, bool isPendingNg);
        void ShowEmptyState(string message);
        
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Peringatan");
        void ShowSuccess(string message, string title = "Sukses");
        void SetBusyState(bool isBusy);
        void FocusUnansweredQuestion();
        
        List<ChecksheetItemData> GetAnswers();
        
        void CloseForm();
    }

    public class ChecksheetItemData
    {
        public int ItemId { get; set; }
        public string ValueString { get; set; }
        public string Notes { get; set; }
        public bool IsPendingNg { get; set; }
        public bool IsAnswered { get; set; }
    }
}
