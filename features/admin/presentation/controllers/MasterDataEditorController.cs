using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mtc_app.features.admin.data.repositories;

namespace mtc_app.features.admin.presentation.controllers
{
    public class MasterDataEditorController
    {
        private readonly IMasterDataEditorView _view;
        private readonly IAdminRepository _repository;

        public MasterDataEditorController(IMasterDataEditorView view, IAdminRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task SaveDataAsync(string category, string subCategory, bool isEditMode, Dictionary<string, object> dataToSave)
        {
            if (isEditMode && category == "User")
            {
                bool isNewPassFilled = !string.IsNullOrWhiteSpace(dataToSave["new_password"]?.ToString());
                bool isOldPassFilled = !string.IsNullOrWhiteSpace(dataToSave["old_password"]?.ToString());
                
                if (isNewPassFilled && !isOldPassFilled) 
                { 
                    _view.ShowWarning("Untuk mengubah password, Anda WAJIB memasukkan Password Lama!");
                    return; 
                }
            }

            try
            {
                _view.SetBusyState(true);
                bool success = await _repository.SaveMasterDataAsync(category, subCategory, isEditMode, dataToSave);
                
                if (success) 
                {
                    _view.ShowSuccess("Data berhasil disimpan!");
                    _view.CloseForm(true); 
                } 
                else 
                {
                    _view.ShowError("Penyimpanan gagal, data tidak tersimpan.", "Gagal");
                }
            }
            catch (Exception ex) 
            { 
                _view.ShowError(ex.Message, "Gagal Disimpan"); 
            }
            finally 
            { 
                _view.SetBusyState(false); 
            }
        }
    }
}
