using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.authentication.data.repositories;
using mtc_app.shared.data.utils;

namespace mtc_app.features.authentication.presentation.controllers
{
    public class SetupController
    {
        private readonly ISetupView _view;
        private readonly ISetupRepository _repository;
        private Dictionary<string, int> _templateMap = new Dictionary<string, int>();

        public SetupController(ISetupView view, ISetupRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadDropdownDataAsync()
        {
            try
            {
                var types = await _repository.GetMachineTypesAsync();
                var areas = await _repository.GetMachineAreasAsync();
                _view.PopulateDropdowns(types.ToArray(), areas.ToArray());
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal memuat data: {ex.Message}", "Database Error");
            }
        }

        public void HandleMachineTypeChange(string typeName)
        {
            if (typeName.Contains("AC90") || typeName.Contains("AC95"))
            {
                _templateMap.Clear();
                var displayNames = new List<string>();
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        var templates = conn.Query(
                            @"SELECT t.template_id, t.template_name 
                              FROM checksheet_templates t
                              JOIN machine_types mt ON t.machine_type_id = mt.type_id
                              WHERE mt.type_name = @TypeName", new { TypeName = typeName }).ToList();
                        
                        foreach (var t in templates)
                        {
                            string displayName = t.template_name;
                            displayName = displayName.Replace("AC95 ", "").Replace(" AC95", "").Replace(" (AC95)", "")
                                                     .Replace("AC90 ", "").Replace(" AC90", "").Replace(" (AC90)", "").Trim();

                            displayNames.Add(displayName);
                            _templateMap[displayName] = (int)t.template_id;
                        }
                    }
                }
                catch { }

                _view.ShowTemplates(displayNames);
            }
            else
            {
                _templateMap.Clear();
                _view.HideTemplates();
            }
        }

        public async Task SaveConfigAsync()
        {
            string type = _view.MachineType;
            string area = _view.MachineArea;
            string number = _view.MachineNumber;

            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(area) || string.IsNullOrEmpty(number))
            {
                _view.ShowWarning("Mohon lengkapi Tipe, Area, dan Nomor Mesin!");
                return;
            }

            try
            {
                int machineId = await _repository.RegisterMachineAsync(type, area, number);

                using (var conn = DatabaseHelper.GetConnection())
                {
                    if (_view.IsTemplateVisible && !string.IsNullOrEmpty(_view.SelectedTemplateName))
                    {
                        if (_templateMap.TryGetValue(_view.SelectedTemplateName, out int templateId))
                        {
                            conn.Execute("UPDATE machines SET current_template_id = @TplId WHERE machine_id = @MacId", 
                                new { TplId = templateId, MacId = machineId });
                        }
                    }
                    else if (!type.Contains("AC90") && !type.Contains("AC95"))
                    {
                        var defaultTpl = conn.QueryFirstOrDefault<int?>(
                            @"SELECT template_id FROM checksheet_templates ct 
                              JOIN machine_types mt ON ct.machine_type_id = mt.type_id 
                              WHERE mt.type_name = @Type LIMIT 1", new { Type = type });
                        
                        if (defaultTpl.HasValue)
                        {
                            conn.Execute("UPDATE machines SET current_template_id = @TplId WHERE machine_id = @MacId", 
                                new { TplId = defaultTpl.Value, MacId = machineId });
                        }
                    }
                }

                DatabaseHelper.UpdateMachineConfig(machineId.ToString());

                try 
                {
                    string configFolder = @"C:\MTC_System\Config";
                    string configFile = Path.Combine(configFolder, "machine_id.txt");
                    if (!Directory.Exists(configFolder)) Directory.CreateDirectory(configFolder);
                    File.WriteAllText(configFile, machineId.ToString());
                }
                catch { }

                _view.ShowSuccess($"Setup Berhasil!\nIdentitas Mesin: {type}-{area}.{number}");
                _view.CloseForm(true);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Gagal menyimpan konfigurasi: {ex.Message}");
            }
        }
    }
}
