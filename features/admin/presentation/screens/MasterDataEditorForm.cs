using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.admin.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.screens
{
    public class MasterDataEditorForm : Form
    {
        private readonly IAdminRepository _repository;
        private string _category;
        private string _subCategory;
        private IDictionary<string, object> _rowData;
        private bool _isEditMode;
        private string[] _extraData; // Variabel baru untuk menampung Tipe Mesin dari DB

        private FlowLayoutPanel pnlForm;
        private AppLabel lblTitle;

        private Dictionary<string, Control> _inputControls = new Dictionary<string, Control>();

        // Constructor sekarang menerima extraData
        public MasterDataEditorForm(IAdminRepository repository, string category, string subCategory = "", object rowData = null, string[] extraData = null)
        {
            _repository = repository;
            _category = category;
            _subCategory = subCategory;
            _rowData = rowData as IDictionary<string, object>; 
            _isEditMode = _rowData != null;
            _extraData = extraData;

            SetupUI();
            GenerateDynamicFields();
        }

        private void SetupUI()
        {
            this.Size = new Size(500, 680); 
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppColors.Surface;

            string titleName = (_category == "Problem" || _category == "Checksheet") ? _subCategory : _category;
            this.Text = _isEditMode ? $"Edit Data {titleName}" : $"Tambah Data {titleName}";

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(24, 24, 24, 0) };
            lblTitle = new AppLabel { Text = this.Text, Font = AppFonts.Header2, ForeColor = AppColors.TextPrimary, AutoSize = true };
            pnlHeader.Controls.Add(lblTitle);

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(24, 15, 24, 15), BackColor = AppColors.CardBackground };
            AppButton btnCancel = new AppButton { Text = "Batal", Type = AppButton.ButtonType.Secondary, Width = 120, Dock = DockStyle.Left };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            AppButton btnSave = new AppButton { Text = "Simpan Data", Type = AppButton.ButtonType.Primary, Width = 150, Dock = DockStyle.Right };
            btnSave.Click += BtnSave_Click;

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);

            pnlForm = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(24, 20, 24, 20), AutoScroll = true };

            this.Controls.Add(pnlForm);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
            pnlForm.BringToFront();
        }

        private void GenerateDynamicFields()
        {
            if (_category == "User")
            {
                AddInput("Full Name", "full_name", GetValue("full_name"));
                AddInput("NIK / Inisial", "nik", GetValue("nik"));
                AddInput("Username", "username", GetValue("username"));
                AddComboBox("Role", "role", new[] { "Operator", "Teknisi", "Stock Control", "Admin", "Group Leader" }, GetValue("role"));
                
                if (_isEditMode) {
                    AddPasswordInput("Password Lama (Wajib jika ganti password)", "old_password");
                    AddPasswordInput("Password Baru (Kosongkan jika tidak diganti)", "new_password");
                } else {
                    AddPasswordInput("Password (Default: 123456 jika kosong)", "new_password");
                }
            }
            else if (_category == "Mesin")
            {
                AddInput("Kode Mesin", "kode", GetValue("kode"));
                AddInput("Tipe Mesin", "nama", GetValue("nama"));
                AddInput("Area", "area", GetValue("area"));
                AddInput("Kondisi", "kondisi", GetValue("kondisi"));
            }
            else if (_category == "Sparepart")
            {
                AddInput("Kode Part", "kode", GetValue("kode"));
                AddInput("Nama Sparepart", "nama", GetValue("nama"));
                AddInput("Stok Tersedia", "stok", GetValue("stok"), isNumeric: true);
                AddInput("Lokasi Rak", "lokasi", GetValue("lokasi"));
            }
            else if (_category == "Problem")
            {
                AddInput($"Nama {_subCategory}", "nama", GetValue("nama"));
            }
            else if (_category == "Checksheet")
            {
                // Gunakan daftar asli dari Database yang dikirim lewat ekstraData. Jika kosong, beri nilai fallback.
                string[] tipeMesinTersedia = (_extraData != null && _extraData.Length > 0) ? _extraData : new[] { "Belum ada template mesin" };
                
                AddComboBoxEditable("Tipe Mesin (Ketik baru atau pilih yang ada)", "tipe_mesin", tipeMesinTersedia, GetValue("tipe_mesin"));
                AddInput("Item Pengecekan", "item_pengecekan", GetValue("item_pengecekan"));
                AddInput("Standar & Judgment", "standar", GetValue("standar"));
                AddInput("Metode Pengecekan", "metode", GetValue("metode"));
                AddComboBox("Tipe Input", "tipe_input", new[] { "Pilihan (OK/NG/NA)", "Angka/Teks" }, GetValue("tipe_input") == "numeric/text" ? "Angka/Teks" : "Pilihan (OK/NG/NA)");
            }
        }

        private void AddInput(string labelText, string fieldKey, string value, bool isNumeric = false)
        {
            Panel pnlContainer = CreateInputContainer(labelText);
            TextBox txtInput = new TextBox { Text = value, Dock = DockStyle.Bottom, Font = AppFonts.Body, Height = 35, BorderStyle = BorderStyle.FixedSingle };
            if (isNumeric) txtInput.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            pnlContainer.Controls.Add(txtInput); pnlForm.Controls.Add(pnlContainer); _inputControls.Add(fieldKey, txtInput); 
        }

        private void AddPasswordInput(string labelText, string fieldKey)
        {
            Panel pnlContainer = CreateInputContainer(labelText);
            TextBox txtInput = new TextBox { Text = "", Dock = DockStyle.Bottom, Font = AppFonts.Body, Height = 35, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };
            pnlContainer.Controls.Add(txtInput); pnlForm.Controls.Add(pnlContainer); _inputControls.Add(fieldKey, txtInput); 
        }

        private void AddComboBox(string labelText, string fieldKey, string[] options, string selectedValue)
        {
            Panel pnlContainer = CreateInputContainer(labelText);
            ComboBox cmbInput = new ComboBox { Dock = DockStyle.Bottom, Font = AppFonts.Body, Height = 35, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            cmbInput.Items.AddRange(options);
            if (!string.IsNullOrEmpty(selectedValue) && cmbInput.Items.Contains(selectedValue)) cmbInput.SelectedItem = selectedValue;
            else if (options.Length > 0) cmbInput.SelectedIndex = 0;
            pnlContainer.Controls.Add(cmbInput); pnlForm.Controls.Add(pnlContainer); _inputControls.Add(fieldKey, cmbInput); 
        }

        private void AddComboBoxEditable(string labelText, string fieldKey, string[] options, string selectedValue)
        {
            Panel pnlContainer = CreateInputContainer(labelText);
            ComboBox cmbInput = new ComboBox { Dock = DockStyle.Bottom, Font = AppFonts.Body, Height = 35, DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Flat };
            cmbInput.Items.AddRange(options);
            if (!string.IsNullOrEmpty(selectedValue)) 
            {
                cmbInput.Text = selectedValue; // Gunakan Text agar bisa menerima nilai yang tidak ada di list
                if (cmbInput.Items.Contains(selectedValue)) cmbInput.SelectedItem = selectedValue;
            }
            else if (options.Length > 0) cmbInput.SelectedIndex = 0;
            pnlContainer.Controls.Add(cmbInput); pnlForm.Controls.Add(pnlContainer); _inputControls.Add(fieldKey, cmbInput); 
        }

        private Panel CreateInputContainer(string labelText)
        {
            Panel pnl = new Panel { Width = 430, Height = 65, Margin = new Padding(0, 0, 0, 15) };
            AppLabel lbl = new AppLabel { Text = labelText, Font = AppFonts.BodySmall, ForeColor = AppColors.TextSecondary, Dock = DockStyle.Top };
            pnl.Controls.Add(lbl); return pnl;
        }

        private string GetValue(string key) { return (_isEditMode && _rowData.ContainsKey(key) && _rowData[key] != null) ? _rowData[key].ToString() : ""; }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            var dataToSave = new Dictionary<string, object>();
            if (_isEditMode && _rowData.ContainsKey("id")) dataToSave["id"] = _rowData["id"];

            foreach (var input in _inputControls)
            {
                string val = input.Value is ComboBox cmb ? cmb.Text : input.Value.Text;
                if (input.Key == "old_password" || input.Key == "new_password") { dataToSave[input.Key] = val; continue; }
                if (string.IsNullOrWhiteSpace(val)) { MessageBox.Show($"Kolom '{input.Key}' tidak boleh kosong!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                dataToSave[input.Key] = val;
            }

            if (_isEditMode && _category == "User")
            {
                bool isNewPassFilled = !string.IsNullOrWhiteSpace(dataToSave["new_password"]?.ToString());
                bool isOldPassFilled = !string.IsNullOrWhiteSpace(dataToSave["old_password"]?.ToString());
                if (isNewPassFilled && !isOldPassFilled) { MessageBox.Show("Untuk mengubah password, Anda WAJIB memasukkan Password Lama!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                bool success = await _repository.SaveMasterDataAsync(_category, _subCategory, _isEditMode, dataToSave);
                if (success) {
                    MessageBox.Show("Data berhasil disimpan!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK; 
                } else MessageBox.Show("Penyimpanan gagal, data tidak tersimpan.", "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Gagal Disimpan", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { this.Cursor = Cursors.Default; }
        }
    }
}