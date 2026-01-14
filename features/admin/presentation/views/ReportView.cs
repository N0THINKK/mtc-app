using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using ClosedXML.Excel;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.views
{
    public partial class ReportView : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblTitle, lblDateStart, lblDateEnd;
        private DateTimePicker dateStart, dateEnd;
        private AppButton btnExport;

        public ReportView()
        {
            InitializeComponent();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                saveFileDialog.Title = "Simpan Laporan Excel";
                saveFileDialog.FileName = $"Laporan_Tiket_{dateStart.Value:yyyy-MM-dd}_hingga_{dateEnd.Value:yyyy-MM-dd}.xlsx";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var data = FetchDataForReport(dateStart.Value, dateEnd.Value);

                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Data Tiket");
                            
                            // Insert data and create a table
                            var table = worksheet.Cell("A1").InsertTable(data);

                            // Style the header
                            worksheet.Row(1).Style.Font.Bold = true;
                            worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromColor(AppColors.Primary);
                            worksheet.Row(1).Style.Font.FontColor = XLColor.White;

                            // Adjust column widths
                            worksheet.Columns().AdjustToContents();

                            workbook.SaveAs(saveFileDialog.FileName);
                        }

                        MessageBox.Show($"Laporan berhasil disimpan di:\n{saveFileDialog.FileName}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Terjadi kesalahan saat membuat laporan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private DataTable FetchDataForReport(DateTime startDate, DateTime endDate)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
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
                    WHERE t.created_at BETWEEN @StartDate AND @EndDate
                    ORDER BY t.created_at DESC";
                
                var reader = connection.ExecuteReader(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1) });
                
                var dataTable = new DataTable();
                dataTable.Load(reader);
                return dataTable;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.lblTitle = new Label();
            this.lblDateStart = new Label();
            this.dateStart = new DateTimePicker();
            this.lblDateEnd = new Label();
            this.dateEnd = new DateTimePicker();
            this.btnExport = new AppButton();

            // Title
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblTitle.ForeColor = AppColors.TextPrimary;
            this.lblTitle.Location = new Point(0, 0);
            this.lblTitle.Text = "Buat Laporan Tiket (Excel)";
            
            // Date Start Label
            this.lblDateStart.AutoSize = true;
            this.lblDateStart.Font = new Font("Segoe UI", 10F);
            this.lblDateStart.Location = new Point(0, 50);
            this.lblDateStart.Text = "Tanggal Mulai:";

            // Date Start Picker
            this.dateStart.Location = new Point(0, 75);
            this.dateStart.Size = new Size(200, 25);
            this.dateStart.Font = new Font("Segoe UI", 10F);
            this.dateStart.Format = DateTimePickerFormat.Short;

            // Date End Label
            this.lblDateEnd.AutoSize = true;
            this.lblDateEnd.Font = new Font("Segoe UI", 10F);
            this.lblDateEnd.Location = new Point(220, 50);
            this.lblDateEnd.Text = "Tanggal Akhir:";

            // Date End Picker
            this.dateEnd.Location = new Point(220, 75);
            this.dateEnd.Size = new Size(200, 25);
            this.dateEnd.Font = new Font("Segoe UI", 10F);
            this.dateEnd.Format = DateTimePickerFormat.Short;

            // Export Button
            this.btnExport.Text = "Generate & Export Excel";
            this.btnExport.Location = new Point(0, 120);
            this.btnExport.Size = new Size(250, 50);
            this.btnExport.Click += BtnExport_Click;

            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblDateStart);
            this.Controls.Add(this.dateStart);
            this.Controls.Add(this.lblDateEnd);
            this.Controls.Add(this.dateEnd);
            this.Controls.Add(this.btnExport);
            this.Name = "ReportView";
            this.Dock = DockStyle.Fill;
            this.ResumeLayout(false);
        }
    }
}
