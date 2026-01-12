using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.features.machine_history.presentation.screens;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

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
                    
                    // Simple query to check user. In production, use Hashing!
                    // Assuming database has 'users' table with 'username' and 'password' columns.
                    // Based on legacy info, table might be different, but using standard 'users' for now based on 'DB-MTCFULL.sql'.
                    string sql = "SELECT * FROM users WHERE username = @Username AND password = @Password LIMIT 1";
                    
                    var user = connection.QueryFirstOrDefault(sql, new { Username = username, Password = password });

                    if (user != null)
                    {
                        MessageBox.Show($"Login Berhasil! Selamat datang, {user.username}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Hide Login Form
                        this.Hide();

                        // Show Main Dashboard (MachineHistoryForm)
                        MachineHistoryForm mainForm = new MachineHistoryForm();
                        mainForm.FormClosed += (s, args) => this.Close(); // Close app when main form closes
                        mainForm.Show();
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