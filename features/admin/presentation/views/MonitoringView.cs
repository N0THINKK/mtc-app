using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.views
{
    public partial class MonitoringView : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        private DataGridView gridTickets;
        private Timer timerRefresh;
        private Label lblTitle;

        public MonitoringView()
        {
            InitializeComponent();
            LoadData();
            if (!this.DesignMode)
            {
                timerRefresh.Start();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle gridStyleHeader = new System.Windows.Forms.DataGridViewCellStyle();

            this.gridTickets = new System.Windows.Forms.DataGridView();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.lblTitle = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.gridTickets)).BeginInit();
            this.SuspendLayout();
            
            // Title
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = AppColors.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(268, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Monitoring Tiket Real-time";

            // Grid
            this.gridTickets.BackgroundColor = System.Drawing.Color.White;
            this.gridTickets.BorderStyle = BorderStyle.None;
            this.gridTickets.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridTickets.Dock = DockStyle.None; // Set later
            this.gridTickets.Location = new System.Drawing.Point(0, 40);
            this.gridTickets.Name = "gridTickets";
            this.gridTickets.Size = new System.Drawing.Size(860, 560);
            this.gridTickets.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Timer
            this.timerRefresh.Interval = 5000; // 5 seconds
            this.timerRefresh.Tick += new System.EventHandler(this.TimerRefresh_Tick);
            
            // This Control
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.gridTickets);
            this.Dock = DockStyle.Fill;


            ((System.ComponentModel.ISupportInitialize)(this.gridTickets)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void TimerRefresh_Tick(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    string sql = @"
                        SELECT 
                            t.ticket_display_code AS 'Kode Tiket',
                            ts.status_name AS 'Status',
                            m.machine_name AS 'Mesin',
                            u_op.full_name AS 'Operator',
                            u_tech.full_name AS 'Teknisi',
                            CONCAT(
                                IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
                                CASE 
                                    WHEN f.failure_name IS NOT NULL THEN f.failure_name
                                    WHEN t.failure_remarks IS NOT NULL THEN t.failure_remarks
                                    ELSE 'Belum Diisi'
                                END,
                                IF(t.applicator_code IS NOT NULL, CONCAT(' (App: ', t.applicator_code, ')'), '')
                            ) AS 'Keluhan',
                            t.created_at AS 'Waktu Lapor',
                            t.started_at AS 'Mulai Perbaikan',
                            t.technician_finished_at AS 'Selesai',
                            TIMEDIFF(t.technician_finished_at, t.created_at) AS 'Total Downtime'
                        FROM tickets t
                        LEFT JOIN machines m ON t.machine_id = m.machine_id
                        LEFT JOIN users u_op ON t.operator_id = u_op.user_id
                        LEFT JOIN users u_tech ON t.technician_id = u_tech.user_id
                        LEFT JOIN ticket_statuses ts ON t.status_id = ts.status_id
                        LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
                        LEFT JOIN failures f ON t.failure_id = f.failure_id
                        ORDER BY t.created_at DESC
                        LIMIT 100;";

                    var data = connection.Query(sql).ToList();
                    gridTickets.DataSource = data;
                    gridTickets.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                }
            }
            catch (Exception ex)
            {
                // Stop timer on error to prevent repeated popups
                timerRefresh.Stop();
                MessageBox.Show($"Gagal memuat data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
