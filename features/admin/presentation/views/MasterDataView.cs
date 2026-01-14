using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.views
{
    public partial class MasterDataView : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        
        // Main Tab Control
        private TabControl tabControl;
        private TabPage tabUsers, tabMachines, tabFailures;

        // User Tab Controls
        private DataGridView gridUsers;
        private AppInput txtUsername, txtPassword, txtFullName, comboRole;
        private AppButton btnAddUser, btnUpdateUser, btnDeleteUser;
        private Dictionary<string, int> _roleNameToIdMap = new Dictionary<string, int>();
        private int? _selectedUserId = null;

        // Machine Tab Controls
        private DataGridView gridMachines;
        private AppInput txtMachineCode, txtMachineName, txtLocation;
        private AppButton btnAddMachine, btnUpdateMachine, btnDeleteMachine;
        private int? _selectedMachineId = null;

        // Failure Tab Controls
        private DataGridView gridFailures;
        private AppInput txtFailureName;
        private AppButton btnAddFailure, btnUpdateFailure, btnDeleteFailure;
        private int? _selectedFailureId = null;


        public MasterDataView()
        {
            InitializeComponent();
            if (!this.DesignMode)
            {
                // Load data for all tabs
                LoadRoles();
                LoadUsers();
                LoadMachines();
                LoadFailures();
            }
        }

        #region User Management Logic
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
            catch (Exception ex) { MessageBox.Show($"Gagal memuat data role: {ex.Message}"); }
        }

        private void LoadUsers()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = "SELECT u.user_id, u.username, u.full_name, r.role_name FROM users u JOIN roles r ON u.role_id = r.role_id ORDER BY u.user_id";
                    gridUsers.DataSource = connection.Query(sql).ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal memuat data user: {ex.Message}"); }
            ClearUserSelection();
        }

        private void BtnAddUser_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.InputValue) || string.IsNullOrWhiteSpace(txtPassword.InputValue) || string.IsNullOrWhiteSpace(comboRole.InputValue))
            {
                MessageBox.Show("Username, Password, dan Role wajib diisi.", "Validasi Gagal");
                return;
            }
            if (!_roleNameToIdMap.TryGetValue(comboRole.InputValue, out int roleId))
            {
                MessageBox.Show("Role yang dipilih tidak valid.", "Validasi Gagal");
                return;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = "INSERT INTO users (username, password, full_name, role_id) VALUES (@Username, @Password, @FullName, @RoleId)";
                    connection.Execute(sql, new { Username = txtUsername.InputValue, Password = txtPassword.InputValue, FullName = txtFullName.InputValue, RoleId = roleId });
                    MessageBox.Show("User berhasil ditambahkan!");
                    LoadUsers();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal menambah user: {ex.Message}"); }
        }

        private void BtnUpdateUser_Click(object sender, EventArgs e)
        {
            if (_selectedUserId == null)
            {
                MessageBox.Show("Pilih user dari tabel terlebih dahulu.", "Info");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtUsername.InputValue) || string.IsNullOrWhiteSpace(comboRole.InputValue))
            {
                MessageBox.Show("Username dan Role wajib diisi.", "Validasi Gagal");
                return;
            }
            if (!_roleNameToIdMap.TryGetValue(comboRole.InputValue, out int roleId))
            {
                MessageBox.Show("Role yang dipilih tidak valid.", "Validasi Gagal");
                return;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = "UPDATE users SET username = @Username, full_name = @FullName, role_id = @RoleId ";
                    if (!string.IsNullOrWhiteSpace(txtPassword.InputValue))
                    {
                        sql += ", password = @Password ";
                    }
                    sql += "WHERE user_id = @UserId";

                    connection.Execute(sql, new { 
                        Username = txtUsername.InputValue, 
                        FullName = txtFullName.InputValue, 
                        RoleId = roleId, 
                        Password = txtPassword.InputValue,
                        UserId = _selectedUserId.Value 
                    });

                    MessageBox.Show("User berhasil diupdate!");
                    LoadUsers();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal mengupdate user: {ex.Message}"); }
        }
        
        private void BtnDeleteUser_Click(object sender, EventArgs e)
        {
            if (_selectedUserId == null)
            {
                MessageBox.Show("Pilih user dari tabel terlebih dahulu.", "Info");
                return;
            }

            var confirmResult = MessageBox.Show($"Anda yakin ingin menghapus user '{txtUsername.InputValue}'?", "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        string sql = "DELETE FROM users WHERE user_id = @UserId";
                        connection.Execute(sql, new { UserId = _selectedUserId.Value });
                        MessageBox.Show("User berhasil dihapus!");
                        LoadUsers();
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Gagal menghapus user: {ex.Message}"); }
            }
        }

        private void GridUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = gridUsers.Rows[e.RowIndex];
                _selectedUserId = Convert.ToInt32(row.Cells["user_id"].Value);

                txtUsername.InputValue = row.Cells["username"].Value.ToString();
                txtFullName.InputValue = row.Cells["full_name"].Value?.ToString();
                comboRole.InputValue = row.Cells["role_name"].Value.ToString();
                txtPassword.InputValue = "";
                
                btnUpdateUser.Enabled = true;
                btnDeleteUser.Enabled = true;
            }
        }

        private void ClearUserSelection()
        {
            _selectedUserId = null;
            txtUsername.InputValue = "";
            txtPassword.InputValue = "";
            txtFullName.InputValue = "";
            comboRole.InputValue = "";
            btnUpdateUser.Enabled = false;
            btnDeleteUser.Enabled = false;
            gridUsers.ClearSelection();
        }

        #endregion

        #region Machine Management Logic
        private void LoadMachines()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    gridMachines.DataSource = connection.Query("SELECT machine_id, machine_code, machine_name, location FROM machines ORDER BY machine_id").ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal memuat data mesin: {ex.Message}"); }
            ClearMachineSelection();
        }

        private void BtnAddMachine_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMachineName.InputValue))
            {
                MessageBox.Show("Nama Mesin wajib diisi.");
                return;
            }
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = "INSERT INTO machines (machine_code, machine_name, location) VALUES (@Code, @Name, @Location)";
                    connection.Execute(sql, new { Code = txtMachineCode.InputValue, Name = txtMachineName.InputValue, Location = txtLocation.InputValue });
                    MessageBox.Show("Mesin berhasil ditambahkan!");
                    LoadMachines();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal menambah mesin: {ex.Message}"); }
        }

        private void BtnUpdateMachine_Click(object sender, EventArgs e)
        {
            if (_selectedMachineId == null) { MessageBox.Show("Pilih mesin dari tabel."); return; }
            if (string.IsNullOrWhiteSpace(txtMachineName.InputValue)) { MessageBox.Show("Nama Mesin wajib diisi."); return; }
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = "UPDATE machines SET machine_code = @Code, machine_name = @Name, location = @Location WHERE machine_id = @Id";
                    connection.Execute(sql, new { Code = txtMachineCode.InputValue, Name = txtMachineName.InputValue, Location = txtLocation.InputValue, Id = _selectedMachineId.Value });
                    MessageBox.Show("Mesin berhasil diupdate!");
                    LoadMachines();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal mengupdate mesin: {ex.Message}"); }
        }

        private void BtnDeleteMachine_Click(object sender, EventArgs e)
        {
            if (_selectedMachineId == null) { MessageBox.Show("Pilih mesin dari tabel."); return; }
            var confirmResult = MessageBox.Show($"Yakin ingin menghapus mesin '{txtMachineName.InputValue}'?", "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Execute("DELETE FROM machines WHERE machine_id = @Id", new { Id = _selectedMachineId.Value });
                        MessageBox.Show("Mesin berhasil dihapus!");
                        LoadMachines();
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Gagal menghapus mesin: {ex.Message}. Pastikan tidak ada tiket yang terkait."); }
            }
        }

        private void GridMachines_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = gridMachines.Rows[e.RowIndex];
                _selectedMachineId = Convert.ToInt32(row.Cells["machine_id"].Value);
                txtMachineCode.InputValue = row.Cells["machine_code"].Value?.ToString();
                txtMachineName.InputValue = row.Cells["machine_name"].Value?.ToString();
                txtLocation.InputValue = row.Cells["location"].Value?.ToString();
                btnUpdateMachine.Enabled = true;
                btnDeleteMachine.Enabled = true;
            }
        }

        private void ClearMachineSelection()
        {
            _selectedMachineId = null;
            txtMachineCode.InputValue = txtMachineName.InputValue = txtLocation.InputValue = "";
            btnUpdateMachine.Enabled = false;
            btnDeleteMachine.Enabled = false;
            gridMachines.ClearSelection();
        }
        #endregion
        
        #region Failure Management Logic
        private void LoadFailures()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    gridFailures.DataSource = connection.Query("SELECT failure_id, failure_name FROM failures ORDER BY failure_name").ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal memuat data masalah: {ex.Message}"); }
            ClearFailureSelection();
        }

        private void BtnAddFailure_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFailureName.InputValue)) { MessageBox.Show("Nama Masalah wajib diisi."); return; }
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Execute("INSERT INTO failures (failure_name) VALUES (@Name)", new { Name = txtFailureName.InputValue });
                    MessageBox.Show("Jenis masalah berhasil ditambahkan!");
                    LoadFailures();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal menambah masalah: {ex.Message}"); }
        }

        private void BtnUpdateFailure_Click(object sender, EventArgs e)
        {
            if (_selectedFailureId == null) { MessageBox.Show("Pilih masalah dari tabel."); return; }
            if (string.IsNullOrWhiteSpace(txtFailureName.InputValue)) { MessageBox.Show("Nama Masalah wajib diisi."); return; }
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Execute("UPDATE failures SET failure_name = @Name WHERE failure_id = @Id", new { Name = txtFailureName.InputValue, Id = _selectedFailureId.Value });
                    MessageBox.Show("Jenis masalah berhasil diupdate!");
                    LoadFailures();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gagal mengupdate masalah: {ex.Message}"); }
        }

        private void BtnDeleteFailure_Click(object sender, EventArgs e)
        {
            if (_selectedFailureId == null) { MessageBox.Show("Pilih masalah dari tabel."); return; }
            var confirmResult = MessageBox.Show($"Yakin ingin menghapus masalah '{txtFailureName.InputValue}'?", "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Execute("DELETE FROM failures WHERE failure_id = @Id", new { Id = _selectedFailureId.Value });
                        MessageBox.Show("Masalah berhasil dihapus!");
                        LoadFailures();
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Gagal menghapus masalah: {ex.Message}. Pastikan tidak ada tiket yang terkait."); }
            }
        }

        private void GridFailures_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = gridFailures.Rows[e.RowIndex];
                _selectedFailureId = Convert.ToInt32(row.Cells["failure_id"].Value);
                txtFailureName.InputValue = row.Cells["failure_name"].Value.ToString();
                btnUpdateFailure.Enabled = true;
                btnDeleteFailure.Enabled = true;
            }
        }

        private void ClearFailureSelection()
        {
            _selectedFailureId = null;
            txtFailureName.InputValue = "";
            btnUpdateFailure.Enabled = false;
            btnDeleteFailure.Enabled = false;
            gridFailures.ClearSelection();
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabControl = new TabControl();
            this.tabUsers = new TabPage("Manajemen User");
            this.tabMachines = new TabPage("Manajemen Mesin");
            this.tabFailures = new TabPage("Manajemen Masalah");
            
            // Main Control
            this.Dock = DockStyle.Fill;
            this.tabControl.Dock = DockStyle.Fill;
            this.tabControl.Font = new Font("Segoe UI", 10F);
            this.tabControl.Controls.AddRange(new Control[] { this.tabUsers, this.tabMachines, this.tabFailures });
            this.Controls.Add(tabControl);

            // --- TAB 1: USERS ---
            var pnlUserForm = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowUser = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = true };
            this.txtUsername = new AppInput { LabelText = "Username", Width = 150 };
            this.txtPassword = new AppInput { LabelText = "Password (kosongi jika tak diubah)", Width = 200 };
            this.txtFullName = new AppInput { LabelText = "Nama Lengkap", Width = 200 };
            this.comboRole = new AppInput { LabelText = "Role", InputType = AppInput.InputTypeEnum.Dropdown, Width = 150 };
            this.btnAddUser = new AppButton { Text = "Tambah", Width = 90, Height = 40, Margin = new Padding(5, 35, 5, 5) };
            this.btnUpdateUser = new AppButton { Text = "Update", Width = 90, Height = 40, Margin = new Padding(5, 35, 5, 5), Enabled = false };
            this.btnDeleteUser = new AppButton { Text = "Hapus", Width = 90, Height = 40, Margin = new Padding(5, 35, 5, 5), Enabled = false, Type = AppButton.ButtonType.Danger };
            this.btnAddUser.Click += BtnAddUser_Click;
            this.btnUpdateUser.Click += BtnUpdateUser_Click;
            this.btnDeleteUser.Click += BtnDeleteUser_Click;
            flowUser.Controls.AddRange(new Control[] { txtUsername, txtPassword, txtFullName, comboRole, btnAddUser, btnUpdateUser, btnDeleteUser });
            pnlUserForm.Controls.Add(flowUser);
            this.gridUsers = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            this.gridUsers.CellClick += GridUsers_CellClick;
            var pnlGridUser = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlGridUser.Controls.Add(gridUsers);
            this.tabUsers.Controls.AddRange(new Control[] { pnlGridUser, pnlUserForm });

            // --- TAB 2: MACHINES ---
            var pnlMachineForm = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowMachine = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            this.txtMachineCode = new AppInput { LabelText = "Kode Mesin", Width = 150 };
            this.txtMachineName = new AppInput { LabelText = "Nama Mesin", Width = 200 };
            this.txtLocation = new AppInput { LabelText = "Lokasi", Width = 150 };
            this.btnAddMachine = new AppButton { Text = "Tambah", Width = 90, Height = 40, Margin = new Padding(5, 35, 5, 5) };
            this.btnUpdateMachine = new AppButton { Text = "Update", Width = 90, Height = 40, Margin = new Padding(5, 35, 5, 5), Enabled = false };
            this.btnDeleteMachine = new AppButton { Text = "Hapus", Width = 90, Height = 40, Margin = new Padding(5, 35, 5, 5), Enabled = false, Type = AppButton.ButtonType.Danger };
            this.btnAddMachine.Click += BtnAddMachine_Click;
            this.btnUpdateMachine.Click += BtnUpdateMachine_Click;
            this.btnDeleteMachine.Click += BtnDeleteMachine_Click;
            flowMachine.Controls.AddRange(new Control[] { txtMachineCode, txtMachineName, txtLocation, btnAddMachine, btnUpdateMachine, btnDeleteMachine });
            pnlMachineForm.Controls.Add(flowMachine);
            this.gridMachines = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            this.gridMachines.CellClick += GridMachines_CellClick;
            var pnlGridMachine = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlGridMachine.Controls.Add(gridMachines);
            this.tabMachines.Controls.AddRange(new Control[] { pnlGridMachine, pnlMachineForm });
            
            // --- TAB 3: FAILURES ---
            var pnlFailureForm = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            var flowFailure = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            this.txtFailureName = new AppInput { LabelText = "Nama Masalah", Width = 300 };
            this.btnAddFailure = new AppButton { Text = "Tambah", Width = 90, Height = 40, Margin = new Padding(5, 35, 5, 5) };
            this.btnUpdateFailure = new AppButton { Text = "Update", Width = 90, Height = 40, Margin = new Padding(5, 35, 5, 5), Enabled = false };
            this.btnDeleteFailure = new AppButton { Text = "Hapus", Width = 90, Height = 40, Margin = new Padding(5, 35, 5, 5), Enabled = false, Type = AppButton.ButtonType.Danger };
            this.btnAddFailure.Click += BtnAddFailure_Click;
            this.btnUpdateFailure.Click += BtnUpdateFailure_Click;
            this.btnDeleteFailure.Click += BtnDeleteFailure_Click;
            flowFailure.Controls.AddRange(new Control[] { txtFailureName, btnAddFailure, btnUpdateFailure, btnDeleteFailure });
            pnlFailureForm.Controls.Add(flowFailure);
            this.gridFailures = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            this.gridFailures.CellClick += GridFailures_CellClick;
            var pnlGridFailure = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlGridFailure.Controls.Add(gridFailures);
            this.tabFailures.Controls.AddRange(new Control[] { pnlGridFailure, pnlFailureForm });
        }
    }
}
