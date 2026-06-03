using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.applicator_patrol.data.dtos;
using mtc_app.features.applicator_patrol.data.repositories;

namespace mtc_app.features.technician.presentation.controllers
{
    public class TechnicianApplicatorPatrolController
    {
        private readonly ITechnicianApplicatorPatrolView _view;
        private readonly IApplicatorPatrolRepository _repository;

        public TechnicianApplicatorPatrolController(ITechnicianApplicatorPatrolView view, IApplicatorPatrolRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            try
            {
                var statsTask = _repository.GetApplicatorNgStatsAsync(start, end);
                var listTask = _repository.GetApplicatorNgListAsync(start, end, _view.CurrentSort);

                await Task.WhenAll(statsTask, listTask);

                var stats = statsTask.Result;
                var list = listTask.Result.ToList();

                _view.UpdateStats(stats?.TotalNgCount ?? 0);

                if (list.Any())
                {
                    _view.HideEmptyState();
                    _view.UpdateGrid(list);
                }
                else
                {
                    _view.UpdateGrid(new List<ApplicatorNgDto>());
                    _view.ShowEmptyState();
                }
            }
            catch (Exception ex)
            {
                _view.ShowError("Gagal memuat data NG Aplikator: " + ex.Message);
            }
        }
    }
}
