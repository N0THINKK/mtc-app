using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.authentication.data.repositories;
using mtc_app.shared.data.dtos;
using mtc_app.shared.data.session;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.navigation;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class LoginForm : AppBaseForm
    {
        private readonly IAuthRepository _authRepository;

        // Composition Root Pattern: Default constructor initializes the implementation.
        // This keeps Program.cs simple while allowing DI for testing if needed via overload.
        public LoginForm() : this(ServiceLocator.CreateAuthRepository())
        {
        }

        public LoginForm(IAuthRepository authRepository)
        {
            InitializeComponent();
            _authRepository = authRepository;

            // Compact UI
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.StartPosition = FormStartPosition.CenterScreen;

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

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            // Reset Error State (if any)
            // txtUsername.FrameColor = AppColors.Border; ...

            string username = txtUsername.InputValue.Trim();
            string password = txtPassword.InputValue.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Harap isi Username dan Password.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // UI Loading State
            btnLogin.Enabled = false;
            btnLogin.Text = "LOGGING IN...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // Async Login
                UserDto user = await _authRepository.LoginAsync(username, password);

                if (user != null)
                {
                    // Show offline login toast if applicable
                    if (user.IsOfflineLogin)
                    {
                        ToastNotification.ShowWarning("Login Offline - synced data only", 4000);
                    }
                    
                    // Success
                    HandleLoginSuccess(user);
                }
                else
                {
                    // Fail
                    MessageBox.Show("Username atau Password salah!", "Login Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan database:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Restore UI State
                btnLogin.Enabled = true;
                btnLogin.Text = "LOGIN";
                this.Cursor = Cursors.Default;
            }
        }

        private void HandleLoginSuccess(UserDto user)
        {
            // 1. Store Session
            UserSession.SetUser(user);

            MessageBox.Show($"Login Berhasil! Selamat datang, {user.Username} ({user.RoleName})", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 2. Hide Login Form
            this.Hide();

            // 3. Navigate
            Form nextForm = DashboardRouter.GetDashboardForUser(user);

            if (nextForm != null)
            {
                // Ensure Login shows back up when the dashboard closes
                nextForm.FormClosed += (s, args) => 
                { 
                    this.Show(); 
                    txtPassword.InputValue = ""; // Clear password for security
                    txtUsername.Focus();
                };
                nextForm.Show();
            }
            else
            {
                MessageBox.Show($"Dashboard untuk role '{user.RoleName}' belum tersedia.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Show();
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}