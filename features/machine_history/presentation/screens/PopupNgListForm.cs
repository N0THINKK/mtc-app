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
            gridPatrols.Columns.Add(new DataGridViewTextBoxColumn { Name = "Note", HeaderText = "Catatan", DataPropertyName = "ActionNote" });

            // Event saat baris diklik ganda (Double Click)
            gridPatrols.CellDoubleClick += GridPatrols_CellDoubleClick;

            this.Controls.Add(gridPatrols);
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
                    // BUKA FORM TEKNISI DENGAN AUTO-START!
                    using (var techForm = new MachineHistoryFormTechnician(dto.TicketId.Value, autoStart: true))
                    {
                        techForm.ShowDialog();
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