using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using mtc_app.features.authentication.data.repositories;
using mtc_app.features.authentication.presentation.controllers;
using mtc_app.shared.data.dtos;
using mtc_app.shared.data.utils;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.navigation;
using mtc_app.shared.presentation.styles;
using mtc_app.features.technician.presentation.screens;
using mtc_app.features.stock.presentation.screens;
using mtc_app.features.machine_history.presentation.screens;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class LoginForm : AppBaseForm, ILoginView
    {
        private readonly LoginController _controller;
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
            _setupRepository = setupRepository;
            _controller = new LoginController(this, authRepository, setupRepository);

            InitializeComponent();
            SetupForm();
        }

        // ==========================================
        // ILoginView Implementation
        // ==========================================
        
        public string SelectedRole => drpRole.InputValue?.Trim() ?? "";
        public string Identity => txtIdentity.InputValue?.Trim() ?? "";
        public string Password => txtPassword.InputValue?.Trim() ?? "";

        public void SetBusyState(bool isBusy)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetBusyState(isBusy)));
                return;
            }
            btnLogin.Enabled = !isBusy;
            btnLogin.Text = isBusy ? "LOGGING IN..." : "LOGIN";
            this.Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
        }

        public void ShowError(string message, string title = "Error")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowError(message, title)));
                return;
            }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowWarning(string message, string title = "Peringatan")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowWarning(message, title)));
                return;
            }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public void ShowSuccess(string message, string title = "Sukses")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowSuccess(message, title)));
                return;
            }
            ToastNotification.ShowSuccess(message, 3000);
        }

        public void HideForm()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(HideForm));
                return;
            }
            this.Hide();
        }

        public void ShowForm()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ShowForm));
                return;
            }
            this.Show();
        }

        public void ProceedToDashboard(UserDto user)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ProceedToDashboard(user)));
                return;
            }
            
            Form nextForm = null;
            if (user.RoleName == "Operator")
            {
                nextForm = new OperatorMainMenuForm();
            }
            else if (user.RoleName == "Teknisi")
            {
                if (string.IsNullOrEmpty(user.Username) || user.Username == "Teknisi")
                    nextForm = new TechnicianDashboardForm();
                else
                    nextForm = new ChecksheetForm(isTeknisiMode: true);
            }
            else if (user.RoleName == "Stock")
            {
                nextForm = new StockDashboardForm();
            }
            else
            {
                nextForm = DashboardRouter.GetDashboardForUser(user);
            }

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

        public void SaveOperatorNikToHistory(string nik)
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
                MessageBox.Show("Gagal menyimpan history NIK.\n\nDetail:\n" + ex.Message, 
                    "Gagal Menyimpan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ==========================================
        // UI Setup & Styling
        // ==========================================

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

        private void StyleCardPanel()
        {
            pnlCard.Paint += PnlCard_Paint;
            pnlCard.Resize += (s, e) => ApplyCardRegion();
            ApplyCardRegion();
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

            if (KnownRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                txtPassword.Visible = false;
                txtPassword.InputValue = "";
                
                switch (role)
                {
                    case "Operator": txtIdentity.LabelText = "NIK Operator"; break;
                    case "Teknisi":  txtIdentity.LabelText = "Inisial / NIK (Kosongi utk ke Dashboard)"; break;
                    case "Stock":    txtIdentity.LabelText = "NIK / Nama Petugas Stock"; break;
                }
                
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
                txtIdentity.LabelText = "Username";
                txtPassword.Visible = true;
                txtPassword.BringToFront();
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
            catch { }
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
            await _controller.HandleLoginAsync();
        }

        private void OnChildFormClosed(object sender, FormClosedEventArgs e)
        {
            this.ShowForm();
            txtIdentity.InputValue = "";
            txtPassword.InputValue = "";
            drpRole.Focus();
        }

        private void btnExit_Click(object sender, EventArgs e) => Application.Exit();

        // ==========================================
        // Machine Label Logic
        // ==========================================

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

            lblMachineName.Location = new Point(ClientSize.Width - 150, ClientSize.Height - 20);
            lblMachineName.Click += (s, e) => OpenSetupForm();
            this.Controls.Add(lblMachineName);
            lblMachineName.BringToFront();

            this.Resize += (s, e) =>
            {
                if (lblMachineName != null)
                    lblMachineName.Location = new Point(ClientSize.Width - lblMachineName.Width - 10, ClientSize.Height - lblMachineName.Height - 5);
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
                        lblMachineName.Location = new Point(ClientSize.Width - lblMachineName.Width - 10, ClientSize.Height - lblMachineName.Height - 5);

                        string lowerName = name.ToLower();
                        if (lowerName.Contains("teknisi")) drpRole.InputValue = "Teknisi";
                        else if (lowerName.Contains("stock") || lowerName.Contains("gudang")) drpRole.InputValue = "Stock";
                        else drpRole.InputValue = "Operator";
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
            using (var setupForm = new SetupForm())
            {
                if (setupForm.ShowDialog() == DialogResult.OK)
                    LoadMachineNameAsync();
            }
        }
    }
}