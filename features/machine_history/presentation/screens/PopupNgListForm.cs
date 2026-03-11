using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public class PopupNgListForm : Form
    {
        private DataGridView gridPatrols;
        private TechnicianRepository _repo;

        public PopupNgListForm()
        {
            _repo = new TechnicianRepository();
            
            this.Text = "Daftar Temuan Patroli (NOT OK)";
            this.Size = new Size(900, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;

            gridPatrols = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.CardBackground,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Cursor = Cursors.Hand // Memberi isyarat bisa diklik
            };

            // Tambahkan kolom
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatrolDate", HeaderText = "Tanggal", DataPropertyName = "FormattedPatrolDate" });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Machine", HeaderText = "Mesin", DataPropertyName = "MachineName" });
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "Item NG", DataPropertyName = "ItemName" });

            // Event saat baris diklik ganda (Double Click)
            gridPatrols.CellDoubleClick += GridPatrols_CellDoubleClick;

            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = AppColors.Background
            };

            var btnClose = new Button
            {
                Text = "TUTUP",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Crimson,
                Size = new Size(120, 40),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(btnClose);

            // Tambahkan ke form (urutan ini penting agar Grid merespons panel bawah)
            this.Controls.Add(pnlBottom);
            this.Controls.Add(gridPatrols);
            gridPatrols.BringToFront();
            
            pnlBottom.Resize += (s, e) => 
            {
                btnClose.Location = new Point(pnlBottom.Width - btnClose.Width - 20, 10);
            }; // Pastikan grid berada di atas panel bottom
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            try
            {
                // Ambil data NG 7 hari terakhir (Bisa disesuaikan)
                var list = await _repo.GetPatrolNgListAsync("NG", "DESC", DateTime.Now.AddDays(-7), DateTime.Now);
                
                gridPatrols.AutoGenerateColumns = false;
                gridPatrols.DataSource = list.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data: " + ex.Message);
            }
        }

        private void GridPatrols_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridPatrols.Rows[e.RowIndex].DataBoundItem is PatrolNgDto dto)
            {
                if (dto.TicketId.HasValue)
                {
                    this.Hide();
                    // BUKA FORM TEKNISI DENGAN AUTO-START!
                    using (var techForm = new MachineHistoryFormTechnician(dto.TicketId.Value, autoStart: true))
                    {
                        techForm.ShowDialog(this);
                    }
                    
                    // Tutup popup daftar NG setelah perbaikan
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Tiket perbaikan belum terbentuk otomatis untuk temuan ini.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}