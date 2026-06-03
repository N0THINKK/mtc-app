using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;

namespace mtc_app.features.technician.presentation.controllers
{
    public class TechnicianPerformanceController
    {
        private readonly ITechnicianPerformanceView _view;
        private readonly ITechnicianRepository _repository;

        public TechnicianPerformanceController(ITechnicianPerformanceView view, ITechnicianRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadDataAsync(DateTime start, DateTime end)
        {
            try
            {
                var dataRaw = await _repository.GetLeaderboardAsync(start, end);
                var data = dataRaw?.ToList() ?? new List<TechnicianPerformanceDto>();
                
                if (data.Count > 0)
                {
                    int totalRepairs = data.Sum(t => t.TotalRepairs);
                    double avgRating = data.Average(t => t.AverageRating);
                    _view.UpdateStats(totalRepairs, (decimal)avgRating);

                    string metric = _view.CurrentMetric;
                    bool asc = _view.SortAscending;

                    switch (metric)
                    {
                        case "rating":
                            data = asc
                                ? data.OrderBy(t => t.AverageRating).ToList()
                                : data.OrderByDescending(t => t.AverageRating).ToList();
                            break;
                        case "stars":
                            data = asc
                                ? data.OrderBy(t => t.TotalStars).ToList()
                                : data.OrderByDescending(t => t.TotalStars).ToList();
                            break;
                        default: // repairs
                            data = asc
                                ? data.OrderBy(t => t.TotalRepairs).ToList()
                                : data.OrderByDescending(t => t.TotalRepairs).ToList();
                            break;
                    }
                }

                _view.UpdateGrid(data);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal memuat data leaderboard: {ex.Message}");
            }
        }
    }
}
