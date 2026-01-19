using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq; // Need to check if Newtonsoft is available or use manual parsing
using mtc_app.shared.presentation.components;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class SetupForm : AppBaseForm
    {
        public SetupForm()
        {
            InitializeComponent();
            
            // Hardcoded initial data
            comboMachineType.SetDropdownItems(new string[] { "AC90", "AC80", "JAM", "TRX", "BSH" });
            comboMachineArea.SetDropdownItems(new string[] { "TRX", "BSH", "JKT" });
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string type = comboMachineType.InputValue.Trim();
            string area = comboMachineArea.InputValue.Trim();
            string number = txtMachineNumber.InputValue.Trim();

            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(area) || string.IsNullOrEmpty(number))
            {
                MessageBox.Show("Mohon isi semua field!", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Format: KODE-AREA.URUT (Contoh: AC90-TRX.01)
                string machineCode = $"{type}-{area}.{number}";
                string machineName = $"{type} {area} {number}"; // Readable name

                // Save to appsettings.json
                DatabaseHelper.UpdateMachineConfig(machineCode);

                MessageBox.Show($"Konfigurasi tersimpan!\nID Mesin: {machineCode}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan konfigurasi: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}