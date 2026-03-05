using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.data.utils;

namespace mtc_app.features.admin.presentation.views
{
    public partial class MasterDataView : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        
        // Main Tab Control
        private TabControl tabControl;
        private TabPage tabUsers, tabMachines, tabParts, tabGeneralMasters;

        // User Tab Controls
        private DataGridView gridUsers;
        private AppInput txtUsername, txtPassword, txtFullName, txtNik, comboRole;
        private AppButton btnAddUser, btnUpdateUser, btnDeleteUser;
        private Dictionary<string, int> _roleNameToIdMap = new Dictionary<string, int>();
        private int? _selectedUserId = null;

        // Machine Tab Controls
        private TabControl tabMachineSub;
        private TabPage subMachineList, subMachineTypes, subMachineAreas, subChecksheetTemplates, subChecksheetItems;
        
        // Machine List (Aggregate)
        private DataGridView gridMachines;
        private AppInput txtMachineType, txtMachineArea, txtMachineNumber;
        private AppButton btnAddMachine, btnUpdateMachine, btnDeleteMachine;
        private int? _selectedMachineId = null;

        // Machine Types Master
        private DataGridView gridMasterTypes;
        private AppInput txtMasterTypeName;
        private AppButton btnAddMasterType, btnUpdateMasterType, btnDeleteMasterType;
        private int? _selectedMasterTypeId = null;

        // Machine Areas Master
        private DataGridView gridMasterAreas;
        private AppInput txtMasterAreaName;
        private AppButton btnAddMasterArea, btnUpdateMasterArea, btnDeleteMasterArea;
        private int? _selectedMasterAreaId = null;

        // Checksheet Templates Master
        private DataGridView gridTemplates;
        private AppInput txtTemplateName, cmbTemplateMachineType;
        private AppButton btnAddTemplate, btnUpdateTemplate, btnDeleteTemplate;
        private int? _selectedTemplateId = null;

        // [BARU] Checksheet Items Master
        private DataGridView gridChecksheetItems;
        private AppInput cmbItemTemplate, cmbItemRole, txtItemName, txtItemStandard, txtItemMethod;
        private AppButton btnAddItem, btnUpdateItem, btnDeleteItem;
        private int? _selectedItemId = null;
        private Dictionary<string, int> _templateNameToIdMap = new Dictionary<string, int>();

        // Part Tab Controls
        private DataGridView gridParts;
        private AppInput txtPartCode, txtPartName, txtPartStock;
        private AppButton btnAddPart, btnUpdatePart, btnDeletePart;
        private int? _selectedPartId = null;

        // General Masters Tab
        private TabControl tabGeneralSub;
        private TabPage subFailures, subCauses, subActions, subTypes;
        
        private DataGridView gridFailures, gridCauses, gridActions, gridTypes;
        private AppInput txtFailureName, txtCauseName, txtActionName, txtTypeName;
        private AppButton btnAddFailure, btnUpdateFailure, btnDeleteFailure;
        private AppButton btnAddCause, btnUpdateCause, btnDeleteCause;
        private AppButton btnAddAction, btnUpdateAction, btnDeleteAction;
        private AppButton btnAddType, btnUpdateType, btnDeleteType;
        
        private int? _selectedFailureId = null, _selectedCauseId = null, _selectedActionId = null, _selectedTypeId = null;


        public MasterDataView()
        {
            InitializeComponent();
            if (!this.DesignMode)
            {
                LoadRoles();
                LoadUsers();
                LoadMachines();
                LoadMasterMachineTypes();
                LoadMasterMachineAreas();
                LoadChecksheetTemplates(); 
                LoadChecksheetItems(); // Load Items on startup
                LoadParts();
                LoadFailures();
                LoadCauses();
                LoadActions();
                LoadProblemTypes();
            }
        }

