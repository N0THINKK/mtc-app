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
using mtc_app.features.technician.presentation.screens; 
using mtc_app.features.stock.presentation.screens;
using mtc_app.features.machine_history.presentation.screens; // Ditambahkan untuk akses Checksheet

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class LoginForm : AppBaseForm
    {
        private readonly IAuthRepository _authRepository;
        private readonly ISetupRepository _setupRepository;
        private Label lblMachineName;
        
        private ComboBox cmbRole;
        private Label lblRoleTitle;

        public LoginForm() : this(ServiceLocator.CreateAuthRepository(), ServiceLocator.CreateSetupRepository())
        {
        }

        public LoginForm(IAuthRepository authRepository, ISetupRepository setupRepository)
        {
            InitializeComponent();
            _authRepository = authRepository;
            _setupRepository = setupRepository;

            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
            
            InitializeMachineNameLabel();
            LoadMachineNameAsync();

            InitializeRoleDropdown();
            
            cmbRole.Text = "Operator"; 
        }

        private void InitializeRoleDropdown()
        {
            this.AutoSize = false;
            this.AutoSizeMode = AutoSizeMode.GrowOnly;
            panel1.Size = new Size(440, 450);
            this.ClientSize = new Size(440, 450);

            lblRoleTitle = new Label
            {
                Text = "Login Sebagai / Username:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(40, 60)
            };

            cmbRole = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown, 
                Font = new Font("Segoe UI", 12F),
                Size = new Size(360, 35),
                Location = new Point(40, 85)
            };
            
            cmbRole.Items.AddRange(new string[] { "Operator", "Teknisi", "Stock" });
            
            panel1.Controls.Add(lblRoleTitle);
            panel1.Controls.Add(cmbRole);

            txtUsername.Location = new Point(40, 140);
            txtPassword.Location = new Point(40, 140);

            btnLogin.Location = new Point(40, 240);
            btnExit.Location = new Point(170, 300);

            cmbRole.TextChanged += CmbRole_TextChanged;
        }

        private void CmbRole_TextChanged(object sender, EventArgs e)
        {
            string input = cmbRole.Text.Trim();

            if (input == "Operator" || input == "Teknisi" || input == "Stock")
            {
                txtPassword.Visible = false;      
                txtPassword.InputValue = "";

                if (input == "Operator")
                    txtUsername.LabelText = "NIK Operator";
                else if (input == "Teknisi")
                    txtUsername.LabelText = "Inisial / NIK (Kosongi utk ke Dashboard)";
                else
                    txtUsername.LabelText = "NIK / Nama Petugas Stock";

                txtUsername.Visible = true;
                txtUsername.BringToFront(); 
            }
            else
            {
                txtUsername.Visible = false;
                txtUsername.InputValue = "";
                
                txtPassword.Visible = true;
                txtPassword.BringToFront();
            }
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.ActiveControl == btnLogin) return;
                this.SelectNextControl(this.ActiveControl, true, true, true, true);
                e.Handled = true;
                e.SuppressKeyPress = true; 
            }
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string roleOrUser = cmbRole.Text.Trim(); 
            string identitasTambahan = txtUsername.InputValue.Trim(); 
            string passwordInput = txtPassword.InputValue.Trim();

            // ---------------------------------------------------------
            // ALUR 1: TANPA PASSWORD (OPERATOR, TEKNISI, STOCK)
            // ---------------------------------------------------------
            if (roleOrUser == "Operator" || roleOrUser == "Teknisi" || roleOrUser == "Stock")
            {
                if (roleOrUser == "Operator" && string.IsNullOrEmpty(identitasTambahan))
                {
                    MessageBox.Show("Harap isi NIK Operator.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                UserSession.SetUser(new UserDto { 
                    Username = string.IsNullOrEmpty(identitasTambahan) ? roleOrUser : identitasTambahan, 
                    RoleName = roleOrUser 
                });

                this.Hide();

                if (roleOrUser == "Teknisi" && string.IsNullOrEmpty(identitasTambahan))
                {
                    var techDashboard = new TechnicianDashboardForm();
                    techDashboard.FormClosed += (s, args) => { this.Show(); cmbRole.Focus(); };
                    techDashboard.Show();
                }
                else if (roleOrUser == "Teknisi" && !string.IsNullOrEmpty(identitasTambahan))
                {
                    // TAHAP 4 SELESAI: Masuk ke Checksheet Teknisi
                    var techCheckForm = new ChecksheetForm(isTeknisiMode: true);
                    techCheckForm.FormClosed += (s, args) => { 
                        this.Show(); 
                        txtUsername.InputValue = ""; // Bersihkan NIK saat kembali
                        cmbRole.Focus(); 
                    };
                    techCheckForm.Show(); 
                }
                else if (roleOrUser == "Operator")
                {
                    // TAHAP 3 SELESAI: Masuk ke Menu Operator
                    var operatorMenu = new OperatorMainMenuForm();
                    operatorMenu.FormClosed += (s, args) => { 
                        this.Show(); 
                        txtUsername.InputValue = ""; // Bersihkan NIK saat kembali
                        cmbRole.Focus(); 
                    };
                    operatorMenu.Show();
                }
                else if (roleOrUser == "Stock")
                {
                    var stockDashboard = new StockDashboardForm();
                    stockDashboard.FormClosed += (s, args) => { 
                        this.Show(); 
                        txtUsername.InputValue = ""; 
                        cmbRole.Focus(); 
                    };
                    stockDashboard.Show();
                }
                return;
            }

            // ---------------------------------------------------------
            // ALUR 2: LAINNYA / ADMIN (DATABASE DAN PASSWORD)
            // ---------------------------------------------------------
            if (string.IsNullOrEmpty(roleOrUser))
            {
                MessageBox.Show("Harap isi Username.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "LOGGING IN...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                UserDto user = await _authRepository.LoginAsync(roleOrUser, passwordInput);

                if (user != null)
                {
                    if (user.IsOfflineLogin) ToastNotification.ShowWarning("Login Offline - synced data only", 4000);
                    HandleLoginSuccess(user);
                }
                else
                {
                    MessageBox.Show("Username atau Password salah!", "Login Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan database:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "LOGIN";
                this.Cursor = Cursors.Default;
            }
        }

        private void HandleLoginSuccess(UserDto user)
        {
            UserSession.SetUser(user);
            ToastNotification.ShowSuccess($"Login Berhasil! Selamat datang, {user.Username} ({user.RoleName})", 3000);

            this.Hide();
            Form nextForm = DashboardRouter.GetDashboardForUser(user);

            if (nextForm != null)
            {
                nextForm.FormClosed += (s, args) => 
                { 
                    this.Show(); 
                    txtPassword.InputValue = ""; 
                    cmbRole.Focus();
                };
                nextForm.Show();
            }
            else
            {
                MessageBox.Show($"Dashboard untuk role '{user.RoleName}' belum tersedia.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Show();
            }
        }

        private void btnExit_Click(object sender, EventArgs e) { Application.Exit(); }

        private void InitializeMachineNameLabel()
        {
            lblMachineName = new Label
            {
                Text = "Machine: Loading...",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold | FontStyle.Underline),
                ForeColor = Color.Gray,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            
            lblMachineName.Location = new Point(this.ClientSize.Width - 150, this.ClientSize.Height - 20);
            lblMachineName.Click += (s, e) => OpenSetupForm();
            this.Controls.Add(lblMachineName);
            lblMachineName.BringToFront(); 

            this.Resize += (s, e) => { if (lblMachineName != null) lblMachineName.Location = new Point(this.ClientSize.Width - lblMachineName.Width - 10, this.ClientSize.Height - lblMachineName.Height - 5); };
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
                        lblMachineName.Location = new Point(this.ClientSize.Width - lblMachineName.Width - 10, this.ClientSize.Height - lblMachineName.Height - 5);
                        
                        string lowerName = name.ToLower();
                        if (lowerName.Contains("teknisi")) cmbRole.Text = "Teknisi";
                        else if (lowerName.Contains("stock") || lowerName.Contains("gudang")) cmbRole.Text = "Stock";
                        else cmbRole.Text = "Operator";
                    }
                    else
                    {
                         lblMachineName.Text = "Machine: Unknown";
                         cmbRole.Text = "Operator";
                    }
                }
                else
                {
                    lblMachineName.Text = "Machine: Not Configured";
                    cmbRole.Text = "Operator";
                }
            }
            catch
            {
                lblMachineName.Text = "Machine: Error";
                cmbRole.Text = "Operator";
            }
        }

        private void OpenSetupForm()
        {
            var setupForm = new SetupForm();
            if (setupForm.ShowDialog() == DialogResult.OK) LoadMachineNameAsync();
        }
    }
}