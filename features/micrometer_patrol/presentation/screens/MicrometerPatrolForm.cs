using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.micrometer_patrol.data.repositories;
using mtc_app.features.micrometer_patrol.presentation.controllers;
using mtc_app.shared.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.micrometer_patrol.presentation.screens
{
    public class MicrometerPatrolForm : AppBaseForm, IMicrometerPatrolView
    {
        private readonly MicrometerPatrolController _controller;
        private readonly IMicrometerPatrolRepository _repository;

        private AppInput txtTanggal;
        private AppInput cmbShift;
        private AppInput cmbNik;
        private AppInput cmbMesin;
        private AppInput txtNotes;
        
        private RadioButton[,] rbPoints = new RadioButton[5, 3];
        
        private AppButton btnSimpan;
        private AppButton btnBatal;

        public MicrometerPatrolForm(IMicrometerPatrolRepository repository, IMasterDataRepository masterDataRepository)
        {
            _repository = repository;
            _controller = new MicrometerPatrolController(this, repository, masterDataRepository);
            
            InitializeUI();
            this.Shown += async (s, e) => await _controller.LoadInitialDataAsync();
        }

        // ==========================================
        // IMicrometerPatrolView Implementation
        // ==========================================

        public string SelectedShift { get => cmbShift.InputValue; set => cmbShift.InputValue = value; }
        public string SelectedMachine { get => cmbMesin.InputValue; set => cmbMesin.InputValue = value; }
        public string SelectedNik { get => cmbNik.InputValue; set => cmbNik.InputValue = value; }
        public string Notes => txtNotes.InputValue?.Trim() ?? "";

        public void PopulateShifts(string[] shifts)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => PopulateShifts(shifts))); return; }
            cmbShift.SetDropdownItems(shifts);
        }

        public void PopulateMachines(string[] machines)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => PopulateMachines(machines))); return; }
            cmbMesin.SetDropdownItems(machines);
        }

        public void PopulateOperators(string[] operators)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => PopulateOperators(operators))); return; }
            cmbNik.SetDropdownItems(operators);
        }

        public void LockMachine(bool isLocked)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => LockMachine(isLocked))); return; }
            cmbMesin.Enabled = !isLocked;
        }

        public void SetTechnicianMode(string username)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => SetTechnicianMode(username))); return; }
            cmbNik.InputType = AppInput.InputTypeEnum.Text;
            cmbNik.AllowCustomText = false;
            cmbNik.Enabled = false;
            cmbNik.InputValue = username;
        }

        public string GetPointValue(int index)
        {
            if (this.InvokeRequired) { return (string)this.Invoke(new Func<string>(() => GetPointValue(index))); }
            if (rbPoints[index, 0].Checked) return "OK";
            if (rbPoints[index, 1].Checked) return "NG";
            if (rbPoints[index, 2].Checked) return "NA";
            return "OK";
        }

        public void SetBusyState(bool isBusy)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => SetBusyState(isBusy))); return; }
            btnSimpan.Enabled = !isBusy;
            btnSimpan.Text = isBusy ? "Menyimpan..." : "Simpan";
            this.Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
        }

        public void ShowError(string message, string title = "Error")
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => ShowError(message, title))); return; }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowWarning(string message, string title = "Peringatan")
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => ShowWarning(message, title))); return; }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public void ShowSuccess(string message, string title = "Sukses")
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => ShowSuccess(message, title))); return; }
            ToastNotification.ShowSuccess(message);
        }

        public void CloseForm(bool success)
        {
            if (this.InvokeRequired) { this.BeginInvoke(new Action(() => CloseForm(success))); return; }
            this.DialogResult = success ? DialogResult.OK : DialogResult.Cancel;
            this.Close();
        }

        // ==========================================
        // UI Layout
        // ==========================================

        private void InitializeUI()
        {
            this.Text = "Form Patroli Mikrometer";
            this.Size = new Size(950, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTitle = new Label { Text = "PATROLI HARIAN MIKROMETER", Font = AppFonts.Header1, ForeColor = AppColors.TextPrimary, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top, Height = 60 };
            this.Controls.Add(lblTitle);

            TableLayoutPanel pnlHeader = new TableLayoutPanel { Dock = DockStyle.Top, Height = 130, ColumnCount = 4, AutoScroll = false, Padding = new Padding(20, 20, 20, 20) };
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this.Controls.Add(pnlHeader);
            pnlHeader.BringToFront();

            txtTanggal = new AppInput { LabelText = "Tanggal", InputType = AppInput.InputTypeEnum.Text, Width = 200, Enabled = false, InputValue = DateTime.Now.ToString("dd/MM/yyyy") };
            cmbShift = new AppInput { LabelText = "Shift", InputType = AppInput.InputTypeEnum.Dropdown, Width = 200, AllowCustomText = false };
            cmbNik = new AppInput { LabelText = "NIK", InputType = AppInput.InputTypeEnum.Dropdown, Width = 200, AllowCustomText = true };
            cmbMesin = new AppInput { LabelText = "No. Mesin", InputType = AppInput.InputTypeEnum.Dropdown, Width = 200, AllowCustomText = false };

            pnlHeader.Controls.Add(txtTanggal, 0, 0);
            pnlHeader.Controls.Add(cmbShift, 1, 0);
            pnlHeader.Controls.Add(cmbNik, 2, 0);
            pnlHeader.Controls.Add(cmbMesin, 3, 0);

            FlowLayoutPanel pnlBody = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(20, 10, 20, 20) };
            
            string[] questions = new string[] 
            {
                "No.1 Ada Nomer Registrasi dan tidak Expired ( Visual Cek )",
                "No.2 Angka terbaca dengan jelas ( Visual cek, Tidak muncul huruf \"B\", \"H\", \"INS\", atau\"P\")",
                "No.3 Zero setting OK (Visual cek, Layar menunjukkan \"0,000\")",
                "No.4 Kondisi Thimble, Anvil dan Spindle OK (Visual dan sentuh)",
                "No.5 Baut Pengunci tidak longgar/Dol (Visual cek, Lihat tanda pada Screw)"
            };

            for (int i = 0; i < 5; i++)
            {
                Panel pnlQuestion = new Panel { Width = 880, Height = 65 };
                Label lblQ = new Label { Text = questions[i], Font = AppFonts.Subtitle, ForeColor = AppColors.TextPrimary, AutoSize = true, Location = new Point(5, 5) };
                
                rbPoints[i, 0] = new RadioButton { Text = "OK", Location = new Point(20, 30), AutoSize = true, Font = AppFonts.Body, ForeColor = AppColors.TextPrimary, Checked = true };
                rbPoints[i, 1] = new RadioButton { Text = "NG", Location = new Point(350, 30), AutoSize = true, Font = AppFonts.Body, ForeColor = AppColors.TextPrimary };
                rbPoints[i, 2] = new RadioButton { Text = "Tidak ada/ Tidak Pakai", Location = new Point(650, 30), AutoSize = true, Font = AppFonts.Body, ForeColor = AppColors.TextPrimary };

                pnlQuestion.Controls.Add(lblQ);
                pnlQuestion.Controls.Add(rbPoints[i, 0]);
                pnlQuestion.Controls.Add(rbPoints[i, 1]);
                pnlQuestion.Controls.Add(rbPoints[i, 2]);

                Panel line = new Panel { BackColor = AppColors.Border, Height = 1, Width = 880, Location = new Point(0, 60) };
                pnlQuestion.Controls.Add(line);
                pnlBody.Controls.Add(pnlQuestion);
            }

            txtNotes = new AppInput { LabelText = "Keterangan", InputType = AppInput.InputTypeEnum.Text, Width = 880, Multiline = true };
            pnlBody.Controls.Add(txtNotes);
            
            this.Controls.Add(pnlBody);
            pnlBody.BringToFront();

            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = AppColors.CardBackground, Padding = new Padding(30, 15, 30, 15) };
            btnBatal = new AppButton { Text = "Keluar", Type = AppButton.ButtonType.Secondary, Width = 150, Dock = DockStyle.Left };
            
            Panel pnlRight = new Panel { Dock = DockStyle.Right, Width = 320, BackColor = Color.Transparent };
            
            AppButton btnHistory = new AppButton { Text = "History", Type = AppButton.ButtonType.Secondary, Width = 150, Location = new Point(0, 0) };
            btnHistory.Click += BtnHistory_Click;
            
            btnSimpan = new AppButton { Text = "Simpan", Type = AppButton.ButtonType.Primary, Width = 150, Location = new Point(170, 0) };
            btnSimpan.Click += async (s, e) => await _controller.SavePatrolAsync();
            btnBatal.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            
            pnlRight.Controls.Add(btnHistory);
            pnlRight.Controls.Add(btnSimpan);
            pnlBottom.Controls.Add(btnBatal);
            pnlBottom.Controls.Add(pnlRight);
            this.Controls.Add(pnlBottom);
            pnlBottom.SendToBack();
        }

        private void BtnHistory_Click(object sender, EventArgs e)
        {
            this.Hide();
            using (var historyForm = new MicrometerPatrolHistoryForm(_repository))
            {
                historyForm.ShowDialog(this);
            }
            this.Show();
        }
    }
}
