using System;
using System.Threading.Tasks;
using mtc_app.features.group_leader.data.repositories;

namespace mtc_app.features.rating.presentation.controllers
{
    public class RatingGlController
    {
        private readonly IRatingGlView _view;
        private readonly IGroupLeaderRepository _repository;
        private readonly Guid _ticketId;

        public RatingGlController(IRatingGlView view, IGroupLeaderRepository repository, Guid ticketId)
        {
            _view = view;
            _repository = repository;
            _ticketId = ticketId;
        }

        public async Task LoadTicketDataAsync()
        {
            try
            {
                var data = await _repository.GetTicketDetailAsync(_ticketId);

                if (data != null)
                {
                    _view.DisplayTicketData(data);
                }
                else
                {
                    _view.ShowError("Data tiket tidak ditemukan!\n\nFitur ini memerlukan koneksi internet.");
                    _view.CloseForm(false);
                }
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal memuat data: {ex.Message}", "Error Database");
            }
        }

        public async Task SubmitRatingAsync()
        {
            if (_view.RatingScore == 0)
            {
                _view.ShowError("Mohon berikan rating (bintang).", "Validasi");
                return;
            }

            try
            {
                await _repository.ValidateTicketAsync(_ticketId, _view.RatingScore, _view.RatingNote);
                
                _view.ShowSuccess("Validasi berhasil disimpan.");
                _view.CloseForm(true);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal menyimpan validasi: {ex.Message}", "Error Database");
            }
        }
    }
}
