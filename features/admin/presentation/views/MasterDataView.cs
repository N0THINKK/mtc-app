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
using Dapper;

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
        private IEnumerable<dynamic> _currentData; 
        private int _currentPage = 1;
        private int _pageSize = 50;
        
        // Paginasi Controls
        private Panel pnlPagination;
        private AppButton btnPrevPage;
        private AppButton btnNextPage;
        private Label lblPageInfo;

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
                Text = "+ Tambah Data", Type = AppButton.ButtonType.Primary, Width = 250, Dock = DockStyle.Right
            };
            btnAdd.Click += async (s, e) =>
            {
                // Target category uses a custom inline dialog
                if (_currentCategory == "Target")
                {
                    await ShowTargetEditorDialog(null);
                    return;
                }

                string[] extraData = null;
                if (_currentCategory == "Checksheet") {
                    this.Cursor = Cursors.WaitCursor;
                    extraData = (await _repository.GetChecksheetTemplatesAsync()).ToArray();
                    this.Cursor = Cursors.Default;
                } else if (_currentCategory == "Mesin") {
                    this.Cursor = Cursors.WaitCursor;
                    var types = await _repository.GetMachineTypesAsync();
                    var areas = await _repository.GetMachineAreasAsync();
                    
                    var combined = new List<string>();
                    combined.Add("TYPES");
                    combined.AddRange(types);
                    combined.Add("AREAS");
                    combined.AddRange(areas);
                    
                    extraData = combined.ToArray();
                    this.Cursor = Cursors.Default;
                }

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

            // ==========================================
            // PAGINATION UI
            // ==========================================
            pnlPagination = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 0), Visible = false };
            
            btnPrevPage = new AppButton { Text = "< Sebelumnya", Type = AppButton.ButtonType.Secondary, Width = 120, Dock = DockStyle.Left };
            btnPrevPage.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; RenderGrid(); } };
            
            btnNextPage = new AppButton { Text = "Selanjutnya >", Type = AppButton.ButtonType.Secondary, Width = 120, Dock = DockStyle.Right };
            btnNextPage.Click += (s, e) => { _currentPage++; RenderGrid(); };

            lblPageInfo = new Label { 
                Text = "Halaman 1 dari 1", 
                Font = AppFonts.Body, 
                ForeColor = AppColors.TextSecondary, 
                AutoSize = false, 
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill 
            };

            pnlPagination.Controls.Add(btnPrevPage);
            pnlPagination.Controls.Add(lblPageInfo);
            pnlPagination.Controls.Add(btnNextPage);
            
            // Add Pagination First (Bottom Dock), then GridData (Fill Dock)
            cardGridContainer.Controls.Add(pnlPagination);
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
                _currentData = _originalData;
                _currentPage = 1;
                RenderGrid();
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
            _currentData = filteredList;
            _currentPage = 1;
            RenderGrid();
        }

        private void RenderGrid()
        {
            if (_currentData == null)
            {
                gridData.DataSource = null;
                pnlPagination.Visible = false;
                return;
            }

            var list = _currentData.ToList();
            int totalItems = list.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)_pageSize);
            if (totalPages == 0) totalPages = 1;

            if (_currentPage > totalPages) _currentPage = totalPages;
            if (_currentPage < 1) _currentPage = 1;

            var pagedData = list.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
            gridData.DataSource = pagedData;

            lblPageInfo.Text = $"Halaman {_currentPage} dari {totalPages} (Total: {totalItems} data)";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < totalPages;
            
            pnlPagination.Visible = totalItems > 0;
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
            btnAdd.Visible = category != "Waktu Break";

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
            else if (category == "Checksheet") 
            {
                pnlProblemTabs.Visible = true;
                btnTabJenis.Visible = true; btnTabDetail.Visible = true; 
                btnTabPenyebab.Visible = false; btnTabTindakan.Visible = false;
                
                btnTabJenis.Text = "Operator"; btnTabDetail.Text = "Teknisi";
                
                HighlightTab(btnTabJenis);
                LoadSubCategory("Checksheet Operator");
                return;
            }
            else if (category == "Waktu Break") 
            {
                pnlProblemTabs.Visible = true;
                btnTabJenis.Visible = true; btnTabDetail.Visible = true; 
                btnTabPenyebab.Visible = false; btnTabTindakan.Visible = false;
                
                btnTabJenis.Text = "Shift 1"; btnTabDetail.Text = "Shift 2";
                
                HighlightTab(btnTabJenis);
                LoadSubCategory("Shift 1");
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
                case "Area Mesin":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 50 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NAMA AREA", DataPropertyName = "nama", FillWeight = 250 });
                    break;
                case "Sparepart":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "KODE PART", DataPropertyName = "kode", FillWeight = 80 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NAMA SPAREPART", DataPropertyName = "nama", FillWeight = 180 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STOK", DataPropertyName = "stok", FillWeight = 60 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LOKASI RAK", DataPropertyName = "lokasi", FillWeight = 80 });
                    break;
                case "Target":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 40 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TIPE MESIN", DataPropertyName = "tipe_mesin", FillWeight = 120 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "AREA", DataPropertyName = "area", FillWeight = 100 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NO MESIN", DataPropertyName = "no_mesin", FillWeight = 80 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TARGET/JAM", DataPropertyName = "target_per_jam", FillWeight = 80 });
                    break;
            }

            gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Edit", HeaderText = "EDIT", Text = "Edit", UseColumnTextForButtonValue = true, FillWeight = 50 });
            gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "HAPUS", Text = "Hapus", UseColumnTextForButtonValue = true, FillWeight = 50 });

            try
            {
                this.Cursor = Cursors.WaitCursor;
                if (category == "User") _originalData = await _repository.GetMasterUsersAsync();
                else if (category == "Mesin") _originalData = await _repository.GetMasterMachinesAsync();
                else if (category == "Area Mesin") _originalData = await _repository.GetMasterMachineAreasDataAsync();
                else if (category == "Sparepart") _originalData = await _repository.GetMasterSparepartsAsync();
                else if (category == "Target") _originalData = await _repository.GetOutputTargetsAsync();
                
                _currentData = _originalData;
                _currentPage = 1;
                RenderGrid();
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
            btnAdd.Visible = _currentCategory != "Waktu Break";
            
            _originalData = null;
            gridData.DataSource = null;
            gridData.Columns.Clear();
            ResetSearchBox();

            if (_currentCategory == "Checksheet")
            {
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 50 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TARGET", DataPropertyName = "role_target", FillWeight = 80 }); 
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TIPE MESIN", DataPropertyName = "tipe_mesin", FillWeight = 100 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM PENGECEKAN", DataPropertyName = "item_pengecekan", FillWeight = 200 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STANDAR (JUDGMENT)", DataPropertyName = "standar", FillWeight = 150 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "METODE", DataPropertyName = "metode", FillWeight = 100 });
            }
            else if (_currentCategory == "Waktu Break")
            {
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 30 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "HARI", DataPropertyName = "hari", FillWeight = 100 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BREAK NON-OT (Menit)", DataPropertyName = "non_ot_minutes", FillWeight = 100 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TAMBAHAN BREAK OT (Menit)", DataPropertyName = "ot_minutes", FillWeight = 100 });
            }
            else // Jika Problem
            {
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 50 });
                gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = subCategory.ToUpper(), DataPropertyName = "nama", FillWeight = 250 });
            }
            gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Edit", HeaderText = "EDIT", Text = "Edit", UseColumnTextForButtonValue = true, FillWeight = 50 });
            if (_currentCategory != "Waktu Break")
            {
                gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "HAPUS", Text = "Hapus", UseColumnTextForButtonValue = true, FillWeight = 50 });
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                
                if (subCategory == "Kategori Masalah") _originalData = await _repository.GetMasterProblemTypesAsync();
                else if (subCategory == "Detail Problem") _originalData = await _repository.GetMasterFailuresAsync();
                else if (subCategory == "Penyebab Problem") _originalData = await _repository.GetMasterCausesAsync();
                else if (subCategory == "Tindakan Perbaikan") _originalData = await _repository.GetMasterActionsAsync();
                else if (subCategory == "Checksheet Operator") _originalData = await _repository.GetMasterChecksheetsAsync("Operator"); 
                else if (subCategory == "Checksheet Teknisi") _originalData = await _repository.GetMasterChecksheetsAsync("Teknisi"); 
                else if (_currentCategory == "Waktu Break") _originalData = await _repository.GetShiftBreaksAsync(subCategory);

                _currentData = _originalData;
                _currentPage = 1;
                RenderGrid();
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
                    // Target uses custom dialog
                    if (_currentCategory == "Target")
                    {
                        await ShowTargetEditorDialog(rowData);
                        return;
                    }

                    if (_currentCategory == "Waktu Break")
                    {
                        await ShowShiftBreakEditorDialog(rowData);
                        return;
                    }

                    string[] extraData = null;
                    if (_currentCategory == "Checksheet") {
                        this.Cursor = Cursors.WaitCursor;
                        extraData = (await _repository.GetChecksheetTemplatesAsync()).ToArray();
                        this.Cursor = Cursors.Default;
                    } else if (_currentCategory == "Mesin") {
                        this.Cursor = Cursors.WaitCursor;
                        var types = await _repository.GetMachineTypesAsync();
                        var areas = await _repository.GetMachineAreasAsync();
                        var combined = new List<string>();
                        combined.Add("TYPES"); combined.AddRange(types);
                        combined.Add("AREAS"); combined.AddRange(areas);
                        extraData = combined.ToArray();
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

                        try
                        {
                            bool success;
                            if (_currentCategory == "Target")
                                success = await _repository.DeleteOutputTargetAsync(idToDelete);
                            else
                                success = await _repository.DeleteMasterDataAsync(_currentCategory, _currentProblemSubCategory, idToDelete);

                            if (success) {
                                if (_currentCategory == "Problem" || _currentCategory == "Checksheet") 
                                    LoadSubCategory(_currentProblemSubCategory);
                                else 
                                    LoadCategory(_currentCategory);
                            } else {
                                MessageBox.Show("Gagal menghapus data. Data mungkin sedang dipakai di tabel lain.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("foreign key") || ex.Message.Contains("REFERENCE"))
                            {
                                MessageBox.Show("Tidak dapat menghapus data ini karena masih digunakan (terikat) dengan data lain di sistem (misal: ada mesin yang menggunakan area ini).", "Gagal Menghapus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            else
                            {
                                MessageBox.Show($"Terjadi kesalahan saat menghapus data:\n{ex.Message}", "Error Sistem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
        }

        // ==========================================
        // TARGET OUTPUT: Custom Editor Dialog
        // ==========================================
        private async Task ShowTargetEditorDialog(object existingData)
        {
            this.Cursor = Cursors.WaitCursor;
            var types = (await _repository.GetMachineTypesAsync()).ToList();
            var areas = (await _repository.GetMachineAreasAsync()).ToList();
            this.Cursor = Cursors.Default;

            using (var dlg = new Form())
            {
                dlg.Text = existingData == null ? "Tambah Target Output" : "Edit Target Output";
                dlg.Size = new Size(400, 320); // Increased height to accommodate new field
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = AppColors.Background;
                dlg.Padding = new Padding(24);

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5 }; // Increased rows
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

                var lblType = new Label { Text = "Tipe Mesin:", Font = AppFonts.Body, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
                var cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = AppFonts.Body };
                cmbType.Items.AddRange(types.ToArray());

                var lblArea = new Label { Text = "Area:", Font = AppFonts.Body, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
                var cmbArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = AppFonts.Body };
                cmbArea.Items.AddRange(areas.ToArray());

                var lblMachineNum = new Label { Text = "No Mesin:", Font = AppFonts.Body, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
                var txtMachineNum = new TextBox { Dock = DockStyle.Fill, Font = AppFonts.Body };

                var lblTarget = new Label { Text = "Target/Jam:", Font = AppFonts.Body, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
                var txtTarget = new NumericUpDown { Minimum = 0, Maximum = 999999, Dock = DockStyle.Fill, Font = AppFonts.Body };

                int? currentTargetId = null;

                // Pre-fill for edit mode
                if (existingData != null)
                {
                    var dict = existingData as IDictionary<string, object>;
                    if (dict != null)
                    {
                        currentTargetId = Convert.ToInt32(dict["id"]);
                        string editType = dict["tipe_mesin"]?.ToString() ?? "";
                        string editArea = dict["area"]?.ToString() ?? "";
                        string editNum = dict["no_mesin"]?.ToString() ?? "";
                        int editTarget = Convert.ToInt32(dict["target_per_jam"]);

                        int typeIdx = types.IndexOf(editType);
                        if (typeIdx >= 0) cmbType.SelectedIndex = typeIdx;

                        int areaIdx = areas.IndexOf(editArea);
                        if (areaIdx >= 0) cmbArea.SelectedIndex = areaIdx;

                        txtMachineNum.Text = editNum;
                        txtTarget.Value = editTarget;
                    }
                }

                var btnSave = new AppButton { Text = "Simpan", Type = AppButton.ButtonType.Primary, Dock = DockStyle.Fill, Height = 40 };

                layout.Controls.Add(lblType, 0, 0); layout.Controls.Add(cmbType, 1, 0);
                layout.Controls.Add(lblArea, 0, 1); layout.Controls.Add(cmbArea, 1, 1);
                layout.Controls.Add(lblMachineNum, 0, 2); layout.Controls.Add(txtMachineNum, 1, 2);
                layout.Controls.Add(lblTarget, 0, 3); layout.Controls.Add(txtTarget, 1, 3);
                layout.Controls.Add(btnSave, 0, 4); layout.SetColumnSpan(btnSave, 2);

                dlg.Controls.Add(layout);

                btnSave.Click += async (s, e) =>
                {
                    if (cmbType.SelectedIndex < 0 || cmbArea.SelectedIndex < 0)
                    {
                        MessageBox.Show("Pilih Tipe Mesin dan Area terlebih dahulu.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string machineNum = txtMachineNum.Text.Trim();
                    if (string.IsNullOrEmpty(machineNum))
                    {
                        MessageBox.Show("Nomor Mesin harus diisi.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string selectedType = cmbType.SelectedItem.ToString();
                    string selectedArea = cmbArea.SelectedItem.ToString();

                    // Resolve type_id and area_id from names
                    int typeId = 0, areaId = 0;
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        typeId = await conn.QueryFirstOrDefaultAsync<int>("SELECT type_id FROM machine_types WHERE type_name = @n", new { n = selectedType });
                        areaId = await conn.QueryFirstOrDefaultAsync<int>("SELECT area_id FROM machine_areas WHERE area_name = @n", new { n = selectedArea });
                    }

                    if (typeId == 0 || areaId == 0)
                    {
                        MessageBox.Show("Tipe Mesin atau Area tidak ditemukan.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    try
                    {
                        bool success = await _repository.SaveOutputTargetAsync(currentTargetId, typeId, areaId, machineNum, (int)txtTarget.Value);
                        if (success)
                        {
                            dlg.DialogResult = DialogResult.OK;
                            dlg.Close();
                        }
                        else
                        {
                            MessageBox.Show("Gagal menyimpan target.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex) when (ex.Message.Contains("Duplicate entry") || ex.Message.Contains("Duplicate key"))
                    {
                        MessageBox.Show("Target untuk kombinasi Tipe Mesin, Area, dan No Mesin ini sudah ada! Silakan ubah salah satunya.", "Data Duplikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Terjadi kesalahan sistem: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadCategory("Target");
                }
            }
        }

        private async Task ShowShiftBreakEditorDialog(object existingData)
        {
            if (existingData == null) return; // Hanya bisa Edit

            using (var dlg = new Form())
            {
                dlg.Text = "Edit Waktu Break (" + _currentProblemSubCategory + ")";
                dlg.Size = new Size(400, 300);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = AppColors.Background;
                dlg.Padding = new Padding(24);

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4 };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

                var lblDayName = new Label { Text = "Hari:", Font = AppFonts.Body, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
                var txtDayName = new TextBox { Dock = DockStyle.Fill, Font = AppFonts.Body, ReadOnly = true, BackColor = Color.LightGray };
                
                var lblNonOt = new Label { Text = "Break Non-OT (Menit):", Font = AppFonts.Body, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
                var txtNonOt = new NumericUpDown { Minimum = 0, Maximum = 1440, Dock = DockStyle.Fill, Font = AppFonts.Body };
                
                var lblOt = new Label { Text = "Break OT (Menit):", Font = AppFonts.Body, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
                var txtOt = new NumericUpDown { Minimum = 0, Maximum = 1440, Dock = DockStyle.Fill, Font = AppFonts.Body };

                int currentBreakId = 0;
                string dayName = "";

                var dict = existingData as IDictionary<string, object>;
                if (dict != null)
                {
                    currentBreakId = Convert.ToInt32(dict["id"]);
                    dayName = dict["hari"]?.ToString() ?? "";
                    
                    if (int.TryParse(dict["non_ot_minutes"]?.ToString(), out int nonOt))
                        txtNonOt.Value = nonOt;
                    if (int.TryParse(dict["ot_minutes"]?.ToString(), out int ot))
                        txtOt.Value = ot;

                    txtDayName.Text = dayName;
                }

                var btnSave = new AppButton { Text = "Simpan", Type = AppButton.ButtonType.Primary, Dock = DockStyle.Fill, Height = 40 };

                layout.Controls.Add(lblDayName, 0, 0); layout.Controls.Add(txtDayName, 1, 0);
                layout.Controls.Add(lblNonOt, 0, 1); layout.Controls.Add(txtNonOt, 1, 1);
                layout.Controls.Add(lblOt, 0, 2); layout.Controls.Add(txtOt, 1, 2);
                layout.Controls.Add(btnSave, 0, 3); layout.SetColumnSpan(btnSave, 2);

                dlg.Controls.Add(layout);

                btnSave.Click += async (s, e) =>
                {
                    try
                    {
                        // dayId arg doesn't matter for Update because we use breakId
                        bool success = await _repository.SaveShiftBreakAsync(currentBreakId, _currentProblemSubCategory, 1, (int)txtNonOt.Value, (int)txtOt.Value);
                        if (success)
                        {
                            dlg.DialogResult = DialogResult.OK;
                            dlg.Close();
                        }
                        else
                        {
                            MessageBox.Show("Gagal menyimpan waktu break.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Terjadi kesalahan sistem: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadSubCategory(_currentProblemSubCategory);
                }
            }
        }
    }
}