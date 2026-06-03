using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;

namespace mtc_app.features.technician.presentation.controllers
{
    public class MachinePerformanceController
    {
        private readonly IMachinePerformanceView _view;
        private readonly ITechnicianRepository _repository;

        public MachinePerformanceController(IMachinePerformanceView view, ITechnicianRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            try
            {
                string area = _view.SelectedArea;
                if (area == "All Areas") area = null;

                var result = await _repository.GetMachinePerformanceAsync(start, end, area);
                var data = result?.ToList() ?? new List<MachinePerformanceDto>();
                
                if (data.Count > 0)
                {
                    if (_view.SortAscending)
                        data = data.OrderBy(x => x.TotalDowntimeSeconds).ToList();
                    else
                        data = data.OrderByDescending(x => x.TotalDowntimeSeconds).ToList();
                }

                _view.UpdateGrid(data);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal memuat data mesin: {ex.Message}");
            }
        }
    }
}
