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
        private ComboBox _cmbCategory;
        private ComboBox _cmbActivities;
        private AppButton _btnSave;
        private AppButton _btnCancel;

        private List<ActivityOption> _allActivities = new List<ActivityOption>();

        public int SelectedActivityId { get; private set; }
        public string SelectedActivityName { get; private set; }

        public ActivitySelectionDialog()
        {
            this.Text = "Pilih Alasan Idle / Keluar";
            this.Size = new Size(420, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppColors.Background;

            var lblCategory = new Label
            {
                Text = "Kategori Berhenti:",
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            _cmbCategory = new ComboBox
            {
                Location = new Point(20, 50),
                Size = new Size(360, 30),
                Font = AppFonts.Body,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbCategory.SelectedIndexChanged += CmbCategory_SelectedIndexChanged;

            var lblReason = new Label
            {
                Text = "Pilih Alasan Detail:",
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 95)
            };

            _cmbActivities = new ComboBox
            {
                Location = new Point(20, 125),
                Size = new Size(360, 30),
                Font = AppFonts.Body,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            LoadActivities();

            _btnSave = new AppButton
            {
                Text = "Simpan",
                Type = AppButton.ButtonType.Primary,
                Size = new Size(160, 40),
                Location = new Point(20, 190)
            };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new AppButton
            {
                Text = "Batal",
                Type = AppButton.ButtonType.Secondary,
                Size = new Size(160, 40),
                Location = new Point(220, 190)
            };
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblCategory);
            this.Controls.Add(_cmbCategory);
            this.Controls.Add(lblReason);
            this.Controls.Add(_cmbActivities);
            this.Controls.Add(_btnSave);
            this.Controls.Add(_btnCancel);
        }

        private class ActivityOption { public int Id { get; set; } public string Name { get; set; } public string Category { get; set; } }

        private void LoadActivities()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    try {
                        _allActivities = conn.Query<ActivityOption>("SELECT id as Id, activity_name as Name, category as Category FROM activity_types").ToList();
                    } catch {
                        _allActivities = conn.Query<ActivityOption>("SELECT id as Id, activity_name as Name, 'Uncategorized' as Category FROM activity_types").ToList();
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to offline
                try
                {
                    var offlineRepo = new mtc_app.shared.data.local.OfflineRepository();
                    var cached = offlineRepo.GetActivityTypesFromCache();
                    if (cached != null && cached.Count > 0)
                    {
                        _allActivities = cached.Select(c => new ActivityOption { Id = c.Id, Name = c.Name, Category = c.Category }).ToList();
                    }
                    else
                    {
                        MessageBox.Show("Gagal memuat daftar alasan: Offline server unreachable and no local cache found.");
                        return;
                    }
                }
                catch (Exception fallbackEx)
                {
                    MessageBox.Show("Gagal memuat daftar alasan: " + fallbackEx.Message);
                    return;
                }
            }

            if (_allActivities != null && _allActivities.Count > 0)
            {
                var categories = _allActivities.Select(a => a.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                foreach (var c in categories) _cmbCategory.Items.Add(c);
                
                if (_cmbCategory.Items.Count > 0) _cmbCategory.SelectedIndex = 0;
            }
        }

        private void CmbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            _cmbActivities.Items.Clear();
            if (_cmbCategory.SelectedItem != null)
            {
                string selectedCat = _cmbCategory.SelectedItem.ToString();
                var filtered = _allActivities.Where(a => a.Category == selectedCat).ToList();
                
                _cmbActivities.DisplayMember = "Name";
                _cmbActivities.ValueMember = "Id";
                
                foreach(var item in filtered) _cmbActivities.Items.Add(item);
                if (_cmbActivities.Items.Count > 0) _cmbActivities.SelectedIndex = 0;
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
