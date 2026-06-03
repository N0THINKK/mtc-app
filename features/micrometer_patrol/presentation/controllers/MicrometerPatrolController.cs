using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mtc_app.features.micrometer_patrol.data.dtos;
using mtc_app.features.micrometer_patrol.data.repositories;
using mtc_app.shared.data.dtos;
using mtc_app.shared.data.repositories;
using mtc_app.shared.data.session;
using mtc_app.shared.data.utils;

namespace mtc_app.features.micrometer_patrol.presentation.controllers
{
    public class MicrometerPatrolController
    {
        private readonly IMicrometerPatrolView _view;
        private readonly IMicrometerPatrolRepository _repository;
        private readonly IMasterDataRepository _masterDataRepository;
        
        private List<CachedShiftDto> _shifts;
        private List<CachedMachineDto> _machines;
        private List<string> _operators;

        public MicrometerPatrolController(IMicrometerPatrolView view, IMicrometerPatrolRepository repository, IMasterDataRepository masterDataRepository)
        {
            _view = view;
            _repository = repository;
            _masterDataRepository = masterDataRepository;
        }

        public async Task LoadInitialDataAsync()
        {
            try
            {
                _shifts = await _masterDataRepository.GetShiftsAsync() ?? new List<CachedShiftDto>();
                if (_shifts.Count > 0)
                {
                    _view.PopulateShifts(_shifts.Select(s => s.ShiftName).ToArray());
                    _view.SelectedShift = _shifts[0].ShiftName;
                }

                if (UserSession.CurrentUser != null && UserSession.CurrentUser.RoleId != 1)
                {
                    _view.SetTechnicianMode(UserSession.CurrentUser.Username);
                }
                else
                {
                    _operators = await _masterDataRepository.GetOperatorsAsync() ?? new List<string>();
                    var nikList = new List<string>(_operators);

                    if (UserSession.CurrentUser != null)
                    {
                        string userValue = string.IsNullOrWhiteSpace(UserSession.CurrentUser.Nik) 
                            ? UserSession.CurrentUser.Username 
                            : UserSession.CurrentUser.Nik;

                        if (!nikList.Contains(userValue)) nikList.Insert(0, userValue);

                        _view.PopulateOperators(nikList.ToArray());
                        _view.SelectedNik = userValue;
                    }
                    else
                    {
                        _view.PopulateOperators(nikList.ToArray());
                        if (nikList.Count > 0) _view.SelectedNik = nikList[0];
                    }
                }

                _machines = await _masterDataRepository.GetMachinesAsync() ?? new List<CachedMachineDto>();
                if (_machines.Count > 0)
                {
                    _view.PopulateMachines(_machines.Select(m => m.Code).ToArray());

                    string configMachineId = DatabaseHelper.GetMachineId();
                    var matchedMachine = _machines.FirstOrDefault(m => m.MachineId.ToString() == configMachineId);
                    if (matchedMachine != null)
                    {
                        _view.SelectedMachine = matchedMachine.Code;
                        _view.LockMachine(true);
                    }
                    else
                    {
                        _view.SelectedMachine = _machines[0].Code;
                    }
                }
            }
            catch (Exception ex)
            {
                _view.ShowWarning("Gagal memuat data master: " + ex.Message);
            }
        }

        public async Task SavePatrolAsync()
        {
            var selectedShiftName = _view.SelectedShift;
            int shiftId = _shifts?.FirstOrDefault(s => s.ShiftName == selectedShiftName)?.ShiftId ?? 0;

            var selectedMachineCode = _view.SelectedMachine;
            int machineId = _machines?.FirstOrDefault(m => m.Code == selectedMachineCode)?.MachineId ?? 0;

            if (shiftId == 0 || machineId == 0)
            {
                _view.ShowWarning("Shift atau Mesin tidak valid!");
                return;
            }

            var patrolData = new MicrometerPatrolDto
            {
                PatrolDate = DateTime.Now,
                ShiftId = shiftId,
                UserId = (int)(UserSession.CurrentUser?.UserId ?? 0),
                MachineId = machineId,
                Point1 = _view.GetPointValue(0),
                Point2 = _view.GetPointValue(1),
                Point3 = _view.GetPointValue(2),
                Point4 = _view.GetPointValue(3),
                Point5 = _view.GetPointValue(4),
                Notes = _view.Notes
            };

            _view.SetBusyState(true);

            try
            {
                bool isSuccess = await _repository.SavePatrolAsync(patrolData);

                if (isSuccess)
                {
                    _view.ShowSuccess("Data patroli mikrometer berhasil disimpan!");
                    _view.CloseForm(true);
                }
                else
                {
                    _view.ShowError("Gagal menyimpan data.");
                }
            }
            catch (Exception ex)
            {
                _view.ShowError($"Error: {ex.Message}");
            }
            finally
            {
                _view.SetBusyState(false);
            }
        }
    }
}
