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
        private readonly ISetupRepository _setupRepository;
        private Label lblMachineName;

        // Composition Root Pattern: Default constructor initializes the implementation.
        // This keeps Program.cs simple while allowing DI for testing if needed via overload.
        public LoginForm() : this(ServiceLocator.CreateAuthRepository(), ServiceLocator.CreateSetupRepository())
        {
        }

        public LoginForm(IAuthRepository authRepository, ISetupRepository setupRepository)
        {
            InitializeComponent();
            _authRepository = authRepository;
            _setupRepository = setupRepository;

            // Compact UI
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Enable KeyPreview to catch key presses form-wide
            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
            
            InitializeMachineNameLabel();
            LoadMachineNameAsync();
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

        private void InitializeMachineNameLabel()
        {
            lblMachineName = new Label
            {
                Text = "Machine: Loading...",
                Font = new Font("Segoe UI", 8.25F, FontStyle.Underline),
                ForeColor = Color.Gray,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            
            // Calculate Position (Bottom Right, with padding)
            // Since AutoSize is true for the Form, ensure we add it to the flow or position absolutely relative to client area
            // However, LoginForm seems to use fixed controls or Flow? Designer file will tell.
            // Assuming absolute positioning works if we add to Controls collection.
            
            // We'll hook into Load or Layout event to position it correctly if AutoSize form changes size.
            // But initial placement:
            lblMachineName.Location = new Point(this.ClientSize.Width - 150, this.ClientSize.Height - 20);
            
            lblMachineName.Click += (s, e) => OpenSetupForm();
            this.Controls.Add(lblMachineName);
            lblMachineName.BringToFront(); // Ensure it sits on top of panel1

            // Ensure it stays at bottom right
            this.Resize += (s, e) => 
            {
                 if (lblMachineName != null)
                    lblMachineName.Location = new Point(this.ClientSize.Width - lblMachineName.Width - 10, this.ClientSize.Height - lblMachineName.Height - 5);
            };
        }

        private async void LoadMachineNameAsync()
        {
            try
            {
                string machineIdStr = DatabaseHelper.GetMachineId();
                if (int.TryParse(machineIdStr, out int machineId))
                {
                    string name = await _setupRepository.GetMachineNameByIdAsync(machineId);
                    if (!string.IsNullOrEmpty(name))
                    {
                        lblMachineName.Text = name;
                        // Re-position after text change (autosize)
                        lblMachineName.Location = new Point(this.ClientSize.Width - lblMachineName.Width - 10, this.ClientSize.Height - lblMachineName.Height - 5);
                    }
                    else
                    {
                         lblMachineName.Text = "Machine: Unknown";
                    }
                }
                else
                {
                    lblMachineName.Text = "Machine: Not Configured";
                }
            }
            catch
            {
                lblMachineName.Text = "Machine: Error";
            }
        }

        private void OpenSetupForm()
        {
            var setupForm = new SetupForm();
            if (setupForm.ShowDialog() == DialogResult.OK)
            {
                // Refresh Name
                LoadMachineNameAsync();
            }
        }
    }
}