using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.data.utils;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.shared.data.session;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public class PopupNgListForm : Form
    {
        private DataGridView gridPatrols;
        private TechnicianRepository _repo;
        private int? _machineId; // Menyimpan ID mesin untuk filter

        public PopupNgListForm(int? machineId = null)
        {
            _machineId = machineId;
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
                
                // Menerapkan filter mesin jika diakses langsung dari form spesifik mesin
                if (_machineId.HasValue)
                {
                    list = list.Where(x => x.MachineId == _machineId.Value).ToList();
                }

                gridPatrols.AutoGenerateColumns = false;
                gridPatrols.DataSource = list.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data: " + ex.Message);
            }
        }

        // Tambahkan 'async' di sini karena kita akan memanggil fungsi CreateTicketAsync
        private async void GridPatrols_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridPatrols.Rows[e.RowIndex].DataBoundItem is PatrolNgDto dto)
            {
                // Jika sudah ada tiketnya (kasus lama)
                if (dto.TicketId.HasValue && dto.TicketId.Value > 0)
                {
                    this.Hide(); 
                    // [MODIFIKASI] Tambahkan dto.DetailId
                    using (var techForm = new MachineHistoryFormTechnician(dto.TicketId.Value, autoStart: true, dto.DetailId))
                    {
                        techForm.ShowDialog(this);
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    // TIKET BELUM ADA: Tawarkan untuk membuat tiket saat itu juga
                    var confirm = MessageBox.Show(
                        $"Temuan NG pada '{dto.ItemName}' belum memiliki tiket perbaikan di antrean.\n\nApakah Anda ingin membuat tiket baru dan mulai memperbaikinya sekarang?", 
                        "Buat Tiket Perbaikan", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Question);

                    if (confirm == DialogResult.Yes)
                    {
                        try
                        {
                            using (var conn = DatabaseHelper.GetConnection())
                            {
                                // 1. Ambil machine_id dari database berdasarkan log_id
                                int machineId = conn.QueryFirstOrDefault<int>("SELECT machine_id FROM patrol_logs WHERE log_id = @LogId", new { LogId = dto.LogId });

                                if (machineId == 0)
                                {
                                    MessageBox.Show("Gagal menemukan data mesin.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }

                                var historyRepo = new MachineHistoryRepository();
                                
                                // 2. Buat tiket baru secara on-demand
                                var req = new CreateTicketRequest
                                {
                                    MachineId = machineId,
                                    OperatorNik = UserSession.CurrentUser?.Username ?? "-", // Anggap yg sedang login sebagai operator/pelapor
                                    TechnicianNik = UserSession.CurrentUser?.Username,
                                    ShiftName = "A",
                                    ApplicatorCode = "-",
                                    StatusId = 2, // Langsung status 'Repairing' (2) karena teknisi langsung auto-start
                                    IsMachineRunning = 0,
                                    StartedAt = DateTime.Now,
                                    Problems = new System.Collections.Generic.List<TicketProblemRequest>
                                    {
                                        new TicketProblemRequest
                                        {
                                            ProblemTypeName = "Lain-lain",
                                            FailureName = $"[CHECKSHEET] {dto.ItemName} NG"
                                        }
                                    }
                                };

                                var ticketResult = await historyRepo.CreateTicketAsync(req);

                                // 3. Tandai di tabel checksheet bahwa tiket sudah dibuat (agar tidak dobel jika diklik lagi nanti)
                                conn.Execute("UPDATE patrol_log_details SET is_ticket_created = 1 WHERE detail_id = @DetailId", new { DetailId = dto.DetailId });

                                this.Hide();

                                // 4. Buka form teknisi menggunakan ID tiket yang baru saja berhasil di-generate
                                // [MODIFIKASI] Menambahkan param dto.DetailId agar hanya ID checksheet ini yang ditutup saat perbaikan selesai
                                using (var techForm = new MachineHistoryFormTechnician(ticketResult.TicketId, autoStart: true, dto.DetailId))
                                {
                                    techForm.ShowDialog(this);
                                }
                                
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Gagal membuat tiket: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }
}