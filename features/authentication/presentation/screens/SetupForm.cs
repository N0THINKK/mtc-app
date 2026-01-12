using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq; // Need to check if Newtonsoft is available or use manual parsing

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class SetupForm : Form
    {
        public SetupForm()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string machineId = txtMachineId.Text.Trim();
            string lineId = txtLineId.Text.Trim();

            if (string.IsNullOrEmpty(machineId) || string.IsNullOrEmpty(lineId))
            {
                MessageBox.Show("Mohon isi semua field!", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Save to appsettings.json
                DatabaseHelper.UpdateMachineConfig(machineId, lineId);

                MessageBox.Show("Konfigurasi tersimpan! Aplikasi akan lanjut.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
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