using System;
using System.Data;
using System.Threading.Tasks;
using mtc_app.features.machine_history.data.repositories;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public class ChecksheetHistoryController
    {
        private readonly IChecksheetHistoryView _view;
        private readonly IMachineHistoryRepository _repository;
        private readonly int _machineId;
        private readonly int _templateId;
        private readonly string _roleTarget;

        public ChecksheetHistoryController(IChecksheetHistoryView view, IMachineHistoryRepository repository, int machineId, int templateId, string roleTarget)
        {
            _view = view;
            _repository = repository;
            _machineId = machineId;
            _templateId = templateId;
            _roleTarget = roleTarget;
        }

        public async Task LoadHistoryDataAsync()
        {
            try
            {
                _view.ShowLoading();

                DataTable pivotData = await _repository.GetChecksheetHistoryPivotAsync(_machineId, _templateId, _roleTarget, 30);

                if (pivotData.Rows.Count == 0)
                {
                    _view.SetStatusMessage("Tidak ada riwayat patroli dalam 30 hari terakhir.");
                }
                else
                {
                    _view.DisplayData(pivotData);
                }
            }
            catch (Exception ex)
            {
                _view.SetStatusMessage("Gagal memuat history: " + ex.Message, isError: true);
            }
        }
    }
}
