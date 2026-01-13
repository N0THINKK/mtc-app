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
    public partial class MasterUserView : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        
        private DataGridView gridUsers;
        private Panel pnlUserForm;
        private AppInput txtUsername;
        private AppInput txtPassword;
        private AppInput txtFullName;
        private AppInput comboRole;
        private AppButton btnAddUser;
        private Label lblTitle;

        // Dictionary to map role names back to role IDs
        private Dictionary<string, int> _roleNameToIdMap = new Dictionary<string, int>();


        public MasterUserView()
        {
            InitializeComponent();
            if (!this.DesignMode)
            {
                LoadRoles();
                LoadUsers();
            }
        }

        private void LoadRoles()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    var roles = connection.Query("SELECT role_id, role_name FROM roles ORDER BY role_name").ToList();
                    
                    _roleNameToIdMap.Clear();
                    var roleNames = new System.Collections.Generic.List<string>();
                    
                    foreach (var role in roles)
                    {
                        roleNames.Add(role.role_name);
                        _roleNameToIdMap[role.role_name] = role.role_id;
                    }
                    
                    comboRole.SetDropdownItems(roleNames.ToArray());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data role: {ex.Message}", "Error");
            }
        }

        private void LoadUsers()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = @"
                        SELECT u.user_id, u.username, u.full_name, r.role_name 
                        FROM users u 
                        JOIN roles r ON u.role_id = r.role_id 
                        ORDER BY u.user_id";
                    var users = connection.Query(sql).ToList();
                    gridUsers.DataSource = users;
                    gridUsers.Columns["user_id"].HeaderText = "ID";
                    gridUsers.Columns["username"].HeaderText = "Username";
                    gridUsers.Columns["full_name"].HeaderText = "Nama Lengkap";
                    gridUsers.Columns["role_name"].HeaderText = "Role";
                    gridUsers.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.Fill);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data user: {ex.Message}", "Error");
            }
        }

        private void BtnAddUser_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.InputValue) || 
                string.IsNullOrWhiteSpace(txtPassword.InputValue) || 
                string.IsNullOrWhiteSpace(comboRole.InputValue))
            {
                MessageBox.Show("Username, Password, dan Role wajib diisi.", "Validasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_roleNameToIdMap.TryGetValue(comboRole.InputValue, out int roleId))
            {
                MessageBox.Show("Role yang dipilih tidak valid.", "Validasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    string sql = @"
                        INSERT INTO users (username, password, full_name, role_id) 
                        VALUES (@Username, @Password, @FullName, @RoleId)";
                    
                    connection.Execute(sql, new { 
                        Username = txtUsername.InputValue,
                        Password = txtPassword.InputValue, // Note: In production, hash this password!
                        FullName = txtFullName.InputValue,
                        RoleId = roleId
                    });

                    MessageBox.Show("User berhasil ditambahkan!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadUsers(); // Refresh grid
                    
                    // Clear inputs
                    txtUsername.InputValue = "";
                    txtPassword.InputValue = "";
                    txtFullName.InputValue = "";
                    comboRole.InputValue = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menambah user: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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
            this.SuspendLayout();
            this.components = new System.ComponentModel.Container();
            
            // Title
            this.lblTitle = new Label();
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblTitle.ForeColor = AppColors.TextPrimary;
            this.lblTitle.Location = new Point(0, 0);
            this.lblTitle.Text = "Manajemen Pengguna";

            // Form Panel
            this.pnlUserForm = new Panel();
            this.pnlUserForm.Dock = DockStyle.Top;
            this.pnlUserForm.Height = 120;
            this.pnlUserForm.Padding = new Padding(0, 40, 0, 10);

            this.txtUsername = new AppInput { LabelText = "Username", Width = 180, Margin = new Padding(5) };
            this.txtPassword = new AppInput { LabelText = "Password", Width = 180, Margin = new Padding(5) };
            this.txtFullName = new AppInput { LabelText = "Nama Lengkap", Width = 200, Margin = new Padding(5) };
            this.comboRole = new AppInput { LabelText = "Role", InputType = AppInput.InputTypeEnum.Dropdown, Width = 180, Margin = new Padding(5) };

            this.btnAddUser = new AppButton { Text = "Tambah User", Width = 120, Height = 40, Margin = new Padding(10, 35, 5, 5) };
            this.btnAddUser.Click += BtnAddUser_Click;

            FlowLayoutPanel flowLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            flowLayout.Controls.Add(txtUsername);
            flowLayout.Controls.Add(txtPassword);
            flowLayout.Controls.Add(txtFullName);
            flowLayout.Controls.Add(comboRole);
            flowLayout.Controls.Add(btnAddUser);
            this.pnlUserForm.Controls.Add(flowLayout);

            // Grid
            this.gridUsers = new DataGridView();
            this.gridUsers.Dock = DockStyle.Fill;
            this.gridUsers.BackgroundColor = Color.White;
            this.gridUsers.BorderStyle = BorderStyle.None;
            this.gridUsers.AllowUserToAddRows = false;
            this.gridUsers.AllowUserToDeleteRows = false;
            this.gridUsers.ReadOnly = true;

            Panel gridPanel = new Panel { Dock = DockStyle.Fill };
            gridPanel.Controls.Add(gridUsers);
            
            this.Controls.Add(gridPanel);
            this.Controls.Add(pnlUserForm);
            this.Controls.Add(lblTitle);
            
            this.Name = "MasterUserView";
            this.Size = new System.Drawing.Size(860, 600);
            this.ResumeLayout(false);
        }
    }
}