        #region User Management
        private void LoadRoles()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    var roles = connection.Query("SELECT role_id, role_name FROM roles ORDER BY role_name").ToList();
                    _roleNameToIdMap.Clear();
                    var roleNames = new List<string>();
                    foreach (var role in roles)
                    {
                        roleNames.Add(role.role_name);
                        _roleNameToIdMap[role.role_name] = role.role_id;
                    }
                    comboRole.SetDropdownItems(roleNames.ToArray());
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal memuat role: {ex.Message}"); }
        }

        private void LoadUsers()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = "SELECT u.user_id, u.username, u.full_name, u.nik, r.role_name FROM users u JOIN roles r ON u.role_id = r.role_id ORDER BY u.user_id";
                    gridUsers.DataSource = connection.Query(sql).ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal memuat user: {ex.Message}"); }
            ClearUserSelection();
        }

        private void BtnAddUser_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.InputValue) || string.IsNullOrWhiteSpace(txtPassword.InputValue) || string.IsNullOrWhiteSpace(comboRole.InputValue))
            {
                MessageBox.Show("Username, Password, dan Role wajib diisi."); return;
            }
            if (!_roleNameToIdMap.TryGetValue(comboRole.InputValue, out int roleId)) return;

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = "INSERT INTO users (username, password, full_name, nik, role_id) VALUES (@Username, @Password, @FullName, @Nik, @RoleId)";
                    connection.Execute(sql, new { Username = txtUsername.InputValue, Password = txtPassword.InputValue, FullName = txtFullName.InputValue, Nik = txtNik.InputValue, RoleId = roleId });
                    AutoClosingMessageBox.Show("User berhasil ditambahkan!", "Sukses", 1500);
                    LoadUsers();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal menambah user: {ex.Message}"); }
        }

        private void BtnUpdateUser_Click(object sender, EventArgs e)
        {
            if (_selectedUserId == null) return;
            if (!_roleNameToIdMap.TryGetValue(comboRole.InputValue, out int roleId)) return;

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = "UPDATE users SET username = @Username, full_name = @FullName, nik = @Nik, role_id = @RoleId ";
                    if (!string.IsNullOrWhiteSpace(txtPassword.InputValue)) sql += ", password = @Password ";
                    sql += "WHERE user_id = @UserId";

                    connection.Execute(sql, new { Username = txtUsername.InputValue, FullName = txtFullName.InputValue, Nik = txtNik.InputValue, RoleId = roleId, Password = txtPassword.InputValue, UserId = _selectedUserId.Value });
                    AutoClosingMessageBox.Show("User berhasil diupdate!", "Sukses", 1500);
                    LoadUsers();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal update user: {ex.Message}"); }
        }
        
        private void BtnDeleteUser_Click(object sender, EventArgs e)
        {
            if (_selectedUserId == null) return;
            if (MessageBox.Show($"Hapus user '{txtUsername.InputValue}'?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Execute("DELETE FROM users WHERE user_id = @UserId", new { UserId = _selectedUserId.Value });
                        AutoClosingMessageBox.Show("User dihapus!", "Sukses", 1500);
                        LoadUsers();
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Gagal hapus user: {ex.Message}"); }
            }
        }

        private void GridUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = gridUsers.Rows[e.RowIndex];
                _selectedUserId = Convert.ToInt32(row.Cells["user_id"].Value);
                txtUsername.InputValue = row.Cells["username"].Value.ToString();
                txtFullName.InputValue = row.Cells["full_name"].Value?.ToString();
                txtNik.InputValue = row.Cells["nik"].Value?.ToString();
                comboRole.InputValue = row.Cells["role_name"].Value.ToString();
                txtPassword.InputValue = "";
                btnUpdateUser.Enabled = true;
                btnDeleteUser.Enabled = true;
            }
        }

        private void ClearUserSelection()
        {
            _selectedUserId = null;
            txtUsername.InputValue = txtPassword.InputValue = txtFullName.InputValue = txtNik.InputValue = comboRole.InputValue = "";
            btnUpdateUser.Enabled = btnDeleteUser.Enabled = false;
            gridUsers.ClearSelection();
        }
        #endregion

        #region Machine Management
        
        private void LoadMachines()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    gridMachines.DataSource = connection.Query(@"
                        SELECT m.machine_id, COALESCE(t.type_name, 'UNK') as machine_type, COALESCE(a.area_name, 'UNK') as machine_area, m.machine_number,
                        CONCAT(COALESCE(t.type_name, 'UNK'), '-', COALESCE(a.area_name, 'UNK'), '.', m.machine_number) AS machine_name
                        FROM machines m
                        LEFT JOIN machine_types t ON m.type_id = t.type_id
                        LEFT JOIN machine_areas a ON m.area_id = a.area_id
                        ORDER BY m.machine_id").ToList();
                    
                    RefreshMachineDropdowns(connection);
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal memuat mesin: {ex.Message}"); }
            ClearMachineSelection();
        }

        private void RefreshMachineDropdowns(IDbConnection conn)
        {
            try {
                var types = conn.Query<string>("SELECT type_name FROM machine_types ORDER BY type_name").ToArray();
                var areas = conn.Query<string>("SELECT area_name FROM machine_areas ORDER BY area_name").ToArray();
                txtMachineType.SetDropdownItems(types);
                cmbTemplateMachineType.SetDropdownItems(types); 
                txtMachineArea.SetDropdownItems(areas);
            } catch { }
        }

        private void BtnAddMachine_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMachineType.InputValue) || string.IsNullOrWhiteSpace(txtMachineArea.InputValue) || string.IsNullOrWhiteSpace(txtMachineNumber.InputValue))
                return;
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    int typeId = GetOrCreateLookupId(connection, "machine_types", "type_id", "type_name", txtMachineType.InputValue);
                    int areaId = GetOrCreateLookupId(connection, "machine_areas", "area_id", "area_name", txtMachineArea.InputValue);
                    connection.Execute("INSERT INTO machines (type_id, area_id, machine_number) VALUES (@TypeId, @AreaId, @Number)", new { TypeId = typeId, AreaId = areaId, Number = txtMachineNumber.InputValue });
                    AutoClosingMessageBox.Show("Mesin ditambahkan!", "Sukses", 1500);
                    LoadMachines();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal tambah mesin: {ex.Message}"); }
        }

        private void BtnUpdateMachine_Click(object sender, EventArgs e)
        {
            if (_selectedMachineId == null) return;
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    int typeId = GetOrCreateLookupId(connection, "machine_types", "type_id", "type_name", txtMachineType.InputValue);
                    int areaId = GetOrCreateLookupId(connection, "machine_areas", "area_id", "area_name", txtMachineArea.InputValue);
                    connection.Execute("UPDATE machines SET type_id = @TypeId, area_id = @AreaId, machine_number = @Number WHERE machine_id = @Id", 
                        new { TypeId = typeId, AreaId = areaId, Number = txtMachineNumber.InputValue, Id = _selectedMachineId.Value });
                    AutoClosingMessageBox.Show("Mesin diupdate!", "Sukses", 1500);
                    LoadMachines();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal update mesin: {ex.Message}"); }
        }

        private int GetOrCreateLookupId(IDbConnection conn, string tableName, string idCol, string nameCol, string value)
        {
            var id = conn.QueryFirstOrDefault<int?>($"SELECT {idCol} FROM {tableName} WHERE {nameCol} = @Value", new { Value = value });
            if (id.HasValue) return id.Value;
            return conn.QuerySingle<int>($"INSERT INTO {tableName} ({nameCol}) VALUES (@Value); SELECT LAST_INSERT_ID();", new { Value = value });
        }

        private void BtnDeleteMachine_Click(object sender, EventArgs e)
        {
            if (_selectedMachineId == null) return;
            if (MessageBox.Show("Hapus mesin ini?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Execute("DELETE FROM machines WHERE machine_id = @Id", new { Id = _selectedMachineId.Value });
                        AutoClosingMessageBox.Show("Mesin dihapus!", "Sukses", 1500);
                        LoadMachines();
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Gagal hapus mesin: {ex.Message}"); }
            }
        }

        private void GridMachines_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = gridMachines.Rows[e.RowIndex];
                _selectedMachineId = Convert.ToInt32(row.Cells["machine_id"].Value);
                txtMachineType.InputValue = row.Cells["machine_type"].Value?.ToString();
                txtMachineArea.InputValue = row.Cells["machine_area"].Value?.ToString();
                txtMachineNumber.InputValue = row.Cells["machine_number"].Value?.ToString();
                btnUpdateMachine.Enabled = btnDeleteMachine.Enabled = true;
            }
        }

        private void ClearMachineSelection()
        {
            _selectedMachineId = null;
            txtMachineType.InputValue = txtMachineArea.InputValue = txtMachineNumber.InputValue = "";
            btnUpdateMachine.Enabled = btnDeleteMachine.Enabled = false;
            gridMachines.ClearSelection();
        }

        // --- Master Types ---
        private void LoadMasterMachineTypes() { try { using (var c = DatabaseHelper.GetConnection()) gridMasterTypes.DataSource = c.Query("SELECT type_id, type_name FROM machine_types ORDER BY type_name").ToList(); } catch { } ClearMasterTypeSelection(); }
        private void BtnAddMasterType_Click(object sender, EventArgs e) { GenericAdd("machine_types", "type_name", txtMasterTypeName.InputValue, LoadMasterMachineTypes); }
        private void BtnUpdateMasterType_Click(object sender, EventArgs e) { GenericUpdate("machine_types", "type_name", "type_id", txtMasterTypeName.InputValue, _selectedMasterTypeId, LoadMasterMachineTypes); }
        private void BtnDeleteMasterType_Click(object sender, EventArgs e) { GenericDelete("machine_types", "type_id", _selectedMasterTypeId, LoadMasterMachineTypes); }
        private void GridMasterTypes_CellClick(object sender, DataGridViewCellEventArgs e) { if(e.RowIndex>=0) { _selectedMasterTypeId=(int)gridMasterTypes.Rows[e.RowIndex].Cells["type_id"].Value; txtMasterTypeName.InputValue=gridMasterTypes.Rows[e.RowIndex].Cells["type_name"].Value.ToString(); btnUpdateMasterType.Enabled=btnDeleteMasterType.Enabled=true; } }
        private void ClearMasterTypeSelection() { _selectedMasterTypeId=null; txtMasterTypeName.InputValue=""; btnUpdateMasterType.Enabled=btnDeleteMasterType.Enabled=false; }

        // --- Master Areas ---
        private void LoadMasterMachineAreas() { try { using (var c = DatabaseHelper.GetConnection()) gridMasterAreas.DataSource = c.Query("SELECT area_id, area_name FROM machine_areas ORDER BY area_name").ToList(); } catch { } ClearMasterAreaSelection(); }
        private void BtnAddMasterArea_Click(object sender, EventArgs e) { GenericAdd("machine_areas", "area_name", txtMasterAreaName.InputValue, LoadMasterMachineAreas); }
        private void BtnUpdateMasterArea_Click(object sender, EventArgs e) { GenericUpdate("machine_areas", "area_name", "area_id", txtMasterAreaName.InputValue, _selectedMasterAreaId, LoadMasterMachineAreas); }
        private void BtnDeleteMasterArea_Click(object sender, EventArgs e) { GenericDelete("machine_areas", "area_id", _selectedMasterAreaId, LoadMasterMachineAreas); }
        private void GridMasterAreas_CellClick(object sender, DataGridViewCellEventArgs e) { if(e.RowIndex>=0) { _selectedMasterAreaId=(int)gridMasterAreas.Rows[e.RowIndex].Cells["area_id"].Value; txtMasterAreaName.InputValue=gridMasterAreas.Rows[e.RowIndex].Cells["area_name"].Value.ToString(); btnUpdateMasterArea.Enabled=btnDeleteMasterArea.Enabled=true; } }
        private void ClearMasterAreaSelection() { _selectedMasterAreaId=null; txtMasterAreaName.InputValue=""; btnUpdateMasterArea.Enabled=btnDeleteMasterArea.Enabled=false; }

        // --- Master Checksheet Templates ---
        private void LoadChecksheetTemplates()
        {
            try { 
                using (var c = DatabaseHelper.GetConnection()) 
                {
                    gridTemplates.DataSource = c.Query(@"
                        SELECT t.template_id, t.template_name, mt.type_name as machine_type 
                        FROM checksheet_templates t 
                        JOIN machine_types mt ON t.machine_type_id = mt.type_id 
                        ORDER BY mt.type_name, t.template_name").ToList();
                } 
            } catch { }
            ClearTemplateSelection();
            RefreshTemplateDropdownForItem(); // Update data di tab pertanyaan
        }

        private void BtnAddTemplate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTemplateName.InputValue) || string.IsNullOrWhiteSpace(cmbTemplateMachineType.InputValue)) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    int typeId = GetOrCreateLookupId(conn, "machine_types", "type_id", "type_name", cmbTemplateMachineType.InputValue);
                    conn.Execute("INSERT INTO checksheet_templates (machine_type_id, template_name) VALUES (@TypeId, @TplName)", 
                                 new { TypeId = typeId, TplName = txtTemplateName.InputValue });
                    AutoClosingMessageBox.Show("Template ditambahkan!", "Sukses", 1000);
                    LoadChecksheetTemplates();
                }
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnUpdateTemplate_Click(object sender, EventArgs e)
        {
            if (_selectedTemplateId == null) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    int typeId = GetOrCreateLookupId(conn, "machine_types", "type_id", "type_name", cmbTemplateMachineType.InputValue);
                    conn.Execute("UPDATE checksheet_templates SET machine_type_id = @TypeId, template_name = @TplName WHERE template_id = @Id", 
                                 new { TypeId = typeId, TplName = txtTemplateName.InputValue, Id = _selectedTemplateId.Value });
                    AutoClosingMessageBox.Show("Template diupdate!", "Sukses", 1000);
                    LoadChecksheetTemplates();
                }
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnDeleteTemplate_Click(object sender, EventArgs e)
        {
            if (_selectedTemplateId == null) return;
            if (MessageBox.Show("Hapus template ini?", "Konfirmasi", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Execute("DELETE FROM checksheet_templates WHERE template_id = @Id", new { Id = _selectedTemplateId.Value });
                        AutoClosingMessageBox.Show("Template dihapus!", "Sukses", 1000);
                        LoadChecksheetTemplates();
                    }
                } catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void GridTemplates_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                _selectedTemplateId = (int)gridTemplates.Rows[e.RowIndex].Cells["template_id"].Value;
                txtTemplateName.InputValue = gridTemplates.Rows[e.RowIndex].Cells["template_name"].Value.ToString();
                cmbTemplateMachineType.InputValue = gridTemplates.Rows[e.RowIndex].Cells["machine_type"].Value.ToString();
                btnUpdateTemplate.Enabled = btnDeleteTemplate.Enabled = true;
            }
        }

        private void ClearTemplateSelection()
        {
            _selectedTemplateId = null;
            txtTemplateName.InputValue = "";
            btnUpdateTemplate.Enabled = btnDeleteTemplate.Enabled = false;
        }

        // --- [BARU] Master Checksheet ITEMS (Pertanyaan) ---
        private void RefreshTemplateDropdownForItem()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var templates = conn.Query(@"
                        SELECT t.template_id, CONCAT(mt.type_name, ' - ', t.template_name) as display_name 
                        FROM checksheet_templates t 
                        JOIN machine_types mt ON t.machine_type_id = mt.type_id
                        ORDER BY mt.type_name, t.template_name").ToList();
                    
                    _templateNameToIdMap.Clear();
                    var list = new List<string>();
                    foreach (var t in templates)
                    {
                        list.Add(t.display_name);
                        _templateNameToIdMap[t.display_name] = (int)t.template_id;
                    }
                    cmbItemTemplate.SetDropdownItems(list.ToArray());
                }
            }
            catch { }
        }

        private void LoadChecksheetItems()
        {
            try
            {
                using (var c = DatabaseHelper.GetConnection())
                {
                    gridChecksheetItems.DataSource = c.Query(@"
                        SELECT i.item_id, CONCAT(mt.type_name, ' - ', t.template_name) as template_display,
                               i.role_target, i.item_name, i.check_method, i.standard_judgment
                        FROM checksheet_items i
                        JOIN checksheet_templates t ON i.template_id = t.template_id
                        JOIN machine_types mt ON t.machine_type_id = mt.type_id
                        ORDER BY mt.type_name, t.template_name, i.role_target, i.item_id").ToList();
                }
            }
            catch { }
            ClearItemSelection();
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtItemName.InputValue) || string.IsNullOrWhiteSpace(cmbItemTemplate.InputValue)) return;
            if (!_templateNameToIdMap.TryGetValue(cmbItemTemplate.InputValue, out int tplId)) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Execute(@"INSERT INTO checksheet_items (template_id, role_target, item_name, standard_judgment, check_method) 
                                   VALUES (@TId, @Role, @Name, @Std, @Method)", 
                                 new { 
                                     TId = tplId, 
                                     Role = cmbItemRole.InputValue, 
                                     Name = txtItemName.InputValue, 
                                     Std = txtItemStandard.InputValue, 
                                     Method = txtItemMethod.InputValue 
                                 });
                    AutoClosingMessageBox.Show("Pertanyaan ditambahkan!", "Sukses", 1000);
                    LoadChecksheetItems();
                }
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnUpdateItem_Click(object sender, EventArgs e)
        {
            if (_selectedItemId == null || string.IsNullOrWhiteSpace(txtItemName.InputValue)) return;
            if (!_templateNameToIdMap.TryGetValue(cmbItemTemplate.InputValue, out int tplId)) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Execute(@"UPDATE checksheet_items 
                                   SET template_id = @TId, role_target = @Role, item_name = @Name, standard_judgment = @Std, check_method = @Method 
                                   WHERE item_id = @Id", 
                                 new { 
                                     TId = tplId, 
                                     Role = cmbItemRole.InputValue, 
                                     Name = txtItemName.InputValue, 
                                     Std = txtItemStandard.InputValue, 
                                     Method = txtItemMethod.InputValue,
                                     Id = _selectedItemId.Value 
                                 });
                    AutoClosingMessageBox.Show("Pertanyaan diupdate!", "Sukses", 1000);
                    LoadChecksheetItems();
                }
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnDeleteItem_Click(object sender, EventArgs e)
        {
            if (_selectedItemId == null) return;
            if (MessageBox.Show("Hapus pertanyaan ini?", "Konfirmasi", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Execute("DELETE FROM checksheet_items WHERE item_id = @Id", new { Id = _selectedItemId.Value });
                        AutoClosingMessageBox.Show("Pertanyaan dihapus!", "Sukses", 1000);
                        LoadChecksheetItems();
                    }
                } catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void GridChecksheetItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = gridChecksheetItems.Rows[e.RowIndex];
                _selectedItemId = (int)row.Cells["item_id"].Value;
                cmbItemTemplate.InputValue = row.Cells["template_display"].Value?.ToString();
                cmbItemRole.InputValue = row.Cells["role_target"].Value?.ToString();
                txtItemName.InputValue = row.Cells["item_name"].Value?.ToString();
                txtItemMethod.InputValue = row.Cells["check_method"].Value?.ToString();
                txtItemStandard.InputValue = row.Cells["standard_judgment"].Value?.ToString();
                
                btnUpdateItem.Enabled = btnDeleteItem.Enabled = true;
            }
        }

        private void ClearItemSelection()
        {
            _selectedItemId = null;
            txtItemName.InputValue = txtItemStandard.InputValue = txtItemMethod.InputValue = "";
            btnUpdateItem.Enabled = btnDeleteItem.Enabled = false;
        }

        #endregion

        #region Part Management
        private void LoadParts() { try { using (var c = DatabaseHelper.GetConnection()) gridParts.DataSource = c.Query("SELECT part_id, part_code, part_name, stock_qty FROM parts ORDER BY part_name").ToList(); } catch { } ClearPartSelection(); }
        private void BtnAddPart_Click(object sender, EventArgs e) { GenericAddPart(); }
        private void BtnUpdatePart_Click(object sender, EventArgs e) { GenericUpdatePart(); }
        private void BtnDeletePart_Click(object sender, EventArgs e) { GenericDelete("parts", "part_id", _selectedPartId, LoadParts); }
        private void GridParts_CellClick(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) { _selectedPartId = (int)gridParts.Rows[e.RowIndex].Cells["part_id"].Value; txtPartCode.InputValue = gridParts.Rows[e.RowIndex].Cells["part_code"].Value?.ToString(); txtPartName.InputValue = gridParts.Rows[e.RowIndex].Cells["part_name"].Value?.ToString(); txtPartStock.InputValue = gridParts.Rows[e.RowIndex].Cells["stock_qty"].Value?.ToString(); btnUpdatePart.Enabled = btnDeletePart.Enabled = true; } }
        private void ClearPartSelection() { _selectedPartId = null; txtPartCode.InputValue = txtPartName.InputValue = txtPartStock.InputValue = ""; btnUpdatePart.Enabled = btnDeletePart.Enabled = false; }
        private void GenericAddPart() { try { using(var c=DatabaseHelper.GetConnection()){ c.Execute("INSERT INTO parts (part_code, part_name, stock_qty) VALUES (@C, @N, @S)", new{C=txtPartCode.InputValue, N=txtPartName.InputValue, S=int.TryParse(txtPartStock.InputValue, out int s)?s:0}); AutoClosingMessageBox.Show("Part ditambah!","Sukses",1000); LoadParts(); } } catch(Exception ex){MessageBox.Show(ex.Message);} }
        private void GenericUpdatePart() { if(_selectedPartId==null)return; try { using(var c=DatabaseHelper.GetConnection()){ c.Execute("UPDATE parts SET part_code=@C, part_name=@N, stock_qty=@S WHERE part_id=@ID", new{C=txtPartCode.InputValue, N=txtPartName.InputValue, S=int.TryParse(txtPartStock.InputValue, out int s)?s:0, ID=_selectedPartId}); AutoClosingMessageBox.Show("Part diupdate!","Sukses",1000); LoadParts(); } } catch(Exception ex){MessageBox.Show(ex.Message);} }
        #endregion

        #region General Masters
        private void LoadFailures() { try { using (var c = DatabaseHelper.GetConnection()) gridFailures.DataSource = c.Query("SELECT failure_id, failure_name FROM failures ORDER BY failure_name").ToList(); } catch { } ClearFailureSelection(); }
        private void BtnAddFailure_Click(object sender, EventArgs e) { GenericAdd("failures", "failure_name", txtFailureName.InputValue, LoadFailures); }
        private void BtnUpdateFailure_Click(object sender, EventArgs e) { GenericUpdate("failures", "failure_name", "failure_id", txtFailureName.InputValue, _selectedFailureId, LoadFailures); }
        private void BtnDeleteFailure_Click(object sender, EventArgs e) { GenericDelete("failures", "failure_id", _selectedFailureId, LoadFailures); }
        private void GridFailures_CellClick(object sender, DataGridViewCellEventArgs e) { if(e.RowIndex>=0) { _selectedFailureId = (int)gridFailures.Rows[e.RowIndex].Cells["failure_id"].Value; txtFailureName.InputValue = gridFailures.Rows[e.RowIndex].Cells["failure_name"].Value.ToString(); btnUpdateFailure.Enabled=btnDeleteFailure.Enabled=true; } }
        private void ClearFailureSelection() { _selectedFailureId = null; txtFailureName.InputValue = ""; btnUpdateFailure.Enabled = btnDeleteFailure.Enabled = false; }

        private void LoadCauses() { try { using (var c = DatabaseHelper.GetConnection()) gridCauses.DataSource = c.Query("SELECT cause_id, cause_name FROM failure_causes ORDER BY cause_name").ToList(); } catch { } ClearCauseSelection(); }
        private void BtnAddCause_Click(object sender, EventArgs e) { GenericAdd("failure_causes", "cause_name", txtCauseName.InputValue, LoadCauses); }
        private void BtnUpdateCause_Click(object sender, EventArgs e) { GenericUpdate("failure_causes", "cause_name", "cause_id", txtCauseName.InputValue, _selectedCauseId, LoadCauses); }
        private void BtnDeleteCause_Click(object sender, EventArgs e) { GenericDelete("failure_causes", "cause_id", _selectedCauseId, LoadCauses); }
        private void GridCauses_CellClick(object sender, DataGridViewCellEventArgs e) { if(e.RowIndex>=0) { _selectedCauseId = (int)gridCauses.Rows[e.RowIndex].Cells["cause_id"].Value; txtCauseName.InputValue = gridCauses.Rows[e.RowIndex].Cells["cause_name"].Value.ToString(); btnUpdateCause.Enabled=btnDeleteCause.Enabled=true; } }
        private void ClearCauseSelection() { _selectedCauseId = null; txtCauseName.InputValue = ""; btnUpdateCause.Enabled = btnDeleteCause.Enabled = false; }

        private void LoadActions() { try { using (var c = DatabaseHelper.GetConnection()) gridActions.DataSource = c.Query("SELECT action_id, action_name FROM actions ORDER BY action_name").ToList(); } catch { } ClearActionSelection(); }
        private void BtnAddAction_Click(object sender, EventArgs e) { GenericAdd("actions", "action_name", txtActionName.InputValue, LoadActions); }
        private void BtnUpdateAction_Click(object sender, EventArgs e) { GenericUpdate("actions", "action_name", "action_id", txtActionName.InputValue, _selectedActionId, LoadActions); }
        private void BtnDeleteAction_Click(object sender, EventArgs e) { GenericDelete("actions", "action_id", _selectedActionId, LoadActions); }
        private void GridActions_CellClick(object sender, DataGridViewCellEventArgs e) { if(e.RowIndex>=0) { _selectedActionId = (int)gridActions.Rows[e.RowIndex].Cells["action_id"].Value; txtActionName.InputValue = gridActions.Rows[e.RowIndex].Cells["action_name"].Value.ToString(); btnUpdateAction.Enabled=btnDeleteAction.Enabled=true; } }
        private void ClearActionSelection() { _selectedActionId = null; txtActionName.InputValue = ""; btnUpdateAction.Enabled = btnDeleteAction.Enabled = false; }

        private void LoadProblemTypes() { try { using (var c = DatabaseHelper.GetConnection()) gridTypes.DataSource = c.Query("SELECT type_id, type_name FROM problem_types ORDER BY type_name").ToList(); } catch { } ClearTypeSelection(); }
        private void BtnAddType_Click(object sender, EventArgs e) { GenericAdd("problem_types", "type_name", txtTypeName.InputValue, LoadProblemTypes); }
        private void BtnUpdateType_Click(object sender, EventArgs e) { GenericUpdate("problem_types", "type_name", "type_id", txtTypeName.InputValue, _selectedTypeId, LoadProblemTypes); }
        private void BtnDeleteType_Click(object sender, EventArgs e) { GenericDelete("problem_types", "type_id", _selectedTypeId, LoadProblemTypes); }
        private void GridTypes_CellClick(object sender, DataGridViewCellEventArgs e) { if(e.RowIndex>=0) { _selectedTypeId = (int)gridTypes.Rows[e.RowIndex].Cells["type_id"].Value; txtTypeName.InputValue = gridTypes.Rows[e.RowIndex].Cells["type_name"].Value.ToString(); btnUpdateType.Enabled=btnDeleteType.Enabled=true; } }
        private void ClearTypeSelection() { _selectedTypeId = null; txtTypeName.InputValue = ""; btnUpdateType.Enabled = btnDeleteType.Enabled = false; }

        private void GenericAdd(string table, string col, string val, Action reload) { if(string.IsNullOrWhiteSpace(val)) return; try { using(var c=DatabaseHelper.GetConnection()) { c.Execute($"INSERT INTO {table} ({col}) VALUES (@V)", new{V=val}); AutoClosingMessageBox.Show("Data disimpan!","Sukses",1000); reload(); } } catch(Exception ex){ MessageBox.Show(ex.Message); } }
        private void GenericUpdate(string table, string col, string idCol, string val, int? id, Action reload) { if(id==null || string.IsNullOrWhiteSpace(val)) return; try { using(var c=DatabaseHelper.GetConnection()) { c.Execute($"UPDATE {table} SET {col}=@V WHERE {idCol}=@ID", new{V=val, ID=id}); AutoClosingMessageBox.Show("Data diupdate!","Sukses",1000); reload(); } } catch(Exception ex){ MessageBox.Show(ex.Message); } }
        private void GenericDelete(string table, string idCol, int? id, Action reload) { if(id==null) return; if(MessageBox.Show("Yakin hapus?","Confirm",MessageBoxButtons.YesNo)==DialogResult.Yes) try { using(var c=DatabaseHelper.GetConnection()) { c.Execute($"DELETE FROM {table} WHERE {idCol}=@ID", new{ID=id}); AutoClosingMessageBox.Show("Data dihapus!","Sukses",1000); reload(); } } catch(Exception ex){ MessageBox.Show(ex.Message); } }
        #endregion

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabControl = new TabControl { Dock = DockStyle.Fill, Font = AppFonts.BodySmall };
            this.tabUsers = new TabPage("Manajemen User");
            this.tabMachines = new TabPage("Manajemen Mesin");
            this.tabParts = new TabPage("Manajemen Sparepart");
            this.tabGeneralMasters = new TabPage("Master Lainnya");
            
            this.Dock = DockStyle.Fill;
            this.tabControl.Controls.AddRange(new Control[] { this.tabUsers, this.tabMachines, this.tabParts, this.tabGeneralMasters });
            this.Controls.Add(tabControl);

            BuildUserTab();
            BuildMachineTab();
            BuildPartTab();
            BuildGeneralMastersTab();
        }

        private void BuildUserTab()
        {
            var pnlForm = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = true };
            this.txtUsername = new AppInput { LabelText = "Username", Width = 150 };
            this.txtPassword = new AppInput { LabelText = "Password (kosongi jk sama)", Width = 200 };
            this.txtFullName = new AppInput { LabelText = "Nama Lengkap", Width = 200 };
            this.txtNik = new AppInput { LabelText = "NIK / Inisial", Width = 100 };
            this.comboRole = new AppInput { LabelText = "Role", InputType = AppInput.InputTypeEnum.Dropdown, Width = 150 };
            this.btnAddUser = new AppButton { Text = "Tambah", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5) };
            this.btnUpdateUser = new AppButton { Text = "Update", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5), Enabled = false };
            this.btnDeleteUser = new AppButton { Text = "Hapus", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5), Enabled = false, Type = AppButton.ButtonType.Danger };
            btnAddUser.Click += BtnAddUser_Click; btnUpdateUser.Click += BtnUpdateUser_Click; btnDeleteUser.Click += BtnDeleteUser_Click;
            flow.Controls.AddRange(new Control[] { txtUsername, txtPassword, txtFullName, txtNik, comboRole, btnAddUser, btnUpdateUser, btnDeleteUser });
            pnlForm.Controls.Add(flow);
            
            this.gridUsers = CreateGrid();
            this.gridUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "user_id", DataPropertyName = "user_id", Visible = false });
            this.gridUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "username", HeaderText = "Username", DataPropertyName = "username" });
            this.gridUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "full_name", HeaderText = "Nama Lengkap", DataPropertyName = "full_name" });
            this.gridUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "nik", HeaderText = "NIK", DataPropertyName = "nik" });
            this.gridUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "role_name", HeaderText = "Role", DataPropertyName = "role_name" });
            this.gridUsers.CellClick += GridUsers_CellClick;
            
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) }; pnlGrid.Controls.Add(gridUsers);
            this.tabUsers.Controls.AddRange(new Control[] { pnlGrid, pnlForm });
        }

        private void BuildMachineTab()
        {
            this.tabMachineSub = new TabControl { Dock = DockStyle.Fill };

            // 1. DAFTAR MESIN
            this.subMachineList = new TabPage("Daftar Mesin");
            var pnlForm = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            this.txtMachineType = new AppInput { LabelText = "Tipe Mesin", InputType = AppInput.InputTypeEnum.Dropdown, Width = 150, AllowCustomText = true }; 
            this.txtMachineArea = new AppInput { LabelText = "Area", InputType = AppInput.InputTypeEnum.Dropdown, Width = 100, AllowCustomText = true }; 
            this.txtMachineNumber = new AppInput { LabelText = "No. Mesin", Width = 100 };
            this.btnAddMachine = new AppButton { Text = "Tambah", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5) };
            this.btnUpdateMachine = new AppButton { Text = "Update", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5), Enabled = false };
            this.btnDeleteMachine = new AppButton { Text = "Hapus", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5), Enabled = false, Type = AppButton.ButtonType.Danger };
            btnAddMachine.Click += BtnAddMachine_Click; btnUpdateMachine.Click += BtnUpdateMachine_Click; btnDeleteMachine.Click += BtnDeleteMachine_Click;
            flow.Controls.AddRange(new Control[] { txtMachineType, txtMachineArea, txtMachineNumber, btnAddMachine, btnUpdateMachine, btnDeleteMachine });
            pnlForm.Controls.Add(flow);
            
            this.gridMachines = CreateGrid();
            this.gridMachines.Columns.Add(new DataGridViewTextBoxColumn { Name = "machine_id", DataPropertyName = "machine_id", Visible = false });
            this.gridMachines.Columns.Add(new DataGridViewTextBoxColumn { Name = "machine_name", HeaderText = "Nama Mesin", DataPropertyName = "machine_name" });
            this.gridMachines.Columns.Add(new DataGridViewTextBoxColumn { Name = "machine_type", HeaderText = "Tipe", DataPropertyName = "machine_type" });
            this.gridMachines.Columns.Add(new DataGridViewTextBoxColumn { Name = "machine_area", HeaderText = "Area", DataPropertyName = "machine_area" });
            this.gridMachines.Columns.Add(new DataGridViewTextBoxColumn { Name = "machine_number", HeaderText = "No.", DataPropertyName = "machine_number" });
            this.gridMachines.CellClick += GridMachines_CellClick;
            
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) }; pnlGrid.Controls.Add(gridMachines);
            this.subMachineList.Controls.AddRange(new Control[] { pnlGrid, pnlForm });

            // 2. MASTER TIPE
            this.subMachineTypes = new TabPage("Master Tipe Mesin");
            var pnlType = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowType = new FlowLayoutPanel { Dock = DockStyle.Fill };
            this.txtMasterTypeName = new AppInput { LabelText = "Nama Tipe", Width = 250 };
            this.btnAddMasterType = new AppButton { Text = "Tambah", Width = 90, Margin = new Padding(5,35,5,5) };
            this.btnUpdateMasterType = new AppButton { Text = "Update", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false };
            this.btnDeleteMasterType = new AppButton { Text = "Hapus", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false, Type=AppButton.ButtonType.Danger };
            btnAddMasterType.Click += BtnAddMasterType_Click; btnUpdateMasterType.Click += BtnUpdateMasterType_Click; btnDeleteMasterType.Click += BtnDeleteMasterType_Click;
            flowType.Controls.AddRange(new Control[] { txtMasterTypeName, btnAddMasterType, btnUpdateMasterType, btnDeleteMasterType });
            pnlType.Controls.Add(flowType);
            this.gridMasterTypes = CreateGrid();
            this.gridMasterTypes.Columns.Add(new DataGridViewTextBoxColumn { Name="type_id", DataPropertyName="type_id", Visible=false });
            this.gridMasterTypes.Columns.Add(new DataGridViewTextBoxColumn { Name="type_name", DataPropertyName="type_name", HeaderText="Tipe Mesin" });
            this.gridMasterTypes.CellClick += GridMasterTypes_CellClick;
            this.subMachineTypes.Controls.Add(gridMasterTypes); this.subMachineTypes.Controls.Add(pnlType);

            // 3. MASTER AREA
            this.subMachineAreas = new TabPage("Master Area Mesin");
            var pnlArea = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowArea = new FlowLayoutPanel { Dock = DockStyle.Fill };
            this.txtMasterAreaName = new AppInput { LabelText = "Nama Area", Width = 250 };
            this.btnAddMasterArea = new AppButton { Text = "Tambah", Width = 90, Margin = new Padding(5,35,5,5) };
            this.btnUpdateMasterArea = new AppButton { Text = "Update", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false };
            this.btnDeleteMasterArea = new AppButton { Text = "Hapus", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false, Type=AppButton.ButtonType.Danger };
            btnAddMasterArea.Click += BtnAddMasterArea_Click; btnUpdateMasterArea.Click += BtnUpdateMasterArea_Click; btnDeleteMasterArea.Click += BtnDeleteMasterArea_Click;
            flowArea.Controls.AddRange(new Control[] { txtMasterAreaName, btnAddMasterArea, btnUpdateMasterArea, btnDeleteMasterArea });
            pnlArea.Controls.Add(flowArea);
            this.gridMasterAreas = CreateGrid();
            this.gridMasterAreas.Columns.Add(new DataGridViewTextBoxColumn { Name="area_id", DataPropertyName="area_id", Visible=false });
            this.gridMasterAreas.Columns.Add(new DataGridViewTextBoxColumn { Name="area_name", DataPropertyName="area_name", HeaderText="Area Mesin" });
            this.gridMasterAreas.CellClick += GridMasterAreas_CellClick;
            this.subMachineAreas.Controls.Add(gridMasterAreas); this.subMachineAreas.Controls.Add(pnlArea);

            // 4. MASTER TEMPLATE CHECKSHEET
            this.subChecksheetTemplates = new TabPage("Master Template Checksheet");
            var pnlTpl = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowTpl = new FlowLayoutPanel { Dock = DockStyle.Fill };
            this.cmbTemplateMachineType = new AppInput { LabelText = "Untuk Tipe Mesin", InputType = AppInput.InputTypeEnum.Dropdown, Width = 150, AllowCustomText = false };
            this.txtTemplateName = new AppInput { LabelText = "Nama Template (Cth: Type A)", Width = 250 };
            this.btnAddTemplate = new AppButton { Text = "Tambah", Width = 90, Margin = new Padding(5,35,5,5) };
            this.btnUpdateTemplate = new AppButton { Text = "Update", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false };
            this.btnDeleteTemplate = new AppButton { Text = "Hapus", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false, Type=AppButton.ButtonType.Danger };
            btnAddTemplate.Click += BtnAddTemplate_Click; btnUpdateTemplate.Click += BtnUpdateTemplate_Click; btnDeleteTemplate.Click += BtnDeleteTemplate_Click;
            flowTpl.Controls.AddRange(new Control[] { cmbTemplateMachineType, txtTemplateName, btnAddTemplate, btnUpdateTemplate, btnDeleteTemplate });
            pnlTpl.Controls.Add(flowTpl);
            this.gridTemplates = CreateGrid();
            this.gridTemplates.Columns.Add(new DataGridViewTextBoxColumn { Name="template_id", DataPropertyName="template_id", Visible=false });
            this.gridTemplates.Columns.Add(new DataGridViewTextBoxColumn { Name="machine_type", DataPropertyName="machine_type", HeaderText="Tipe Mesin" });
            this.gridTemplates.Columns.Add(new DataGridViewTextBoxColumn { Name="template_name", DataPropertyName="template_name", HeaderText="Nama Template" });
            this.gridTemplates.CellClick += GridTemplates_CellClick;
            this.subChecksheetTemplates.Controls.Add(gridTemplates); this.subChecksheetTemplates.Controls.Add(pnlTpl);

            // 5. [BARU] MASTER PERTANYAAN CHECKSHEET
            this.subChecksheetItems = new TabPage("Master Pertanyaan Checksheet");
            
            // --- Panel Atas (Form Input) ---
            var pnlItemForm = new Panel { Dock = DockStyle.Top, Height = 180, Padding = new Padding(10) };
            var flowItem1 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80, WrapContents = false };
            var flowItem2 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80, WrapContents = false };

            this.cmbItemTemplate = new AppInput { LabelText = "Pilih Template Pekerjaan", InputType = AppInput.InputTypeEnum.Dropdown, Width = 300, AllowCustomText = false };
            this.cmbItemRole = new AppInput { LabelText = "Target Role", InputType = AppInput.InputTypeEnum.Dropdown, Width = 150, AllowCustomText = false };
            this.cmbItemRole.SetDropdownItems(new string[] { "Operator", "Teknisi" });
            this.txtItemName = new AppInput { LabelText = "Poin Inspeksi (Contoh: Tekanan Udara)", Width = 350 };
            
            this.txtItemMethod = new AppInput { LabelText = "Metode (Cth: Visual)", Width = 150 };
            this.txtItemStandard = new AppInput { LabelText = "Standar OK (Cth: 0.45 MPa)", Width = 300 };

            this.btnAddItem = new AppButton { Text = "Tambah", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5) };
            this.btnUpdateItem = new AppButton { Text = "Update", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5), Enabled = false };
            this.btnDeleteItem = new AppButton { Text = "Hapus", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5), Enabled = false, Type = AppButton.ButtonType.Danger };
            
            btnAddItem.Click += BtnAddItem_Click; 
            btnUpdateItem.Click += BtnUpdateItem_Click; 
            btnDeleteItem.Click += BtnDeleteItem_Click;

            // Baris 1
            flowItem1.Controls.AddRange(new Control[] { cmbItemTemplate, cmbItemRole, txtItemName });
            // Baris 2
            flowItem2.Controls.AddRange(new Control[] { txtItemMethod, txtItemStandard, btnAddItem, btnUpdateItem, btnDeleteItem });
            
            pnlItemForm.Controls.Add(flowItem2);
            pnlItemForm.Controls.Add(flowItem1);

            // --- Panel Bawah (Grid Data) ---
            this.gridChecksheetItems = CreateGrid();
            this.gridChecksheetItems.Columns.Add(new DataGridViewTextBoxColumn { Name="item_id", DataPropertyName="item_id", Visible=false });
            this.gridChecksheetItems.Columns.Add(new DataGridViewTextBoxColumn { Name="template_display", DataPropertyName="template_display", HeaderText="Template Pekerjaan", FillWeight = 20 });
            this.gridChecksheetItems.Columns.Add(new DataGridViewTextBoxColumn { Name="role_target", DataPropertyName="role_target", HeaderText="Role", FillWeight = 10 });
            this.gridChecksheetItems.Columns.Add(new DataGridViewTextBoxColumn { Name="item_name", DataPropertyName="item_name", HeaderText="Poin Inspeksi", FillWeight = 30 });
            this.gridChecksheetItems.Columns.Add(new DataGridViewTextBoxColumn { Name="check_method", DataPropertyName="check_method", HeaderText="Metode", FillWeight = 15 });
            this.gridChecksheetItems.Columns.Add(new DataGridViewTextBoxColumn { Name="standard_judgment", DataPropertyName="standard_judgment", HeaderText="Standar OK", FillWeight = 25 });
            this.gridChecksheetItems.CellClick += GridChecksheetItems_CellClick;
            
            var pnlItemGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) }; 
            pnlItemGrid.Controls.Add(gridChecksheetItems);
            
            this.subChecksheetItems.Controls.AddRange(new Control[] { pnlItemGrid, pnlItemForm });

            // Add Sub Tabs
            this.tabMachineSub.Controls.AddRange(new Control[] { subMachineList, subMachineTypes, subMachineAreas, subChecksheetTemplates, subChecksheetItems });
            this.tabMachines.Controls.Add(tabMachineSub);
        }

        private void BuildPartTab()
        {
            var pnlForm = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            this.txtPartCode = new AppInput { LabelText = "Kode Part", Width = 150 };
            this.txtPartName = new AppInput { LabelText = "Nama Part", Width = 300 };
            this.txtPartStock = new AppInput { LabelText = "Stok", Width = 100, InputType = AppInput.InputTypeEnum.Text };
            this.btnAddPart = new AppButton { Text = "Tambah", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5) };
            this.btnUpdatePart = new AppButton { Text = "Update", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5), Enabled = false };
            this.btnDeletePart = new AppButton { Text = "Hapus", Width = 90, Height = 35, Margin = new Padding(5, 35, 5, 5), Enabled = false, Type = AppButton.ButtonType.Danger };
            btnAddPart.Click += BtnAddPart_Click; btnUpdatePart.Click += BtnUpdatePart_Click; btnDeletePart.Click += BtnDeletePart_Click;
            flow.Controls.AddRange(new Control[] { txtPartCode, txtPartName, txtPartStock, btnAddPart, btnUpdatePart, btnDeletePart });
            pnlForm.Controls.Add(flow);
            
            this.gridParts = CreateGrid();
            this.gridParts.Columns.Add(new DataGridViewTextBoxColumn { Name = "part_id", DataPropertyName = "part_id", Visible = false });
            this.gridParts.Columns.Add(new DataGridViewTextBoxColumn { Name = "part_code", HeaderText = "Kode Part", DataPropertyName = "part_code" });
            this.gridParts.Columns.Add(new DataGridViewTextBoxColumn { Name = "part_name", HeaderText = "Nama Part", DataPropertyName = "part_name" });
            this.gridParts.Columns.Add(new DataGridViewTextBoxColumn { Name = "stock_qty", HeaderText = "Stok", DataPropertyName = "stock_qty" });
            this.gridParts.CellClick += GridParts_CellClick;
            
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) }; pnlGrid.Controls.Add(gridParts);
            this.tabParts.Controls.AddRange(new Control[] { pnlGrid, pnlForm });
        }

        private void BuildGeneralMastersTab()
        {
            this.tabGeneralSub = new TabControl { Dock = DockStyle.Fill };
            
            this.subFailures = new TabPage("Jenis Kerusakan");
            var pnlFail = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowFail = new FlowLayoutPanel { Dock = DockStyle.Fill };
            this.txtFailureName = new AppInput { LabelText = "Nama Kerusakan", Width = 400 };
            this.btnAddFailure = new AppButton { Text = "Tambah", Width = 90, Margin = new Padding(5,35,5,5) };
            this.btnUpdateFailure = new AppButton { Text = "Update", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false };
            this.btnDeleteFailure = new AppButton { Text = "Hapus", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false, Type=AppButton.ButtonType.Danger };
            btnAddFailure.Click += BtnAddFailure_Click; btnUpdateFailure.Click += BtnUpdateFailure_Click; btnDeleteFailure.Click += BtnDeleteFailure_Click;
            flowFail.Controls.AddRange(new Control[]{ txtFailureName, btnAddFailure, btnUpdateFailure, btnDeleteFailure });
            pnlFail.Controls.Add(flowFail);
            this.gridFailures = CreateGrid();
            this.gridFailures.Columns.Add(new DataGridViewTextBoxColumn { Name="failure_id", DataPropertyName="failure_id", Visible=false });
            this.gridFailures.Columns.Add(new DataGridViewTextBoxColumn { Name="failure_name", DataPropertyName="failure_name", HeaderText="Nama Kerusakan" });
            this.gridFailures.CellClick += GridFailures_CellClick;
            this.subFailures.Controls.Add(gridFailures); this.subFailures.Controls.Add(pnlFail);

            this.subCauses = new TabPage("Penyebab Problem");
            var pnlCause = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowCause = new FlowLayoutPanel { Dock = DockStyle.Fill };
            this.txtCauseName = new AppInput { LabelText = "Nama Penyebab", Width = 400 };
            this.btnAddCause = new AppButton { Text = "Tambah", Width = 90, Margin = new Padding(5,35,5,5) };
            this.btnUpdateCause = new AppButton { Text = "Update", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false };
            this.btnDeleteCause = new AppButton { Text = "Hapus", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false, Type=AppButton.ButtonType.Danger };
            btnAddCause.Click += BtnAddCause_Click; btnUpdateCause.Click += BtnUpdateCause_Click; btnDeleteCause.Click += BtnDeleteCause_Click;
            flowCause.Controls.AddRange(new Control[]{ txtCauseName, btnAddCause, btnUpdateCause, btnDeleteCause });
            pnlCause.Controls.Add(flowCause);
            this.gridCauses = CreateGrid();
            this.gridCauses.Columns.Add(new DataGridViewTextBoxColumn { Name="cause_id", DataPropertyName="cause_id", Visible=false });
            this.gridCauses.Columns.Add(new DataGridViewTextBoxColumn { Name="cause_name", DataPropertyName="cause_name", HeaderText="Nama Penyebab" });
            this.gridCauses.CellClick += GridCauses_CellClick;
            this.subCauses.Controls.Add(gridCauses); this.subCauses.Controls.Add(pnlCause);

            this.subActions = new TabPage("Tindakan Perbaikan");
            var pnlAction = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowAction = new FlowLayoutPanel { Dock = DockStyle.Fill };
            this.txtActionName = new AppInput { LabelText = "Nama Tindakan", Width = 400 };
            this.btnAddAction = new AppButton { Text = "Tambah", Width = 90, Margin = new Padding(5,35,5,5) };
            this.btnUpdateAction = new AppButton { Text = "Update", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false };
            this.btnDeleteAction = new AppButton { Text = "Hapus", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false, Type=AppButton.ButtonType.Danger };
            btnAddAction.Click += BtnAddAction_Click; btnUpdateAction.Click += BtnUpdateAction_Click; btnDeleteAction.Click += BtnDeleteAction_Click;
            flowAction.Controls.AddRange(new Control[]{ txtActionName, btnAddAction, btnUpdateAction, btnDeleteAction });
            pnlAction.Controls.Add(flowAction);
            this.gridActions = CreateGrid();
            this.gridActions.Columns.Add(new DataGridViewTextBoxColumn { Name="action_id", DataPropertyName="action_id", Visible=false });
            this.gridActions.Columns.Add(new DataGridViewTextBoxColumn { Name="action_name", DataPropertyName="action_name", HeaderText="Nama Tindakan" });
            this.gridActions.CellClick += GridActions_CellClick;
            this.subActions.Controls.Add(gridActions); this.subActions.Controls.Add(pnlAction);

            this.subTypes = new TabPage("Kategori Problem");
            var pnlType = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowType = new FlowLayoutPanel { Dock = DockStyle.Fill };
            this.txtTypeName = new AppInput { LabelText = "Kategori Problem", Width = 400 };
            this.btnAddType = new AppButton { Text = "Tambah", Width = 90, Margin = new Padding(5,35,5,5) };
            this.btnUpdateType = new AppButton { Text = "Update", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false };
            this.btnDeleteType = new AppButton { Text = "Hapus", Width = 90, Margin = new Padding(5,35,5,5), Enabled=false, Type=AppButton.ButtonType.Danger };
            btnAddType.Click += BtnAddType_Click; btnUpdateType.Click += BtnUpdateType_Click; btnDeleteType.Click += BtnDeleteType_Click;
            flowType.Controls.AddRange(new Control[]{ txtTypeName, btnAddType, btnUpdateType, btnDeleteType });
            pnlType.Controls.Add(flowType);
            this.gridTypes = CreateGrid();
            this.gridTypes.Columns.Add(new DataGridViewTextBoxColumn { Name="type_id", DataPropertyName="type_id", Visible=false });
            this.gridTypes.Columns.Add(new DataGridViewTextBoxColumn { Name="type_name", DataPropertyName="type_name", HeaderText="Kategori Problem" });
            this.gridTypes.CellClick += GridTypes_CellClick;
            this.subTypes.Controls.Add(gridTypes); this.subTypes.Controls.Add(pnlType);

            this.tabGeneralSub.Controls.AddRange(new Control[] { subFailures, subCauses, subActions, subTypes });
            this.tabGeneralMasters.Controls.Add(tabGeneralSub);
        }

        private DataGridView CreateGrid()
        {
            return new DataGridView 
            { 
                Dock = DockStyle.Fill, 
                AllowUserToAddRows = false, 
                ReadOnly = true, 
                BackgroundColor = AppColors.CardBackground, 
                BorderStyle = BorderStyle.None, 
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, 
                AutoGenerateColumns = false, 
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 40
            };
        }
    }
}