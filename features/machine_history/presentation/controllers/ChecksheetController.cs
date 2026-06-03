using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtc_app.shared.data.session;
using mtc_app.shared.data.utils;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.presentation.controllers
{
    public class ChecksheetController
    {
        private readonly IChecksheetView _view;
        private readonly bool _isTeknisiMode;
        
        public int CurrentMachineId { get; private set; }
        public int CurrentTemplateId { get; private set; }
        
        private List<int> _pendingNgItemIds = new List<int>();

        public ChecksheetController(IChecksheetView view, bool isTeknisiMode)
        {
            _view = view;
            _isTeknisiMode = isTeknisiMode;
        }

        public async Task LoadInitialDataAsync()
        {
            try
            {
                SetupPelaksanaInfo();

                using (var conn = DatabaseHelper.GetConnection())
                {
                    string machineIdStr = DatabaseHelper.GetMachineId();
                    if (!int.TryParse(machineIdStr, out int mId))
                    {
                        _view.ShowError("Terminal ini belum di-setup untuk mesin apapun.\nSilakan gunakan menu Setup terlebih dahulu.");
                        _view.CloseForm();
                        return;
                    }
                    CurrentMachineId = mId;

                    var machineInfo = await conn.QueryFirstOrDefaultAsync(
                        @"SELECT m.current_template_id, t.template_name, m.machine_number, mt.type_name 
                          FROM machines m 
                          LEFT JOIN checksheet_templates t ON m.current_template_id = t.template_id 
                          LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                          WHERE m.machine_id = @Id", new { Id = CurrentMachineId });

                    if (machineInfo == null || machineInfo.current_template_id == null)
                    {
                        _view.ShowWarning("SPV / Admin belum mengatur 'Template Checksheet' untuk mesin ini.\nSilakan atur di Master Data Mesin.");
                        _view.CloseForm();
                        return;
                    }

                    CurrentTemplateId = (int)machineInfo.current_template_id;
                    _view.SetMachineInfo($"No. Mesin: {machineInfo.type_name}.{machineInfo.machine_number} | Mode Pekerjaan: {machineInfo.template_name}");

                    string targetRole = _isTeknisiMode ? "Teknisi" : "Operator";
                    var items = (await conn.QueryAsync(
                        @"SELECT item_id, item_name, standard_judgment, check_method, input_type 
                          FROM checksheet_items 
                          WHERE template_id = @TplId AND role_target = @RoleTarget",
                          new { TplId = CurrentTemplateId, RoleTarget = targetRole })).ToList();

                    if (items.Count == 0)
                    {
                        _view.ShowEmptyState($"Belum ada pertanyaan checksheet khusus {targetRole} di template '{machineInfo.template_name}'.\nHubungi SPV untuk menambahkan pertanyaan di Master Data.");
                        return;
                    }

                    var pendingNgIds = await conn.QueryAsync<int>(@"
                        SELECT DISTINCT d.item_id
                        FROM patrol_logs l
                        JOIN patrol_log_details d ON l.log_id = d.log_id
                        WHERE l.machine_id = @Id 
                          AND d.status IN ('NOT_OK', 'NG', 'NG_CARRYOVER')
                    ", new { Id = CurrentMachineId });
                    
                    _pendingNgItemIds = pendingNgIds.ToList();

                    _view.ClearQuestions();
                    int number = 1;
                    foreach (var item in items)
                    {
                        string inputType = item.input_type != null ? item.input_type.ToString() : "options";
                        int currentItemId = (int)item.item_id;
                        bool isPending = _pendingNgItemIds.Contains(currentItemId);
                        
                        _view.AddQuestion(number, currentItemId, item.item_name, item.standard_judgment, item.check_method, inputType, isPending);
                        number++;
                    }
                }
            }
            catch (Exception ex)
            {
                _view.ShowError("Error memuat checksheet: " + ex.Message);
            }
        }

        private void SetupPelaksanaInfo()
        {
            string pelaksanaLabel = _isTeknisiMode ? "Teknisi" : "NIK Operator";
            string pelaksanaValue = UserSession.CurrentUser?.Username ?? "-";

            if (_isTeknisiMode)
            {
                string fullName = UserSession.CurrentUser?.FullName;
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    var words = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 1) pelaksanaValue = words[0];
                    else if (words.Length >= 2) pelaksanaValue = $"{words[0]} {words[words.Length - 1]}";

                    var textInfo = new System.Globalization.CultureInfo("id-ID", false).TextInfo;
                    pelaksanaValue = textInfo.ToTitleCase(pelaksanaValue.ToLower());
                }
            }
            _view.SetPelaksanaInfo(pelaksanaLabel, pelaksanaValue);
        }

        public async Task SaveChecksheetAsync()
        {
            string userNik = UserSession.CurrentUser?.Username ?? "-";
            if (string.IsNullOrWhiteSpace(userNik) || userNik == "-")
            {
                string warningMsg = _isTeknisiMode ? "Sesi Teknisi tidak valid!" : "Sesi Operator tidak valid!";
                _view.ShowWarning(warningMsg);
                return;
            }

            var answers = _view.GetAnswers();
            var firstUnanswered = answers.FirstOrDefault(c => !c.IsAnswered);
            
            if (firstUnanswered != null)
            {
                _view.FocusUnansweredQuestion();
                _view.ShowWarning("Masih ada pertanyaan yang belum dijawab!\nPastikan semua pertanyaan memiliki status OK, NOT OK, atau N/A.");
                return;
            }

            _view.SetBusyState(true);

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    try { await conn.ExecuteAsync("ALTER TABLE patrol_logs MODIFY shift VARCHAR(10);"); } catch { }
                    
                    string currentShift = _view.Shift;
                    string insertLogSql = "INSERT INTO patrol_logs (machine_id, user_nik, shift) VALUES (@MachId, @Nik, @Shift); SELECT LAST_INSERT_ID();";
                    int logId = await conn.QuerySingleAsync<int>(insertLogSql, new { MachId = CurrentMachineId, Nik = userNik, Shift = currentShift });

                    foreach (var item in answers)
                    {
                        string status = item.ValueString;
                        bool createTicket = false;

                        if (item.IsPendingNg && (status == "NG" || status == "NOT_OK"))
                        {
                            status = "PERBAIKAN_OK";
                        }

                        await conn.ExecuteAsync(
                            @"INSERT INTO patrol_log_details (log_id, item_id, status, action_note, is_ticket_created) 
                              VALUES (@LogId, @ItemId, @Status, @Note, @TicketCreated)",
                            new { LogId = logId, ItemId = item.ItemId, Status = status, Note = item.Notes, TicketCreated = createTicket }
                        );
                    }
                }

                _view.ShowSuccess("Hasil Patroli Harian berhasil disimpan!");
                _view.CloseForm();
            }
            catch (Exception ex)
            {
                _view.ShowError("Gagal menyimpan patroli: " + ex.Message);
            }
            finally
            {
                _view.SetBusyState(false);
            }
        }
    }
}
