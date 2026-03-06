using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using mtc_app.features.admin.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.features.admin.presentation.views
{
    public class MasterDataView : UserControl
    {
        private readonly IAdminRepository _repository;
        private AppLabel lblTitle;
        private DataGridView gridData;
        private AppButton btnAdd; // <--- [MODIFIKASI] Jadikan btnAdd global agar teksnya bisa diganti dinamis
        
        private string _currentCategory = "";
        private string _currentProblemSubCategory = "";

        // Tambahan untuk Fitur Search
        private TextBox txtSearch;
        private IEnumerable<dynamic> _originalData; 

        // Komponen untuk Tab Problem
        private FlowLayoutPanel pnlProblemTabs;
        private AppButton btnTabJenis, btnTabDetail, btnTabPenyebab, btnTabTindakan;

        public MasterDataView(IAdminRepository repository)
        {
            _repository = repository;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(1100, 700);
            this.BackColor = AppColors.Surface;
            this.Padding = new Padding(24);

            // ==========================================
            // 1. HEADER SECTION (DIPERBAIKI ANTI-HILANG)
            // ==========================================
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 15) };

            // Judul di pojok kiri
            lblTitle = new AppLabel { Text = "Master Data", Font = AppFonts.Header2, ForeColor = AppColors.TextPrimary, AutoSize = true, Dock = DockStyle.Left };

            // Tombol Tambah (Akan menempel di pojok kanan)
            btnAdd = new AppButton
            {
                Text = "+ Tambah Data", Type = AppButton.ButtonType.Primary, Width = 250, Dock = DockStyle.Right // <--- [MODIFIKASI] Lebar ditambah agar teks panjang muat
            };
            btnAdd.Click += async (s, e) => // <--- [MODIFIKASI] Tambahkan async
            {
                // <--- [MODIFIKASI] Ambil daftar tipe mesin (Template) untuk form Checksheet
                string[] extraData = null;
                if (_currentCategory == "Checksheet") {
                    this.Cursor = Cursors.WaitCursor;
                    extraData = (await _repository.GetChecksheetTemplatesAsync()).ToArray();
                    this.Cursor = Cursors.Default;
                }

                // <--- [MODIFIKASI] Lempar extraData ke constructor form
                using (var form = new mtc_app.features.admin.presentation.screens.MasterDataEditorForm(_repository, _currentCategory, _currentProblemSubCategory, null, extraData))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        if (_currentCategory == "Problem" || _currentCategory == "Checksheet") 
                            LoadSubCategory(_currentProblemSubCategory);
                        else 
                            LoadCategory(_currentCategory);
                    }
                }
            };

            // Jarak (Spacer) antara tombol tambah dan kotak pencarian
            Panel pnlSpacer = new Panel { Width = 15, Dock = DockStyle.Right, BackColor = Color.Transparent };

            // Kotak Pencarian (Akan menempel di sebelah kiri tombol tambah)
            Panel pnlSearch = new Panel { Width = 250, BackColor = AppColors.CardBackground, Dock = DockStyle.Right, Padding = new Padding(12, 10, 12, 10) };
            txtSearch = new TextBox { Text = "Pencarian...", ForeColor = AppColors.TextDisabled, BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, Font = AppFonts.BodySmall, BackColor = AppColors.CardBackground };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "Pencarian...") { txtSearch.Text = ""; txtSearch.ForeColor = AppColors.TextPrimary; } };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Pencarian..."; txtSearch.ForeColor = AppColors.TextDisabled; } };
            
            // EVENT LIVE SEARCH
            txtSearch.TextChanged += TxtSearch_TextChanged;

            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlSearch.ClientRectangle, AppColors.Border, ButtonBorderStyle.Solid);

            // PENYUSUNAN DOCKING (Urutan sangat penting agar tidak terbalik)
            pnlHeader.Controls.Add(btnAdd);     // 1. Paling Kanan
            pnlHeader.Controls.Add(pnlSpacer);  // 2. Jarak kosong
            pnlHeader.Controls.Add(pnlSearch);  // 3. Search Bar
            pnlHeader.Controls.Add(lblTitle);   // 4. Paling Kiri

            // ==========================================
            // 2. PROBLEM TABS SECTION 
            // ==========================================
            pnlProblemTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 60, BackColor = Color.Transparent, 
                FlowDirection = FlowDirection.LeftToRight, Visible = false, Padding = new Padding(0, 10, 0, 10)
            };

            btnTabJenis = CreateTabButton("Kategori Masalah");
            btnTabDetail = CreateTabButton("Detail Problem");
            btnTabPenyebab = CreateTabButton("Penyebab Problem");
            btnTabTindakan = CreateTabButton("Tindakan Perbaikan");

            btnTabJenis.Click += (s, e) => { HighlightTab(btnTabJenis); LoadSubCategory(btnTabJenis.Text); };
            btnTabDetail.Click += (s, e) => { HighlightTab(btnTabDetail); LoadSubCategory(btnTabDetail.Text); };
            btnTabPenyebab.Click += (s, e) => { HighlightTab(btnTabPenyebab); LoadSubCategory(btnTabPenyebab.Text); };
            btnTabTindakan.Click += (s, e) => { HighlightTab(btnTabTindakan); LoadSubCategory(btnTabTindakan.Text); };

            pnlProblemTabs.Controls.AddRange(new Control[] { btnTabJenis, btnTabDetail, btnTabPenyebab, btnTabTindakan });

            // ==========================================
            // 3. DATAGRID CONTAINER
            // ==========================================
            AppCard cardGridContainer = new AppCard { Dock = DockStyle.Fill, ShowShadow = true, CornerRadius = 16, BackColor = AppColors.CardBackground, Padding = new Padding(20) };

            gridData = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = AppColors.CardBackground, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(238, 242, 246), ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, EnableHeadersVisualStyles = false,
                RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AutoGenerateColumns = false
            };

            gridData.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            gridData.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
            gridData.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252); 
            gridData.ColumnHeadersDefaultCellStyle.Font = new Font(AppFonts.FontFamily, 10.5F, FontStyle.Bold);
            gridData.ColumnHeadersDefaultCellStyle.Padding = new Padding(16, 14, 16, 14);
            gridData.ColumnHeadersHeight = 50;

            gridData.DefaultCellStyle.BackColor = AppColors.CardBackground;
            gridData.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            gridData.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            gridData.DefaultCellStyle.SelectionForeColor = AppColors.TextPrimary;
            gridData.DefaultCellStyle.Font = AppFonts.BodySmall;
            gridData.DefaultCellStyle.Padding = new Padding(16, 12, 16, 12);
            gridData.RowTemplate.Height = 56;
            
            gridData.DataBindingComplete += (s, e) => gridData.ClearSelection();
            gridData.CellPainting += GridData_CellPainting;
            gridData.CellContentClick += GridData_CellContentClick;

            cardGridContainer.Controls.Add(gridData);
            
            this.Controls.Add(cardGridContainer);
            this.Controls.Add(pnlProblemTabs);
            this.Controls.Add(pnlHeader);
            cardGridContainer.BringToFront();

            this.ResumeLayout(false);
        }

        // ==========================================
        // FITUR LIVE SEARCH
        // ==========================================
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            if (_originalData == null) return;

            string keyword = txtSearch.Text.Trim().ToLower();

            // Jika kosong, kembalikan ke data full
            if (string.IsNullOrWhiteSpace(keyword) || keyword == "pencarian...")
            {
                gridData.DataSource = _originalData.ToList();
                return;
            }

            // Loop menyaring semua data (Scan ke semua kolom/properties)
            var filteredList = _originalData.Where(row =>
            {
                // Dapper row dikonversi jadi dictionary untuk mempermudah scan isinya
                var dict = row as IDictionary<string, object>;
                if (dict == null) return false;

                foreach (var value in dict.Values)
                {
                    if (value != null && value.ToString().ToLower().Contains(keyword))
                    {
                        return true; // Ditemukan kecocokan di salah satu kolom!
                    }
                }
                return false;
            }).ToList();

            // Tampilkan hasil saringan ke grid
            gridData.DataSource = filteredList;
        }

        private void ResetSearchBox()
        {
            txtSearch.TextChanged -= TxtSearch_TextChanged; // Hentikan event sementara
            txtSearch.Text = "Pencarian...";
            txtSearch.ForeColor = AppColors.TextDisabled;
            txtSearch.TextChanged += TxtSearch_TextChanged; // Nyalakan lagi
        }

        private AppButton CreateTabButton(string text)
        {
            return new AppButton { Text = text, Type = AppButton.ButtonType.Secondary, Height = 40, Width = 160, Margin = new Padding(0, 0, 10, 0) };
        }

        private void HighlightTab(AppButton activeBtn)
        {
            btnTabJenis.Type = AppButton.ButtonType.Secondary;
            btnTabDetail.Type = AppButton.ButtonType.Secondary;
            btnTabPenyebab.Type = AppButton.ButtonType.Secondary;
            btnTabTindakan.Type = AppButton.ButtonType.Secondary;
            activeBtn.Type = AppButton.ButtonType.Primary;
        }

        // ==========================================
        // FUNGSI LOAD KATEGORI
        // ==========================================
        public async void LoadCategory(string category)
        {
            _currentCategory = category;
            lblTitle.Text = $"Kelola Data {category}";
            btnAdd.Text = $"+ Tambah Data {category}"; // <--- [MODIFIKASI] Update teks tombol otomatis

            _originalData = null;
            gridData.DataSource = null;
            gridData.Columns.Clear();
            ResetSearchBox();

            if (category == "Problem")
            {
                pnlProblemTabs.Visible = true;
                btnTabJenis.Visible = true; btnTabDetail.Visible = true; btnTabPenyebab.Visible = true; btnTabTindakan.Visible = true;
                btnTabJenis.Text = "Kategori Masalah"; btnTabDetail.Text = "Detail Problem"; 
                
                HighlightTab(btnTabJenis);
                LoadSubCategory("Kategori Masalah");
                return; 
            }
            else if (category == "Checksheet") // <--- [MODIFIKASI] Munculkan tab untuk Checksheet
            {
                pnlProblemTabs.Visible = true;
                btnTabJenis.Visible = true; btnTabDetail.Visible = true; 
                btnTabPenyebab.Visible = false; btnTabTindakan.Visible = false;
                
                btnTabJenis.Text = "Operator"; btnTabDetail.Text = "Teknisi";
                
                HighlightTab(btnTabJenis);
                LoadSubCategory("Checksheet Operator");
                return;
            }

            pnlProblemTabs.Visible = false;

            switch (category)
            {
                case "User":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 50 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NAMA", DataPropertyName = "full_name", FillWeight = 150 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ROLE", DataPropertyName = "role", FillWeight = 80 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NIK / INISIAL", DataPropertyName = "nik", FillWeight = 100 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "USERNAME", DataPropertyName = "username", FillWeight = 120 });
                    break;
                case "Mesin":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TIPE MESIN", DataPropertyName = "tipe", FillWeight = 150 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "AREA", DataPropertyName = "area", FillWeight = 100 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "KODE MESIN", DataPropertyName = "kode", FillWeight = 80 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "KONDISI", DataPropertyName = "kondisi", FillWeight = 80 });
                    break;
                case "Sparepart":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "KODE PART", DataPropertyName = "kode", FillWeight = 80 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NAMA SPAREPART", DataPropertyName = "nama", FillWeight = 180 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STOK", DataPropertyName = "stok", FillWeight = 60 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LOKASI RAK", DataPropertyName = "lokasi", FillWeight = 80 });
                    break;
            }

            gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Edit", HeaderText = "EDIT", Text = "Edit", UseColumnTextForButtonValue = true, FillWeight = 50 });
            gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "HAPUS", Text = "Hapus", UseColumnTextForButtonValue = true, FillWeight = 50 });

            try
            {
                this.Cursor = Cursors.WaitCursor;
                if (category == "User") _originalData = await _repository.GetMasterUsersAsync();
                else if (category == "Mesin") _originalData = await _repository.GetMasterMachinesAsync();
                else if (category == "Sparepart") _originalData = await _repository.GetMasterSparepartsAsync();
                
                gridData.DataSource = _originalData?.ToList();
            }
            catch (Exception ex) { MessageBox.Show($"Gagal memuat {category}:\n" + ex.Message, "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { this.Cursor = Cursors.Default; }
        }

        private async void LoadSubCategory(string subCategory)
        {
            if (_currentCategory == "Checksheet" && (subCategory == "Operator" || subCategory == "Teknisi"))
            {
                subCategory = $"Checksheet {subCategory}";
            }
            _currentProblemSubCategory = subCategory;

            // <--- [MODIFIKASI] Ubah teks di Header berdasarkan Tab yang diklik
            lblTitle.Text = $"Kelola Data {subCategory}";
            btnAdd.Text = $"+ Tambah {subCategory}"; 
            
            _originalData = null;
            gridData.DataSource = null;
            gridData.Columns.Clear();
            ResetSearchBox();

            if (_currentCategory == "Checksheet")
            {
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 50 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TARGET", DataPropertyName = "role_target", FillWeight = 80 }); // <--- [MODIFIKASI] Tambah Kolom Target Role
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TIPE MESIN", DataPropertyName = "tipe_mesin", FillWeight = 100 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM PENGECEKAN", DataPropertyName = "item_pengecekan", FillWeight = 200 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STANDAR (JUDGMENT)", DataPropertyName = "standar", FillWeight = 150 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "METODE", DataPropertyName = "metode", FillWeight = 100 });
            }
            else // Jika Problem
            {
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 50 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = subCategory.ToUpper(), DataPropertyName = "nama", FillWeight = 250 });
            }
            
            gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Edit", HeaderText = "EDIT", Text = "Edit", UseColumnTextForButtonValue = true, FillWeight = 50 });
            gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "HAPUS", Text = "Hapus", UseColumnTextForButtonValue = true, FillWeight = 50 });

            try
            {
                this.Cursor = Cursors.WaitCursor;
                
                if (subCategory == "Kategori Masalah") _originalData = await _repository.GetMasterProblemTypesAsync();
                else if (subCategory == "Detail Problem") _originalData = await _repository.GetMasterFailuresAsync();
                else if (subCategory == "Penyebab Problem") _originalData = await _repository.GetMasterCausesAsync();
                else if (subCategory == "Tindakan Perbaikan") _originalData = await _repository.GetMasterActionsAsync();
                else if (subCategory == "Checksheet Operator") _originalData = await _repository.GetMasterChecksheetsAsync("Operator"); // <--- Panggil Data
                else if (subCategory == "Checksheet Teknisi") _originalData = await _repository.GetMasterChecksheetsAsync("Teknisi"); // <--- Panggil Data

                gridData.DataSource = _originalData?.ToList();
            }
            catch (Exception ex) { MessageBox.Show($"Gagal memuat {subCategory}:\n" + ex.Message, "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { this.Cursor = Cursors.Default; }
        }

        private void GridData_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = gridData.Columns[e.ColumnIndex].Name;
            if (colName == "Edit" || colName == "Delete")
            {
                e.PaintBackground(e.CellBounds, true);
                int btnHeight = 32, btnWidth = 65;
                int btnY = e.CellBounds.Y + (e.CellBounds.Height - btnHeight) / 2;
                int btnX = e.CellBounds.X + (e.CellBounds.Width - btnWidth) / 2;
                Rectangle btnRect = new Rectangle(btnX, btnY, btnWidth, btnHeight);
                
                Color btnColor = colName == "Edit" ? AppColors.Warning : AppColors.Danger;
                string btnText = colName == "Edit" ? "Edit" : "Hapus";

                using (System.Drawing.Drawing2D.GraphicsPath path = GraphicsUtils.GetRoundedRectangle(btnRect, 6))
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(20, btnColor))) e.Graphics.FillPath(brush, path);
                    using (Pen pen = new Pen(btnColor, 1f)) e.Graphics.DrawPath(pen, path);
                }

                TextRenderer.DrawText(e.Graphics, btnText, new Font(AppFonts.BodySmall, FontStyle.Bold), btnRect, btnColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
        }

        private async void GridData_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string colName = gridData.Columns[e.ColumnIndex].Name;
                var rowData = gridData.Rows[e.RowIndex].DataBoundItem; 

                if (colName == "Edit")
                {
                    // <--- [MODIFIKASI] Ambil Template untuk dikirim ke Editor
                    string[] extraData = null;
                    if (_currentCategory == "Checksheet") {
                        this.Cursor = Cursors.WaitCursor;
                        extraData = (await _repository.GetChecksheetTemplatesAsync()).ToArray();
                        this.Cursor = Cursors.Default;
                    }

                    using (var form = new mtc_app.features.admin.presentation.screens.MasterDataEditorForm(_repository, _currentCategory, _currentProblemSubCategory, rowData, extraData))
                    {
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            if (_currentCategory == "Problem" || _currentCategory == "Checksheet") 
                                LoadSubCategory(_currentProblemSubCategory);
                            else 
                                LoadCategory(_currentCategory);
                        }
                    }
                }
                else if (colName == "Delete")
                {
                    if (MessageBox.Show($"Yakin ingin menghapus data ini secara permanen?", "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        var dataDict = rowData as IDictionary<string, object>;
                        int idToDelete = Convert.ToInt32(dataDict["id"]);

                        bool success = await _repository.DeleteMasterDataAsync(_currentCategory, _currentProblemSubCategory, idToDelete);
                        if (success) {
                            if (_currentCategory == "Problem" || _currentCategory == "Checksheet") 
                                LoadSubCategory(_currentProblemSubCategory);
                            else 
                                LoadCategory(_currentCategory);
                        } else {
                            MessageBox.Show("Gagal menghapus data. Data mungkin sedang dipakai di tabel lain.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }
}