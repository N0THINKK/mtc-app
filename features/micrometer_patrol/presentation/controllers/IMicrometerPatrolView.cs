using System.Collections.Generic;

namespace mtc_app.features.micrometer_patrol.presentation.controllers
{
    public interface IMicrometerPatrolView
    {
        string SelectedShift { get; set; }
        string SelectedMachine { get; set; }
        string SelectedNik { get; set; }
        string Notes { get; }
        
        void PopulateShifts(string[] shifts);
        void PopulateMachines(string[] machines);
        void PopulateOperators(string[] operators);
        
        void LockMachine(bool isLocked);
        void SetTechnicianMode(string username);
        
        string GetPointValue(int index);
        
        void SetBusyState(bool isBusy);
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Peringatan");
        void ShowSuccess(string message, string title = "Sukses");
        void CloseForm(bool success);
    }
}
