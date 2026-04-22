using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper; 
using mtc_app.features.authentication.data.repositories;
using mtc_app.shared.data.dtos;
using mtc_app.shared.data.session;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.navigation;
using mtc_app.shared.presentation.styles;
using mtc_app.features.technician.presentation.screens;
using mtc_app.features.stock.presentation.screens;
using mtc_app.features.machine_history.presentation.screens;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class LoginForm : AppBaseForm
    {
        private readonly IAuthRepository _authRepository;
        private readonly ISetupRepository _setupRepository;
        private Label lblMachineName;

        private static readonly string[] KnownRoles = { "Operator", "Teknisi", "Stock" };

        public LoginForm() : this(
            ServiceLocator.CreateAuthRepository(),
            ServiceLocator.CreateSetupRepository())
        {
        }

        public LoginForm(IAuthRepository authRepository, ISetupRepository setupRepository)
        {
            _authRepository = authRepository;
            _setupRepository = setupRepository;

            InitializeComponent();
            SetupForm();
        }

        // =====================================================================
        // SETUP
        // =====================================================================

        private void SetupForm()
        {
            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;

            StyleCardPanel();
            ConfigureRoleDropdown();
            SizeCardToContent();

            InitializeMachineNameLabel();
            LoadMachineNameAsync();

            this.Resize += (s, e) => CenterCard();
            tblLayout.SizeChanged += (s, e) => SizeCardToContent();
            this.VisibleChanged += LoginForm_VisibleChanged;
        }

        private void LoginForm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible && drpRole.InputValue?.Trim() == "Operator")
            {
                LoadOperatorNiks();
            }
        }

        // =====================================================================
        // CARD STYLING (rounded corners + border)
        // =====================================================================

        private void StyleCardPanel()
        {
            pnlCard.Paint += PnlCard_Paint;
            pnlCard.Resize += (s, e) => ApplyCardRegion();
            ApplyCardRegion();

            // Set wrapper panel height to match the AppInput's natural height
            pnlInputSwap.Height = txtIdentity.Height;
        }

        private void PnlCard_Paint(object sender, PaintEventArgs e)
        {
            var rect = new Rectangle(0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            using (var path = BuildRoundedPath(rect, AppDimens.CardCornerRadius))
            using (var pen = new Pen(AppColors.Border, 1f))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            }
        }

        private void ApplyCardRegion()
        {
            var rect = new Rectangle(0, 0, pnlCard.Width, pnlCard.Height);
            using (var path = BuildRoundedPath(rect, AppDimens.CardCornerRadius))
                pnlCard.Region = new Region(path);
        }

        private static GraphicsPath BuildRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // =====================================================================
        // RESPONSIVE CARD SIZING
        // =====================================================================

        /// <summary>
        /// Card width is proportional to the form. Height adapts to content.
        /// No hardcoded pixel positions — the TableLayoutPanel handles stacking.
        /// </summary>
        private void SizeCardToContent()
        {
            int cardWidth = Math.Min(460, (int)(this.ClientSize.Width * 0.55));
            cardWidth = Math.Max(380, cardWidth);

            int cardHeight = tblLayout.PreferredSize.Height + pnlCard.Padding.Vertical;

            pnlCard.Size = new Size(cardWidth, cardHeight);
            CenterCard();
        }

        private void CenterCard()
        {
            pnlCard.Location = new Point(
                Math.Max(0, (ClientSize.Width - pnlCard.Width) / 2),
                Math.Max(0, (ClientSize.Height - pnlCard.Height) / 2));
        }

        // =====================================================================
        // ROLE DROPDOWN
        // =====================================================================

        private void ConfigureRoleDropdown()
        {
            drpRole.SetDropdownItems(KnownRoles);
            drpRole.InputValueChanged += OnRoleChanged;
            drpRole.InputValue = "Operator";
            OnRoleChanged(drpRole, EventArgs.Empty);
        }

        private void OnRoleChanged(object sender, EventArgs e)
        {
            string role = drpRole.InputValue.Trim();

            if (IsKnownRole(role))
            {
                txtPassword.Visible = false;
                txtPassword.InputValue = "";
                txtIdentity.LabelText = GetIdentityLabel(role);
                
                if (role == "Operator")
                {
                    txtIdentity.InputType = AppInput.InputTypeEnum.Dropdown;
                    txtIdentity.AllowCustomText = true;
                    LoadOperatorNiks();
                }
                else
                {
                    txtIdentity.InputType = AppInput.InputTypeEnum.Text;
                }
                
                txtIdentity.Visible = true;
                txtIdentity.BringToFront();
            }
            else
            {
                txtIdentity.Visible = false;
                txtIdentity.InputValue = "";
                txtPassword.Visible = true;
                txtPassword.BringToFront();
            }
        }

        private static bool IsKnownRole(string role)
        {
            foreach (string r in KnownRoles)
                if (string.Equals(r, role, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string GetIdentityLabel(string role)
        {
            switch (role)
            {
                case "Operator": return "NIK Operator";
                case "Teknisi":  return "Inisial / NIK (Kosongi utk ke Dashboard)";
                case "Stock":    return "NIK / Nama Petugas Stock";
                default:         return "Username";
            }
        }

        private void LoadOperatorNiks()
        {
            try
            {
                string configPath = @"C:\MTC_System\Config\operator_niks.csv";
                if (File.Exists(configPath))
                {
                    var niks = File.ReadAllText(configPath)
                        .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim())
                        .Where(n => !string.IsNullOrEmpty(n))
                        .Distinct()
                        .ToArray();
                    txtIdentity.SetDropdownItems(niks);
                }
                else
                {
                    txtIdentity.SetDropdownItems(new string[0]);
                }
            }
            catch { /* Ignore error on reading local CSV */ }
        }

        private void SaveOperatorNik(string nik)
        {
            try 
            {
                string dirPath = @"C:\MTC_System\Config";
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                string filePath = Path.Combine(dirPath, "operator_niks.csv");
                
                var recentNiks = new List<string>();
                if (File.Exists(filePath))
                {
                    recentNiks.AddRange(File.ReadAllText(filePath)
                        .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim())
                        .Where(n => !string.IsNullOrEmpty(n)));
                }
                
                recentNiks.Insert(0, nik);
                var uniqueNiks = recentNiks.Distinct().Take(10);
                
                File.WriteAllText(filePath, string.Join(",", uniqueNiks));
            } 
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan history NIK. Pastikan file 'operator_niks.csv' sedang TIDAK dibuka di Excel!\n\nDetail:\n" + ex.Message, 
                    "Gagal Menyimpan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =====================================================================
        // LOGIN
        // =====================================================================

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
            string roleOrUser = drpRole.InputValue.Trim();
            string identity = txtIdentity.InputValue.Trim();
            string password = txtPassword.InputValue.Trim();

            if (IsKnownRole(roleOrUser))
                await HandleRoleLoginAsync(roleOrUser, identity);
            else
                await HandleAdminLoginAsync(roleOrUser, password);
        }

        private async System.Threading.Tasks.Task HandleRoleLoginAsync(string role, string identity)
        {
            if (role == "Operator" && string.IsNullOrEmpty(identity))
            {
                MessageBox.Show("Harap isi NIK Operator.", "Peringatan",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (role == "Operator")
            {
                SaveOperatorNik(identity);
            }

            string fetchedFullName = null;
            long fetchedUserId = 0;
            
            // Coba ambil dari database jika identitas tidak kosong, jika tidak ada, buat baru
            if (!string.IsNullOrEmpty(identity))
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        var user = conn.QueryFirstOrDefault<UserDto>(
                            "SELECT user_id as UserId, full_name as FullName FROM users WHERE username = @Username OR nik = @Username LIMIT 1",
                            new { Username = identity }
                        );

                        if (user != null)
                        {
                            fetchedUserId = user.UserId;
                            fetchedFullName = user.FullName;
                        }
                        else if (role == "Operator")
                        {
                            // AUTO CREATE NEW OPERATOR
                            string insertSql = "INSERT INTO users (full_name, nik, username, role_id, password) VALUES (@Nik, @Nik, @Nik, 1, '123456'); SELECT LAST_INSERT_ID();";
                            fetchedUserId = conn.ExecuteScalar<long>(insertSql, new { Nik = identity });
                            fetchedFullName = identity;
                        }
                    }
                }
                catch { /* Abaikan jika gagal konek DB, tampil nama login saja */ }
            }

            UserSession.SetUser(new UserDto
            {
                UserId = fetchedUserId,
                Username = string.IsNullOrEmpty(identity) ? role : identity,
                RoleName = role,
                FullName = fetchedFullName // Menyimpan nama lengkap untuk ditampilkan di Checksheet
            });

            this.Hide();
            Form nextForm = CreateFormForRole(role, identity);
            if (nextForm != null)
            {
                nextForm.FormClosed += OnChildFormClosed;
                nextForm.Show();
            }
        }

        private Form CreateFormForRole(string role, string identity)
        {
            switch (role)
            {
                case "Teknisi" when string.IsNullOrEmpty(identity):
                    return new TechnicianDashboardForm();
                case "Teknisi":
                    // Sementara langsung masuk ke patroli harian (mesin/cutting)
                    return new mtc_app.features.machine_history.presentation.screens.ChecksheetForm(isTeknisiMode: true);
                case "Operator":
                    return new OperatorMainMenuForm();
                case "Stock":
                    return new StockDashboardForm();
                default:
                    return null;
            }
        }

        private async System.Threading.Tasks.Task HandleAdminLoginAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Harap isi Username.", "Peringatan",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "LOGGING IN...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                UserDto user = await _authRepository.LoginAsync(username, password);
                if (user != null)
                {
                    if (user.IsOfflineLogin)
                        ToastNotification.ShowWarning("Login Offline - synced data only", 4000);
                    HandleLoginSuccess(user);
                }
                else
                {
                    MessageBox.Show("Username atau Password salah!", "Login Gagal",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan database:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            ToastNotification.ShowSuccess(
                $"Login Berhasil! Selamat datang, {user.Username} ({user.RoleName})", 3000);

            this.Hide();
            Form nextForm = DashboardRouter.GetDashboardForUser(user);
            if (nextForm != null)
            {
                nextForm.FormClosed += OnChildFormClosed;
                nextForm.Show();
            }
            else
            {
                MessageBox.Show($"Dashboard untuk role '{user.RoleName}' belum tersedia.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Show();
            }
        }

        // =====================================================================
        // CHILD FORM LIFECYCLE
        // =====================================================================

        private void OnChildFormClosed(object sender, FormClosedEventArgs e)
        {
            this.Show();
            txtIdentity.InputValue = "";
            txtPassword.InputValue = "";
            drpRole.Focus();
        }

        private void btnExit_Click(object sender, EventArgs e) => Application.Exit();

        // =====================================================================
        // MACHINE NAME LABEL (bottom-right)
        // =====================================================================

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

            lblMachineName.Location = new Point(
                ClientSize.Width - 150, ClientSize.Height - 20);
            lblMachineName.Click += (s, e) => OpenSetupForm();
            this.Controls.Add(lblMachineName);
            lblMachineName.BringToFront();

            this.Resize += (s, e) =>
            {
                if (lblMachineName != null)
                    lblMachineName.Location = new Point(
                        ClientSize.Width - lblMachineName.Width - 10,
                        ClientSize.Height - lblMachineName.Height - 5);
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
                        lblMachineName.Location = new Point(
                            ClientSize.Width - lblMachineName.Width - 10,
                            ClientSize.Height - lblMachineName.Height - 5);

                        string lowerName = name.ToLower();
                        if (lowerName.Contains("teknisi"))
                            drpRole.InputValue = "Teknisi";
                        else if (lowerName.Contains("stock") || lowerName.Contains("gudang"))
                            drpRole.InputValue = "Stock";
                        else
                            drpRole.InputValue = "Operator";
                    }
                    else
                    {
                        lblMachineName.Text = "Machine: Unknown";
                        drpRole.InputValue = "Operator";
                    }
                }
                else
                {
                    lblMachineName.Text = "Machine: Not Configured";
                    drpRole.InputValue = "Operator";
                }
            }
            catch
            {
                lblMachineName.Text = "Machine: Error";
                drpRole.InputValue = "Operator";
            }
        }

        private void OpenSetupForm()
        {
            var setupForm = new SetupForm();
            if (setupForm.ShowDialog() == DialogResult.OK)
                LoadMachineNameAsync();
        }
    }
}