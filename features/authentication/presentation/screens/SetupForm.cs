using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using Dapper; // Tambahkan ini untuk akses database langsung
using mtc_app.features.authentication.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.data.utils;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class SetupForm : AppBaseForm
    {
        private readonly ISetupRepository _repository;
        
        // Komponen UI Dinamis untuk Template Checksheet
        private Label lblTemplate;
        private ComboBox cmbTemplate;
        private Dictionary<string, int> _templateMap = new Dictionary<string, int>();
        
        // Timer untuk mendeteksi perubahan Tipe Mesin (Tanpa perlu event desainer)
        private Timer _typeWatcherTimer;
        private string _lastType = "";

        public SetupForm()
        {
            InitializeComponent();
            _repository = new SetupRepository();
            
            InitTemplateUI();
            LoadDropdownData();
            
            this.Resize += (s, e) => RepositionControls();
        }

        private void InitTemplateUI()
        {
            // 1. Buat Label dan ComboBox Template
            lblTemplate = new Label 
            { 
                Text = "Template Checksheet (Khusus AC90/AC95):", 
                AutoSize = true, 
                Visible = false, 
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.DimGray
            };
            
            cmbTemplate = new ComboBox 
            { 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                Visible = false, 
                Font = new Font("Segoe UI", 12F), 
                Size = new Size(300, 35) 
            };

            // 2. Tambahkan ke Form setelah UI selesai di-load
            this.Load += (s, e) => 
            {
                if (txtMachineNumber.Parent != null)
                {
                    txtMachineNumber.Parent.Controls.Add(lblTemplate);
                    txtMachineNumber.Parent.Controls.Add(cmbTemplate);
                    RepositionControls(); // Atur posisi awal
                }
            };

            // 3. Pasang Watcher untuk mendeteksi ketikan/pilihan di txtMachineType
            _typeWatcherTimer = new Timer { Interval = 500 };
            _typeWatcherTimer.Tick += (s, e) => 
            {
                string currentType = comboMachineType.InputValue?.Trim().ToUpper() ?? "";
                if (currentType != _lastType)
                {
                    _lastType = currentType;
                    HandleMachineTypeChange(currentType);
                }
            };
            _typeWatcherTimer.Start();
        }

        private void RepositionControls()
        {
            if (txtMachineNumber == null || btnSave == null || pnlMain == null) return;

            // Jika template muncul, geser tombol save ke bawahnya
            if (lblTemplate.Visible)
            {
                lblTemplate.Location = new Point(txtMachineNumber.Left, txtMachineNumber.Bottom + 15);
                cmbTemplate.Location = new Point(txtMachineNumber.Left, lblTemplate.Bottom + 5);
                btnSave.Top = cmbTemplate.Bottom + 25;
            }
            else
            {
                // Jika tidak, tombol save langsung di bawah txtMachineNumber
                btnSave.Top = txtMachineNumber.Bottom + 25;
            }

            // Posisikan tombol exit selalu di tengah bawah btnSave
            if (btnExit != null)
            {
                btnExit.Top = btnSave.Bottom + 15;
                btnExit.Left = (pnlMain.Width - btnExit.Width) / 2;
            }
        }

        private void HandleMachineTypeChange(string typeName)
        {
            if (typeName.Contains("AC90") || typeName.Contains("AC95"))
            {
                lblTemplate.Visible = true;
                cmbTemplate.Visible = true;
                LoadTemplatesFromDb(typeName);
            }
            else
            {
                lblTemplate.Visible = false;
                cmbTemplate.Visible = false;
                cmbTemplate.Items.Clear();
                _templateMap.Clear();
            }
            RepositionControls(); // Susun ulang layout tombol
        }

        private void LoadTemplatesFromDb(string typeName)
        {
            cmbTemplate.Items.Clear();
            _templateMap.Clear();
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    // Ambil daftar template yang terikat dengan tipe mesin ini
                    var templates = conn.Query(
                        @"SELECT t.template_id, t.template_name 
                          FROM checksheet_templates t
                          JOIN machine_types mt ON t.machine_type_id = mt.type_id
                          WHERE mt.type_name = @TypeName", new { TypeName = typeName }).ToList();
                    
                    foreach (var t in templates)
                    {
                        // Hapus embel-embel tipe mesin (misal 'AC95 ' atau ' AC90') dari nama template agar UI lebih bersih
                        string displayName = t.template_name;
                        displayName = displayName.Replace("AC95 ", "").Replace(" AC95", "").Replace(" (AC95)", "")
                                                 .Replace("AC90 ", "").Replace(" AC90", "").Replace(" (AC90)", "").Trim();

                        cmbTemplate.Items.Add(displayName);
                        _templateMap[displayName] = (int)t.template_id;
                    }
                    
                    if (cmbTemplate.Items.Count > 0)
                        cmbTemplate.SelectedIndex = 0;
                }
            }
            catch { /* Abaikan jika error koneksi */ }
        }

        private async void LoadDropdownData()
        {
            try
            {
                comboMachineType.Visible = true;
                comboMachineArea.Visible = true;
                txtMachineNumber.Visible = true;
                
                var types = await _repository.GetMachineTypesAsync();
                var areas = await _repository.GetMachineAreasAsync();

                comboMachineType.SetDropdownItems(types.ToArray());
                comboMachineArea.SetDropdownItems(areas.ToArray());

                btnSave.Click -= btnSave_Click;
                btnSave.Click += BtnSave_Click_Logic;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnSave_Click_Logic(object sender, EventArgs e)
        {
            string type = comboMachineType.InputValue.Trim().ToUpper();
            string area = comboMachineArea.InputValue.Trim().ToUpper();
            string number = txtMachineNumber.InputValue.Trim().ToUpper();

            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(area) || string.IsNullOrEmpty(number))
            {
                MessageBox.Show("Mohon lengkapi Tipe, Area, dan Nomor Mesin!", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 1. Simpan Mesin Utama
                int machineId = await _repository.RegisterMachineAsync(type, area, number);

                // 2. [FITUR BARU] Update current_template_id ke tabel machines
                using (var conn = DatabaseHelper.GetConnection())
                {
                    if (cmbTemplate.Visible && cmbTemplate.SelectedIndex >= 0)
                    {
                        // Jika AC90/AC95, simpan template yang dipilih SPV
                        if (_templateMap.TryGetValue(cmbTemplate.Text, out int templateId))
                        {
                            conn.Execute("UPDATE machines SET current_template_id = @TplId WHERE machine_id = @MacId", 
                                new { TplId = templateId, MacId = machineId });
                        }
                    }
                    else if (!type.Contains("AC90") && !type.Contains("AC95"))
                    {
                        // Jika mesin lain (AC81 dsb), otomatis pasangkan dengan template default mereka
                        var defaultTpl = conn.QueryFirstOrDefault<int?>(
                            @"SELECT template_id FROM checksheet_templates ct 
                              JOIN machine_types mt ON ct.machine_type_id = mt.type_id 
                              WHERE mt.type_name = @Type LIMIT 1", new { Type = type });
                        
                        if (defaultTpl.HasValue)
                        {
                            conn.Execute("UPDATE machines SET current_template_id = @TplId WHERE machine_id = @MacId", 
                                new { TplId = defaultTpl.Value, MacId = machineId });
                        }
                    }
                }

                // 3. Simpan Config Lokal
                DatabaseHelper.UpdateMachineConfig(machineId.ToString());

                try 
                {
                    string configFolder = @"C:\MTC_System\Config";
                    string configFile = Path.Combine(configFolder, "machine_id.txt");
                    if (!Directory.Exists(configFolder)) Directory.CreateDirectory(configFolder);
                    File.WriteAllText(configFile, machineId.ToString());
                }
                catch { }

                MessageBox.Show($"Setup Berhasil!\nIdentitas Mesin: {type}-{area}.{number}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan konfigurasi: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void btnSave_Click(object sender, EventArgs e) { }
        private void btnExit_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}