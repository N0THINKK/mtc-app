using System;
using System.Threading.Tasks;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;

namespace mtc_app.features.rating.presentation.controllers
{
    public class RatingTechnicianController
    {
        private readonly IRatingTechnicianView _view;
        private readonly ITechnicianRepository _repository;
        private readonly long _ticketId;
        private readonly PatrolNgDto _patrolDto;

        public RatingTechnicianController(IRatingTechnicianView view, ITechnicianRepository repository, long ticketId, PatrolNgDto patrolDto)
        {
            _view = view;
            _repository = repository;
            _ticketId = ticketId;
            _patrolDto = patrolDto;
        }

        public async Task LoadTicketDataAsync()
        {
            try
            {
                if (_ticketId == 0 && _patrolDto != null)
                {
                    _view.DisplayPatrolData(_patrolDto);
                    return;
                }

                var data = await _repository.GetTicketDetailAsync(_ticketId);

                if (data != null)
                {
                    _view.DisplayTicketData(data);
                    
                    if (!string.IsNullOrEmpty(data.TechRatingNote))
                    {
                        _view.SetReadOnlyMode();
                    }
                }
                else
                {
                    _view.ShowError("Data tiket tidak ditemukan!");
                    _view.CloseForm(false);
                }
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal memuat data: {ex.Message}", "Error Database");
            }
        }

        public async Task SubmitRatingAsync(bool isReadOnlyMode)
        {
            if (isReadOnlyMode)
            {
                _view.CloseForm(true);
                return;
            }

            try
            {
                if (_ticketId > 0)
                {
                    await _repository.UpdateOperatorRatingAsync(_ticketId, 0, _view.RatingNote);
                }
                
                _view.ShowSuccess("Penilaian operator berhasil disimpan.");
                _view.CloseForm(true);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal menyimpan penilaian: {ex.Message}", "Error Database");
            }
        }
    }
}
