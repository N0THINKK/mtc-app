using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.features.admin.data.repositories;

namespace mtc_app.features.admin.presentation.screens
{
    public class MasterDataEditorForm : Form
    {
        private string _category;
        private string _subCategory;
        private IDictionary<string, object> _rowData;
        private bool _isEditMode;

        private FlowLayoutPanel pnlForm;
        private AppLabel lblTitle;

        private Dictionary<string, Control> _inputControls = new Dictionary<string, Control>();

        public MasterDataEditorForm(string category, string subCategory = "", object rowData = null)
        {
            _category = category;
            _subCategory = subCategory;
            _rowData = rowData as IDictionary<string, object>; // Dapper menyertakan fitur cast row ke Dictionary
            _isEditMode = _rowData != null;

            SetupUI();
            GenerateDynamicFields();
        }

        private void SetupUI()
        {
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppColors.Surface;

            string titleName = _category == "Problem" ? _subCategory : _category;
            this.Text = _isEditMode ? $"Edit Data {titleName}" : $"Tambah Data {titleName}";

            // 1. Header
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(24, 24, 24, 0) };
            lblTitle = new AppLabel 
            { 
                Text = this.Text, 
                Font = AppFonts.Header2, 
                ForeColor = AppColors.TextPrimary, 
                AutoSize = true 
            };
            pnlHeader.Controls.Add(lblTitle);

            // 2. Footer (Tombol)
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(24, 15, 24, 15), BackColor = AppColors.CardBackground };
            
            AppButton btnCancel = new AppButton { Text = "Batal", Type = AppButton.ButtonType.Secondary, Width = 120, Dock = DockStyle.Left };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            AppButton btnSave = new AppButton { Text = "Simpan Data", Type = AppButton.ButtonType.Primary, Width = 150, Dock = DockStyle.Right };
            btnSave.Click += BtnSave_Click;

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);

            // 3. Body (Flow Layout untuk input dinamis)
            pnlForm = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(24, 20, 24, 20),
                AutoScroll = true
            };

            this.Controls.Add(pnlForm);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
            pnlForm.BringToFront();
        }

        private void GenerateDynamicFields()
        {
            // Tentukan field apa saja yang perlu digambar berdasarkan Kategori
            if (_category == "User")
            {
                AddInput("Full Name", "full_name", GetValue("full_name"));
                AddInput("NIK / Inisial", "nik", GetValue("nik"));
                AddInput("Username", "username", GetValue("username"));
                AddComboBox("Role", "role", new[] { "Operator", "Teknisi", "Stock Control", "Admin", "Group Leader" }, GetValue("role"));
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
                // Semua sub-kategori Problem (Jenis, Detail, Penyebab, Tindakan) hanya punya 1 input nama
                AddInput($"Nama {_subCategory}", "nama", GetValue("nama"));
            }
        }

        // ==========================================
        // UI HELPERS (Membangun Input secara Dinamis)
        // ==========================================
        private void AddInput(string labelText, string fieldKey, string value, bool isNumeric = false)
        {
            Panel pnlContainer = CreateInputContainer(labelText);
            TextBox txtInput = new TextBox
            {
                Text = value,
                Dock = DockStyle.Bottom,
                Font = AppFonts.Body,
                Height = 35,
                BorderStyle = BorderStyle.FixedSingle
            };

            if (isNumeric) txtInput.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };

            pnlContainer.Controls.Add(txtInput);
            pnlForm.Controls.Add(pnlContainer);
            _inputControls.Add(fieldKey, txtInput); // Simpan referensi
        }

        private void AddComboBox(string labelText, string fieldKey, string[] options, string selectedValue)
        {
            Panel pnlContainer = CreateInputContainer(labelText);
            ComboBox cmbInput = new ComboBox
            {
                Dock = DockStyle.Bottom,
                Font = AppFonts.Body,
                Height = 35,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };

            cmbInput.Items.AddRange(options);
            if (!string.IsNullOrEmpty(selectedValue) && cmbInput.Items.Contains(selectedValue))
                cmbInput.SelectedItem = selectedValue;
            else if (options.Length > 0)
                cmbInput.SelectedIndex = 0;

            pnlContainer.Controls.Add(cmbInput);
            pnlForm.Controls.Add(pnlContainer);
            _inputControls.Add(fieldKey, cmbInput); // Simpan referensi
        }

        private Panel CreateInputContainer(string labelText)
        {
            Panel pnl = new Panel { Width = 430, Height = 65, Margin = new Padding(0, 0, 0, 15) };
            AppLabel lbl = new AppLabel { Text = labelText, Font = AppFonts.BodySmall, ForeColor = AppColors.TextSecondary, Dock = DockStyle.Top };
            pnl.Controls.Add(lbl);
            return pnl;
        }

        private string GetValue(string key)
        {
            if (_isEditMode && _rowData.ContainsKey(key) && _rowData[key] != null)
                return _rowData[key].ToString();
            return "";
        }

        // ==========================================
        // ACTION: SIMPAN
        // ==========================================
        private readonly IAdminRepository _repository; // Tambahan

        // 1. Constructor diubah untuk menerima repository
        public MasterDataEditorForm(IAdminRepository repository, string category, string subCategory = "", object rowData = null)
        {
            _repository = repository;
            _category = category;
            _subCategory = subCategory;
            _rowData = rowData as IDictionary<string, object>; 
            _isEditMode = _rowData != null;

            SetupUI();
            GenerateDynamicFields();
        }

        // 2. Fungsi Simpan yang SEBENARNYA
        private async void BtnSave_Click(object sender, EventArgs e)
        {
            var dataToSave = new Dictionary<string, object>();
            
            // Jika mode edit, sisipkan ID lama
            if (_isEditMode && _rowData.ContainsKey("id")) 
                dataToSave["id"] = _rowData["id"];

            // Kumpulkan isi teks dari semua Input/ComboBox yang ada di form
            foreach (var input in _inputControls)
            {
                string val = input.Value is ComboBox cmb ? cmb.Text : input.Value.Text;
                if (string.IsNullOrWhiteSpace(val))
                {
                    MessageBox.Show($"Kolom '{input.Key}' tidak boleh kosong!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                dataToSave[input.Key] = val;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                // Lempar data ke Dapper
                bool success = await _repository.SaveMasterDataAsync(_category, _subCategory, _isEditMode, dataToSave);
                
                if (success) {
                    MessageBox.Show("Data berhasil disimpan!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK; // Sukses! Form otomatis tertutup
                } else {
                    MessageBox.Show("Penyimpanan gagal, modul ini mungkin belum diatur.", "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Terjadi kesalahan SQL:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
    }
}