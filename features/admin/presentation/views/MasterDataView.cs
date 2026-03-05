using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        private string _currentCategory = "";

        // Konstruktor sekarang wajib menerima IAdminRepository
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

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent };

            lblTitle = new AppLabel { Text = "Master Data", Font = AppFonts.Header2, ForeColor = AppColors.TextPrimary, AutoSize = true, Location = new Point(0, 10) };

            AppButton btnAdd = new AppButton
            {
                Text = "+ Tambah Data", Type = AppButton.ButtonType.Primary, Width = 150, Height = 40,
                Location = new Point(this.Width - 198, 10), Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAdd.Click += (s, e) => MessageBox.Show($"Form Tambah {_currentCategory} belum dibuat.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Panel pnlSearch = new Panel { Width = 250, Height = 40, BackColor = AppColors.CardBackground, Location = new Point(this.Width - 465, 10), Anchor = AnchorStyles.Top | AnchorStyles.Right, Padding = new Padding(10) };
            TextBox txtSearch = new TextBox { Text = "Pencarian...", ForeColor = AppColors.TextDisabled, BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, Font = AppFonts.BodySmall, BackColor = AppColors.CardBackground };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "Pencarian...") { txtSearch.Text = ""; txtSearch.ForeColor = AppColors.TextPrimary; } };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Pencarian..."; txtSearch.ForeColor = AppColors.TextDisabled; } };
            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlSearch.ClientRectangle, AppColors.Border, ButtonBorderStyle.Solid);

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(pnlSearch);
            pnlHeader.Controls.Add(btnAdd);

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
            this.Controls.Add(pnlHeader);
            cardGridContainer.BringToFront();

            this.ResumeLayout(false);
        }

        // ==========================================
        // FUNGSI LOAD DINAMIS DENGAN DATABASE (ASYNC)
        // ==========================================
        public async void LoadCategory(string category)
        {
            _currentCategory = category;
            lblTitle.Text = $"Kelola Data {category}";

            // Reset tabel
            gridData.DataSource = null;
            gridData.Columns.Clear();

            // Atur Kolom
            switch (category)
            {
                case "User":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 50 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NIK / USERNAME", DataPropertyName = "nama", FillWeight = 150 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ROLE", DataPropertyName = "role", FillWeight = 100 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", DataPropertyName = "status", FillWeight = 80 });
                    break;
                case "Mesin":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "KODE MESIN", DataPropertyName = "kode", FillWeight = 80 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TIPE MESIN", DataPropertyName = "nama", FillWeight = 150 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "AREA", DataPropertyName = "area", FillWeight = 100 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "KONDISI", DataPropertyName = "kondisi", FillWeight = 80 });
                    break;
                case "Sparepart":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "KODE PART", DataPropertyName = "kode", FillWeight = 80 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NAMA SPAREPART", DataPropertyName = "nama", FillWeight = 180 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STOK", DataPropertyName = "stok", FillWeight = 60 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LOKASI RAK", DataPropertyName = "lokasi", FillWeight = 80 });
                    break;
                case "Problem":
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "id", FillWeight = 50 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "KATEGORI MASALAH", DataPropertyName = "kategori", FillWeight = 150 });
                    gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TINGKAT KEPARAHAN", DataPropertyName = "level", FillWeight = 100 });
                    break;
            }

            // Tambah Tombol Edit & Hapus
            gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Edit", HeaderText = "EDIT", Text = "Edit", UseColumnTextForButtonValue = true, FillWeight = 50 });
            gridData.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "HAPUS", Text = "Hapus", UseColumnTextForButtonValue = true, FillWeight = 50 });

            // Fetch Data
            try
            {
                this.Cursor = Cursors.WaitCursor;
                if (category == "User") gridData.DataSource = await _repository.GetMasterUsersAsync();
                else if (category == "Mesin") gridData.DataSource = await _repository.GetMasterMachinesAsync();
                else if (category == "Sparepart") gridData.DataSource = await _repository.GetMasterSparepartsAsync();
                else if (category == "Problem") gridData.DataSource = await _repository.GetMasterProblemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data {category}:\n" + ex.Message, "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
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

        private void GridData_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string colName = gridData.Columns[e.ColumnIndex].Name;
                if (colName == "Edit") MessageBox.Show($"Siapkan form untuk Edit data {_currentCategory} ini.", "Info");
                else if (colName == "Delete")
                {
                    if (MessageBox.Show($"Yakin ingin menghapus baris ini?", "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        // Implementasi Delete Dapper nanti di sini
                        MessageBox.Show("Sistem Delete sedang dibangun."); 
                    }
                }
            }
        }
    }
}