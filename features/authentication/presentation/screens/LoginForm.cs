using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.machine_history.presentation.screens;
using mtc_app.shared.presentation.components;
using mtc_app.features.rating.presentation.screens;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class LoginForm : AppBaseForm
    {
        public LoginForm()
        {
            InitializeComponent();
            
            // Enable KeyPreview to catch key presses form-wide
            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // If login button is focused, let it click
                if (this.ActiveControl == btnLogin)
                {
                    return;
                }

                // Move to next control
                this.SelectNextControl(this.ActiveControl, true, true, true, true);
                e.Handled = true;
                e.SuppressKeyPress = true; // Stop ding sound
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.InputValue.Trim();
            string password = txtPassword.InputValue.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Harap isi Username dan Password.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    // Join with roles table to get role_name
                    string sql = @"
                        SELECT u.*, r.role_name 
                        FROM users u 
                        JOIN roles r ON u.role_id = r.role_id 
                        WHERE u.username = @Username AND u.password = @Password 
                        LIMIT 1";
                    
                    var user = connection.QueryFirstOrDefault(sql, new { Username = username, Password = password });

                    if (user != null)
                    {
                        string roleName = user.role_name;
                        MessageBox.Show($"Login Berhasil! Selamat datang, {user.username} ({roleName})", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Hide Login Form
                        this.Hide();

                        Form nextForm = null;

                        // Normalize string for safety (lowercase, trim)
                        switch (roleName.ToLower().Trim())
                        {
                            case "operator": 
                                nextForm = new MachineHistoryFormOperator();
                                break;
                            case "technician":
                            case "teknisi": 
                                // nextForm = new MachineHistoryFormTechnician();
                                MessageBox.Show("Halaman Teknisi sedang dalam pengembangan.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                break;
                            case "stock control":
                            case "stock_control":
                            case "stock":
                                nextForm = new mtc_app.features.stock.presentation.screens.StockDashboardForm();
                                break;
                            case "admin":
                            case "administrator":
                                nextForm = new mtc_app.features.admin.presentation.screens.AdminDashboardForm();
                                break;
                            case "gl_production":
                                nextForm = new mtc_app.features.group_leader.presentation.screens.GroupLeaderDashboardForm();
                                break;    
                            default: 
                                MessageBox.Show($"Dashboard untuk role '{roleName}' belum tersedia.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                break;
                        }

                        if (nextForm != null)
                        {
                            nextForm.FormClosed += (s, args) => this.Close(); 
                            nextForm.Show();
                        }
                        else
                        {
                            this.Show(); // Show login back if no form is opened
                        }
                    }
                    else
                    {
                        MessageBox.Show("Username atau Password salah!", "Login Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan database:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}