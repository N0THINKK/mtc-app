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
        private Label lblStopwatch;
        private Panel panelButton;
        private AppButton btnRun;
        private AppButton btnBack;
        private System.Diagnostics.Stopwatch stopwatch;
        private Timer timer;

        public MachineRunForm(long ticketId)
        {
            _ticketId = ticketId;
            InitializeComponent();
            this.Shown += MachineRunForm_Shown;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            timer?.Stop();
            timer?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblTitle = new Label();
            this.lblSubtitle = new Label();
            this.lblStopwatch = new Label();
            this.panelButton = new Panel();
            this.btnRun = new AppButton();
            this.btnBack = new AppButton();
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
            this.lblTitle.Font = AppFonts.MetricLarge;
            this.lblTitle.ForeColor = AppColors.TextInverse;
            this.lblTitle.Text = "PERBAIKAN SELESAI, MENUNGGU OPERATOR";

            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = false;
            this.lblSubtitle.Dock = DockStyle.Top;
            this.lblSubtitle.Height = 100;
            this.lblSubtitle.TextAlign = ContentAlignment.TopCenter;
            this.lblSubtitle.Font = AppFonts.Header2;
            this.lblSubtitle.ForeColor = Color.LightGray;
            this.lblSubtitle.Text = "Silakan validasi kondisi mesin.\nJika mesin sudah siap produksi, tekan tombol di bawah.";

            // 
            // lblStopwatch
            // 
            this.lblStopwatch.AutoSize = false;
            this.lblStopwatch.Dock = DockStyle.Top;
            this.lblStopwatch.Height = 120;
            this.lblStopwatch.TextAlign = ContentAlignment.MiddleCenter;
            this.lblStopwatch.Font = new Font("Segoe UI", 60F, FontStyle.Bold);
            this.lblStopwatch.ForeColor = Color.FromArgb(255, 193, 7); // Gold/Yellow
            this.lblStopwatch.Text = "00:00:00";

            // 
            // panelButton - Container for centering button
            // 
            this.panelButton.Dock = DockStyle.Fill;
            this.panelButton.BackColor = Color.Transparent;

            // 
            // btnRun
            // 
            this.btnRun.Anchor = AnchorStyles.None;
            this.btnRun.Text = "RUN MESIN (PRODUKSI)";
            this.btnRun.Type = AppButton.ButtonType.Primary;
            this.btnRun.BackColor = AppColors.Success; // Green for GO
            this.btnRun.Font = AppFonts.MetricMedium;
            this.btnRun.Size = new Size(500, 100);
            this.btnRun.Click += BtnRun_Click;
            
            //
            // btnBack
            //
            this.btnBack.Anchor = AnchorStyles.None;
            this.btnBack.Text = "KEMBALI KE PERBAIKAN";
            this.btnBack.Type = AppButton.ButtonType.Secondary;
            this.btnBack.Font = AppFonts.Header2;
            this.btnBack.Size = new Size(500, 80);
            this.btnBack.Click += BtnBack_Click;
            
            // Center buttons in panel
            this.panelButton.Resize += (s, e) => {
                 int totalHeight = this.btnRun.Height + 20 + this.btnBack.Height;
                 int startY = (this.panelButton.Height - totalHeight) / 2;
                 
                 this.btnRun.Location = new Point(
                    (this.panelButton.Width - this.btnRun.Width) / 2,
                    startY
                 );
                 
                 this.btnBack.Location = new Point(
                    (this.panelButton.Width - this.btnBack.Width) / 2,
                    startY + this.btnRun.Height + 20
                 );
            };
            
            this.panelButton.Controls.Add(this.btnRun);
            this.panelButton.Controls.Add(this.btnBack);

            // 
            // Controls
            // 
            this.Controls.Add(this.panelButton);
            this.Controls.Add(this.lblStopwatch);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
            
            this.ResumeLayout(false);
        }

        private void MachineRunForm_Shown(object sender, EventArgs e)
        {
            StartStopwatch();
        }

        private void StartStopwatch()
        {
            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            timer = new Timer();
            timer.Interval = 100; // Update every 100ms
            timer.Tick += Timer_Tick;
            timer.Enabled = true;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (stopwatch != null && stopwatch.IsRunning && lblStopwatch != null)
            {
                lblStopwatch.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            }
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            // ═══════════════════════════════════════════════════════════════════
            // OFFLINE MODE: Save production resumed locally
            // ═══════════════════════════════════════════════════════════════════
            if (_ticketId < 0)
            {
                try
                {
                    int pendingId = (int)Math.Abs(_ticketId);
                    var request = mtc_app.shared.infrastructure.ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
                    
                    if (request != null)
                    {
                        request.IsMachineRunning = 1; // Machine is Running
                        request.ProductionResumedAt = DateTime.Now;
                        mtc_app.shared.infrastructure.ServiceLocator.OfflineRepo.UpdatePendingTicket(pendingId, request);
                    }
                    
                    
                    stopwatch?.Stop();
                    timer?.Stop();
                    
                    // Navigate to Rating Form
                    using (var ratingForm = new OperatorRatingForm(_ticketId))
                    {
                        ratingForm.ShowDialog();
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error menyimpan offline: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            // ═══════════════════════════════════════════════════════════════════
            // ONLINE MODE: Save directly to database
            // ═══════════════════════════════════════════════════════════════════
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    // 1. Update Ticket: Set Production Resumed Time AND Machine State to Running
                    string sqlTicket = "UPDATE tickets SET production_resumed_at = NOW(), is_machine_running = 1 WHERE ticket_id = @Id";
                    connection.Execute(sqlTicket, new { Id = _ticketId });

                    // 2. Update Machine Status: Set to RUNNING (1)
                    // First, get the machine_id for this ticket
                    int machineId = connection.ExecuteScalar<int>("SELECT machine_id FROM tickets WHERE ticket_id = @Id", new { Id = _ticketId });
                    
                    string sqlMachine = "UPDATE machines SET current_status_id = 1 WHERE machine_id = @MachineId";
                    connection.Execute(sqlMachine, new { MachineId = machineId });
                }

                // MessageBox.Show("Mesin Running! Waktu produksi tercatat.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                stopwatch?.Stop();
                timer?.Stop();
                
                // Navigate to Rating Form
                using (var ratingForm = new OperatorRatingForm(_ticketId))
                {
                    ratingForm.ShowDialog();
                }

                // Close this form and return OK result
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            try
            {
                if (_ticketId > 0)
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();
                        // Revert ticket status to 2 (Repairing) and remove finished_at
                        connection.Execute("UPDATE tickets SET status_id = 2, technician_finished_at = NULL WHERE ticket_id = @Id", new { Id = _ticketId });
                        
                        // Revert the session to not completing and ended_at to NULL so timer continues correctly
                        connection.Execute("UPDATE ticket_technician_sessions SET is_completing_session = 0, ended_at = NULL WHERE ticket_id = @Id AND is_completing_session = 1", new { Id = _ticketId });
                    }
                }
                
                stopwatch?.Stop();
                timer?.Stop();
                
                // Return Cancel so the parent form (MachineHistoryFormTechnician) stays open
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal membatalkan status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
