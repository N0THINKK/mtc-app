using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.micrometer_patrol.data.dtos;
using mtc_app.features.micrometer_patrol.data.repositories;
using mtc_app.shared.data.dtos;
using mtc_app.shared.data.repositories;
using mtc_app.shared.data.session;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.micrometer_patrol.presentation.screens
{
    public class MicrometerPatrolForm : AppBaseForm 
    {
        private readonly IMicrometerPatrolRepository _repository;
        private readonly IMasterDataRepository _masterDataRepository;
        private List<CachedShiftDto> _shifts;
        private List<CachedMachineDto> _machines;
        private List<string> _operators;

        private AppInput txtTanggal;
        private AppInput cmbShift;
        private AppInput cmbNik;
        private AppInput cmbMesin;
        private AppInput txtNotes;
        
        private RadioButton[,] rbPoints = new RadioButton[5, 3]; // 5 rows, 3 columns (OK, NG, NA)
        
        private AppButton btnSimpan;
        private AppButton btnBatal;

        public MicrometerPatrolForm(IMicrometerPatrolRepository repository, IMasterDataRepository masterDataRepository)
        {
            _repository = repository;
            _masterDataRepository = masterDataRepository;
            InitializeUI();
            SetupFormAsync();
            AttachEventHandlers();
        }

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

            // Header Layout
            TableLayoutPanel pnlHeader = new TableLayoutPanel { Dock = DockStyle.Top, Height = 130, ColumnCount = 4, AutoScroll = false, Padding = new Padding(20, 20, 20, 20) };
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this.Controls.Add(pnlHeader);
            pnlHeader.BringToFront();

            txtTanggal = new AppInput { LabelText = "Tanggal", InputType = AppInput.InputTypeEnum.Text, Width = 200, Enabled = false };
            cmbShift = new AppInput { LabelText = "Shift", InputType = AppInput.InputTypeEnum.Dropdown, Width = 200, AllowCustomText = false };
            cmbNik = new AppInput { LabelText = "NIK", InputType = AppInput.InputTypeEnum.Dropdown, Width = 200, AllowCustomText = true };
            cmbMesin = new AppInput { LabelText = "No. Mesin", InputType = AppInput.InputTypeEnum.Dropdown, Width = 200, AllowCustomText = false };

            pnlHeader.Controls.Add(txtTanggal, 0, 0);
            pnlHeader.Controls.Add(cmbShift, 1, 0);
            pnlHeader.Controls.Add(cmbNik, 2, 0);
            pnlHeader.Controls.Add(cmbMesin, 3, 0);

            // Body Layout
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

                // Underline
                Panel line = new Panel { BackColor = AppColors.Border, Height = 1, Width = 880, Location = new Point(0, 60) };
                pnlQuestion.Controls.Add(line);

                pnlBody.Controls.Add(pnlQuestion);
            }

            txtNotes = new AppInput { LabelText = "Keterangan", InputType = AppInput.InputTypeEnum.Text, Width = 880, Multiline = true };
            pnlBody.Controls.Add(txtNotes);
            
            this.Controls.Add(pnlBody);
            pnlBody.BringToFront();

            // Bottom Panel
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = AppColors.CardBackground, Padding = new Padding(30, 15, 30, 15) };
            btnBatal = new AppButton { Text = "Keluar", Type = AppButton.ButtonType.Secondary, Width = 150, Dock = DockStyle.Left };
            btnSimpan = new AppButton { Text = "Simpan", Type = AppButton.ButtonType.Primary, Width = 150, Dock = DockStyle.Right };

            pnlBottom.Controls.Add(btnBatal);
            pnlBottom.Controls.Add(btnSimpan);
            this.Controls.Add(pnlBottom);
            pnlBottom.SendToBack();
        }

        private async void SetupFormAsync()
        {
            txtTanggal.InputValue = DateTime.Now.ToString("dd/MM/yyyy");

            try
            {
                // Load Shifts
                _shifts = await _masterDataRepository.GetShiftsAsync() ?? new List<CachedShiftDto>();
                if (_shifts.Count > 0)
                {
                    cmbShift.SetDropdownItems(_shifts.Select(s => s.ShiftName).ToArray());
                    cmbShift.InputValue = _shifts[0].ShiftName;
                }

                // Load NIK
                if (UserSession.CurrentUser != null && UserSession.CurrentUser.RoleId != 1)
                {
                    // Technician -> No Dropdown, just showing initials disabled
                    cmbNik.InputType = AppInput.InputTypeEnum.Text;
                    cmbNik.AllowCustomText = false;
                    cmbNik.Enabled = false;
                    cmbNik.InputValue = UserSession.CurrentUser.Username;
                }
                else
                {
                    _operators = await _masterDataRepository.GetOperatorsAsync() ?? new List<string>();
                    var nikList = new List<string>(_operators);

                    if (UserSession.CurrentUser != null)
                    {
                        string userValue = string.IsNullOrWhiteSpace(UserSession.CurrentUser.Nik) 
                            ? UserSession.CurrentUser.Username 
                            : UserSession.CurrentUser.Nik;

                        if (!nikList.Contains(userValue)) nikList.Insert(0, userValue);

                        cmbNik.SetDropdownItems(nikList.ToArray());
                        cmbNik.InputValue = userValue;
                    }
                    else
                    {
                        cmbNik.SetDropdownItems(nikList.ToArray());
                        if (nikList.Count > 0) cmbNik.InputValue = nikList[0];
                    }
                }

                // Load Machines
                _machines = await _masterDataRepository.GetMachinesAsync() ?? new List<CachedMachineDto>();
                if (_machines.Count > 0)
                {
                    cmbMesin.SetDropdownItems(_machines.Select(m => m.Code).ToArray());
                    cmbMesin.InputValue = _machines[0].Code;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data master: " + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AttachEventHandlers()
        {
            btnSimpan.Click += BtnSimpan_Click;
            btnBatal.Click += BtnBatal_Click;
        }

        private async void BtnSimpan_Click(object sender, EventArgs e)
        {
            var selectedShiftName = cmbShift.InputValue;
            int shiftId = _shifts?.FirstOrDefault(s => s.ShiftName == selectedShiftName)?.ShiftId ?? 0;

            var selectedMachineCode = cmbMesin.InputValue;
            int machineId = _machines?.FirstOrDefault(m => m.Code == selectedMachineCode)?.MachineId ?? 0;

            if (shiftId == 0 || machineId == 0)
            {
                MessageBox.Show("Shift atau Mesin tidak valid!", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var patrolData = new MicrometerPatrolDto
            {
                PatrolDate = DateTime.Now,
                ShiftId = shiftId,
                UserId = (int)(UserSession.CurrentUser?.UserId ?? 0),
                MachineId = machineId,
                Point1 = GetRadioValue(0),
                Point2 = GetRadioValue(1),
                Point3 = GetRadioValue(2),
                Point4 = GetRadioValue(3),
                Point5 = GetRadioValue(4),
                Notes = txtNotes.InputValue?.Trim() ?? ""
            };

            btnSimpan.Enabled = false;
            btnSimpan.Text = "Menyimpan...";

            try
            {
                bool isSuccess = await _repository.SavePatrolAsync(patrolData);

                if (isSuccess)
                {
                    ToastNotification.ShowSuccess("Data patroli mikrometer berhasil disimpan!");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Gagal menyimpan data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSimpan.Enabled = true;
                btnSimpan.Text = "Simpan";
            }
        }

        private string GetRadioValue(int rowIndex)
        {
            if (rbPoints[rowIndex, 0].Checked) return "OK";
            if (rbPoints[rowIndex, 1].Checked) return "NG";
            if (rbPoints[rowIndex, 2].Checked) return "NA";
            return "OK";
        }

        private void BtnBatal_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
