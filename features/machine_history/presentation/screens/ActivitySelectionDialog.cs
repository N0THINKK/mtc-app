using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public class ActivitySelectionDialog : Form
    {
        private ComboBox _cmbActivities;
        private AppButton _btnSave;
        private AppButton _btnCancel;

        public int SelectedActivityId { get; private set; }
        public string SelectedActivityName { get; private set; }

        public ActivitySelectionDialog()
        {
            this.Text = "Pilih Alasan Idle / Keluar";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppColors.Background;

            var lbl = new Label
            {
                Text = "Alasan Mesin Berhenti / Operator Keluar:",
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            _cmbActivities = new ComboBox
            {
                Location = new Point(20, 60),
                Size = new Size(340, 30),
                Font = AppFonts.Body,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            LoadActivities();

            _btnSave = new AppButton
            {
                Text = "Simpan",
                Type = AppButton.ButtonType.Primary,
                Size = new Size(160, 40),
                Location = new Point(20, 130)
            };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new AppButton
            {
                Text = "Batal",
                Type = AppButton.ButtonType.Secondary,
                Size = new Size(160, 40),
                Location = new Point(200, 130)
            };
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lbl);
            this.Controls.Add(_cmbActivities);
            this.Controls.Add(_btnSave);
            this.Controls.Add(_btnCancel);
        }

        private class ActivityOption { public int Id { get; set; } public string Name { get; set; } }

        private void LoadActivities()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var list = conn.Query<ActivityOption>("SELECT id as Id, activity_name as Name FROM activity_types").ToList();
                    
                    _cmbActivities.DisplayMember = "Name";
                    _cmbActivities.ValueMember = "Id";
                    foreach(var item in list) _cmbActivities.Items.Add(item);
                    if (_cmbActivities.Items.Count > 0) _cmbActivities.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat daftar alasan: " + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_cmbActivities.SelectedItem != null && _cmbActivities.SelectedItem is ActivityOption opt)
            {
                SelectedActivityId = opt.Id;
                SelectedActivityName = opt.Name;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Pilih alasan terlebih dahulu.");
            }
        }
    }
}
