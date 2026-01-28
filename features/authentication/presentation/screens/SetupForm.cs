using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class SetupForm : AppBaseForm
    {
        public SetupForm()
        {
            InitializeComponent();
            LoadDropdownData();
        }

        private async void LoadDropdownData()
        {
            try
            {
                // Restore UI Visibility (in case hidden by previous edits)
                comboMachineType.Visible = true;
                comboMachineArea.Visible = true;
                txtMachineNumber.Visible = true;
                
                // Clear existing items (if any dummy data)
                comboMachineType.SetDropdownItems(new string[] { });
                comboMachineArea.SetDropdownItems(new string[] { });

                using (var conn = DatabaseHelper.GetConnection())
                {
                    // 1. Fetch Distinct Types
                    var types = await conn.QueryAsync<string>("SELECT DISTINCT machine_type FROM machines ORDER BY machine_type");
                    comboMachineType.SetDropdownItems(types.ToArray());

                    // 2. Fetch Distinct Areas
                    var areas = await conn.QueryAsync<string>("SELECT DISTINCT machine_area FROM machines ORDER BY machine_area");
                    comboMachineArea.SetDropdownItems(areas.ToArray());
                }

                // Re-bind Save Button Logic
                btnSave.Click -= btnSave_Click;
                btnSave.Click += BtnSave_Click_Logic;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data Tipe/Area dari database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                using (var conn = DatabaseHelper.GetConnection())
                {
                    // 1. Cek apakah kombinasi mesin ini sudah ada di DB?
                    string sqlCheck = @"SELECT machine_id FROM machines 
                                      WHERE machine_type = @Type 
                                      AND machine_area = @Area 
                                      AND machine_number = @Number";
                    
                    var machineId = await conn.QueryFirstOrDefaultAsync<int?>(sqlCheck, new { Type = type, Area = area, Number = number });

                    if (machineId == null || machineId == 0)
                    {
                        // 2. Jika belum ada, BUAT BARU (Insert)
                        // Karena nomor mesin dinamis dan user baru input sekarang
                        string sqlInsert = @"INSERT INTO machines (machine_type, machine_area, machine_number, current_status_id) 
                                           VALUES (@Type, @Area, @Number, 1);
                                           SELECT LAST_INSERT_ID();";
                        
                        machineId = await conn.QuerySingleAsync<int>(sqlInsert, new { Type = type, Area = area, Number = number });
                        
                        // Buat juga row log kosong agar grafik bisa baca
                        string sqlLog = @"INSERT IGNORE INTO machine_process_logs (machine_id, last_updated) VALUES (@Id, NOW())";
                        await conn.ExecuteAsync(sqlLog, new { Id = machineId });
                    }

                    // 3. Simpan ID ke Config
                    DatabaseHelper.UpdateMachineConfig(machineId.ToString());

                    string machineCode = $"{type}-{area}.{number}";
                    MessageBox.Show($"Setup Berhasil!\nIdentitas Mesin: {machineCode}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
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