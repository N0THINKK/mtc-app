using System;
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.authentication.data.repositories;
using mtc_app.shared.presentation.components;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class SetupForm : AppBaseForm
    {
        private readonly ISetupRepository _repository;

        public SetupForm()
        {
            InitializeComponent();
            _repository = new SetupRepository(); // In a full DI setup, this would be injected
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
                // Register Machine via Repository
                int machineId = await _repository.RegisterMachineAsync(type, area, number);

                // Save Config
                DatabaseHelper.UpdateMachineConfig(machineId.ToString());

                string machineCode = $"{type}-{area}.{number}";
                MessageBox.Show($"Setup Berhasil!\nIdentitas Mesin: {machineCode}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
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