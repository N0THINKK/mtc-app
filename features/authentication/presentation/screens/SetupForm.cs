using System;
using System.IO; // Tambahkan ini untuk akses File & Directory
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.authentication.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.data.utils; // Asumsi DatabaseHelper ada disini

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class SetupForm : AppBaseForm
    {
        private readonly ISetupRepository _repository;

        public SetupForm()
        {
            InitializeComponent();
            _repository = new SetupRepository();
            LoadDropdownData();
        }

        private async void LoadDropdownData()
        {
            try
            {
                // Restore UI Visibility
                comboMachineType.Visible = true;
                comboMachineArea.Visible = true;
                txtMachineNumber.Visible = true;
                
                // Fetch Data via Repository
                var types = await _repository.GetMachineTypesAsync();
                var areas = await _repository.GetMachineAreasAsync();

                comboMachineType.SetDropdownItems(types.ToArray());
                comboMachineArea.SetDropdownItems(areas.ToArray());

                // Re-bind Save Button Logic
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
            // Ambil input dari form
            string type = comboMachineType.InputValue.Trim().ToUpper();
            string area = comboMachineArea.InputValue.Trim().ToUpper();
            string number = txtMachineNumber.InputValue.Trim().ToUpper();

            // Validasi Input
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(area) || string.IsNullOrEmpty(number))
            {
                MessageBox.Show("Mohon lengkapi Tipe, Area, dan Nomor Mesin!", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 1. Register Machine via Repository (Dapatkan ID Mesin dari Database)
                int machineId = await _repository.RegisterMachineAsync(type, area, number);

                // 2. Save Config ke Database Lokal Aplikasi (Logic Lama)
                DatabaseHelper.UpdateMachineConfig(machineId.ToString());

                // 3. [BARU] Simpan Config ke File Teks untuk Logger Service
                try 
                {
                    string configFolder = @"C:\MTC_System\Config";
                    string configFile = Path.Combine(configFolder, "machine_id.txt");

                    // Buat folder jika belum ada
                    if (!Directory.Exists(configFolder))
                    {
                        Directory.CreateDirectory(configFolder);
                    }

                    // Tulis ID Mesin ke file
                    // Service Logger akan membaca angka ini untuk tahu dia harus memonitor mesin mana
                    File.WriteAllText(configFile, machineId.ToString());
                }
                catch (Exception fileEx)
                {
                    // Jika gagal tulis file, beri peringatan tapi jangan gagalkan setup utama
                    MessageBox.Show($"Setup Aplikasi berhasil, namun gagal menyimpan konfigurasi untuk Logger Service.\nError: {fileEx.Message}\n\nMohon pastikan folder C:\\MTC_System bisa diakses.", 
                                    "Warning Logger Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // 4. Tampilkan Pesan Sukses
                string machineCode = $"{type}-{area}.{number}";
                MessageBox.Show($"Setup Berhasil!\nIdentitas Mesin: {machineCode}\nID: {machineId}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan konfigurasi: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // Empty handler for designer compatibility
        private void btnSave_Click(object sender, EventArgs e) { }
    }
}