using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineRunForm : Form
    {
        private long _ticketId;
        private System.ComponentModel.IContainer components = null;
        private Label lblTitle;
        private Label lblSubtitle;
        private AppButton btnRun;

        public MachineRunForm(long ticketId)
        {
            _ticketId = ticketId;
            InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblTitle = new Label();
            this.lblSubtitle = new Label();
            this.btnRun = new AppButton();
            this.SuspendLayout();

            // 
            // Form Setup
            // 
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = AppColors.PrimaryDark; // Dark background for focus
            this.TopMost = true; // Keep on top

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = false;
            this.lblTitle.Dock = DockStyle.Top;
            this.lblTitle.Height = 150;
            this.lblTitle.TextAlign = ContentAlignment.BottomCenter;
            this.lblTitle.Font = new Font("Segoe UI", 36F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.White;
            this.lblTitle.Text = "PERBAIKAN SELESAI";

            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = false;
            this.lblSubtitle.Dock = DockStyle.Top;
            this.lblSubtitle.Height = 100;
            this.lblSubtitle.TextAlign = ContentAlignment.TopCenter;
            this.lblSubtitle.Font = new Font("Segoe UI", 18F, FontStyle.Regular);
            this.lblSubtitle.ForeColor = Color.LightGray;
            this.lblSubtitle.Text = "Silakan validasi kondisi mesin.\nJika mesin sudah siap produksi, tekan tombol di bawah.";

            // 
            // btnRun
            // 
            this.btnRun.Anchor = AnchorStyles.None;
            this.btnRun.Text = "RUN MESIN (PRODUKSI)";
            this.btnRun.Type = AppButton.ButtonType.Primary;
            this.btnRun.BackColor = AppColors.Success; // Green for GO
            this.btnRun.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            this.btnRun.Size = new Size(500, 120);
            this.btnRun.Location = new Point(
                (Screen.PrimaryScreen.Bounds.Width - 500) / 2,
                (Screen.PrimaryScreen.Bounds.Height - 120) / 2 + 50
            );
            this.btnRun.Click += BtnRun_Click;

            // 
            // Controls
            // 
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    // 1. Update Ticket: Set Production Resumed Time
                    string sqlTicket = "UPDATE tickets SET production_resumed_at = NOW() WHERE ticket_id = @Id";
                    connection.Execute(sqlTicket, new { Id = _ticketId });

                    // 2. Update Machine Status: Set to RUNNING (1)
                    // First, get the machine_id for this ticket
                    int machineId = connection.ExecuteScalar<int>("SELECT machine_id FROM tickets WHERE ticket_id = @Id", new { Id = _ticketId });
                    
                    string sqlMachine = "UPDATE machines SET current_status_id = 1 WHERE machine_id = @MachineId";
                    connection.Execute(sqlMachine, new { MachineId = machineId });
                }

                MessageBox.Show("Mesin Running! Waktu produksi tercatat.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Return OK result to parent form so it knows to close too
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
