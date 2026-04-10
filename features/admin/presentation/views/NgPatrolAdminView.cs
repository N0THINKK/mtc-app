using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.admin.data.repositories;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.views
{
    public class NgPatrolAdminView : UserControl
    {
        private DataGridView _grid;
        private IAdminRepository _adminRepo;
        private TechnicianRepository _techRepo; // Untuk fetch data yang sama dengan teknisi

        public NgPatrolAdminView(IAdminRepository adminRepo)
        {
            _adminRepo = adminRepo;
            _techRepo = new TechnicianRepository();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Surface;
            this.Padding = new Padding(24);

            // 1. Header Panel
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60 };
            
            AppLabel lblTitle = new AppLabel 
            { 
                Text = "Manajemen Data Patroli NG", 
                Type = AppLabel.LabelType.Header2, 
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            Button btnRefresh = new Button
            {
                Text = "🔄 Refresh Data",
                Width = 150,
                Height = 40,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppColors.Primary,
                ForeColor = Color.White,
                Font = new Font(AppFonts.Body.FontFamily, AppFonts.Body.Size, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await LoadDataAsync();

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnRefresh);

            // 2. Info Panel
            Panel pnlInfo = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 40,
                Padding = new Padding(0, 10, 0, 10)
            };
            Label lblHelp = new Label
            {
                Text = "💡 Klik ganda (double-click) pada baris data untuk MENGHAPUS record Patroli NG yang salah input.",
                ForeColor = AppColors.TextSecondary,
                Font = AppFonts.Body,
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            pnlInfo.Controls.Add(lblHelp);

            // 3. Grid Panel
            Panel pnlGrid = new Panel 
            { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(0, 16, 0, 0)
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.CardBackground,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 45,
                RowTemplate = { Height = 45 },
                Cursor = Cursors.Hand,
                GridColor = AppColors.Separator
            };

            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = AppColors.SurfaceHover;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.TextPrimary;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font(AppFonts.Body.FontFamily, AppFonts.Body.Size, FontStyle.Bold);
            _grid.DefaultCellStyle.Font = AppFonts.Body;
            _grid.DefaultCellStyle.SelectionBackColor = AppColors.PrimaryLight;
            _grid.DefaultCellStyle.SelectionForeColor = AppColors.PrimaryDark;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatrolDate", HeaderText = "Tanggal", DataPropertyName = "FormattedPatrolDate", FillWeight = 15 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Machine", HeaderText = "Mesin", DataPropertyName = "MachineName", FillWeight = 25 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "Item Checksheet", DataPropertyName = "ItemName", FillWeight = 30 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", FillWeight = 15 });
            
            // Kolom tombol hapus (Virtual)
            var actionColumn = new DataGridViewButtonColumn
            {
                Name = "Action",
                HeaderText = "Aksi",
                Text = "🗑️ Hapus",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                FillWeight = 15
            };
            actionColumn.DefaultCellStyle.BackColor = AppColors.Danger;
            actionColumn.DefaultCellStyle.ForeColor = Color.White;
            _grid.Columns.Add(actionColumn);

            _grid.CellClick += Grid_CellClick;

            pnlGrid.Controls.Add(_grid);

            // Add all
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlInfo);
            this.Controls.Add(pnlHeader);
        }

        public async void LoadData()
        {
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                // Ambil data NG 30 hari terakhir (Bisa disesuaikan)
                var list = await _techRepo.GetPatrolNgListAsync("Semua", "DESC", DateTime.Now.AddDays(-30), DateTime.Now);
                
                _grid.AutoGenerateColumns = false;
                _grid.DataSource = list.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data Patroli NG: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Pastikan baris yang diklik adalah valid
            if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is PatrolNgDto dto)
            {
                // Cek apakah klik pada kolom action, ATAU double click di mana saja di baris itu
                bool isActionColumn = _grid.Columns[e.ColumnIndex].Name == "Action";

                if (isActionColumn)
                {
                    var confirm = MessageBox.Show(
                        $"Apakah Anda yakin ingin menghapus record Patroli NG berikut ini?\n\nMesin: {dto.MachineName}\nItem: {dto.ItemName}\nTanggal: {dto.FormattedPatrolDate}\n\nTindakan ini tidak dapat dibatalkan.",
                        "Konfirmasi Hapus",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm == DialogResult.Yes)
                    {
                        try
                        {
                            bool success = await _adminRepo.DeletePatrolNgAsync(dto.DetailId);
                            if (success)
                            {
                                MessageBox.Show("Data berhasil dihapus.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                await LoadDataAsync(); // Refresh setelah hapus
                            }
                            else
                            {
                                MessageBox.Show("Gagal menghapus data. Data mungkin sudah tidak ada.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Terjadi kesalahan saat menghapus data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }
}
