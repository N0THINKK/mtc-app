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
                    SELECT * FROM view_admin_report 
                    WHERE `Waktu Lapor` BETWEEN @StartDate AND @EndDate
                    ORDER BY `Waktu Lapor` DESC";
                
                var reader = connection.ExecuteReader(sql, new { StartDate = startDate.Date, EndDate = endDate.Date.AddDays(1).AddSeconds(-1) });
                
                var dataTable = new DataTable();
                dataTable.Load(reader);

                // ==========================================================
                // 1. PROSES PEMISAHAN KOLOM DETAIL MASALAH (SESUAI DATABASE)
                // ==========================================================
                string colName = "Detail Masalah"; 
                
                if (dataTable.Columns.Contains(colName))
                {
                    dataTable.Columns.Add("Jenis Masalah", typeof(string));
                    dataTable.Columns.Add("Deskripsi Detail", typeof(string));
                    dataTable.Columns.Add("Nomor Aplikator", typeof(string));

                    // Geser 3 kolom baru ini ke posisi "Detail Masalah" yang lama
                    int colIndex = dataTable.Columns[colName].Ordinal;
                    dataTable.Columns["Jenis Masalah"].SetOrdinal(colIndex);
                    dataTable.Columns["Deskripsi Detail"].SetOrdinal(colIndex + 1);
                    dataTable.Columns["Nomor Aplikator"].SetOrdinal(colIndex + 2);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        // Contoh Data Asli dari database: "Aplikator: Cacat Crimp sisi A (App: 123)"
                        string rawData = row[colName]?.ToString() ?? "";
                        
                        string jenis = "-";
                        string deskripsi = "-";
                        string aplikator = "-";

                        // 1. Ekstrak Nomor Aplikator (Cari teks "(App: ")
                        if (rawData.Contains("(App: "))
                        {
                            int appIndex = rawData.IndexOf("(App: ");
                            // Ambil angka aplikator dan hilangkan tutup kurungnya ')'
                            aplikator = rawData.Substring(appIndex + 6).TrimEnd(')'); 
                            // Sisakan teks rawData untuk Jenis & Deskripsi saja
                            rawData = rawData.Substring(0, appIndex).Trim(); 
                        }

                        // 2. Ekstrak Jenis Masalah & Deskripsi Detail (Pisahkan berdasarkan ": ")
                        if (rawData.Contains(": "))
                        {
                            int colonIndex = rawData.IndexOf(": ");
                            jenis = rawData.Substring(0, colonIndex).Trim();
                            deskripsi = rawData.Substring(colonIndex + 2).Trim();
                        }
                        else
                        {
                            // Fallback jika tidak ada titik dua
                            deskripsi = rawData.Trim(); 
                        }

                        // Masukkan ke kolom masing-masing
                        row["Jenis Masalah"] = jenis;
                        row["Deskripsi Detail"] = deskripsi;
                        row["Nomor Aplikator"] = aplikator;
                    }
                    
                    // Hapus kolom Detail Masalah yang gabung
                    dataTable.Columns.Remove(colName);
                }

                // ==========================================================
                // 2. PROSES UBAH NAMA DAN URUTAN KOLOM DURASI (SESUAI DATABASE)
                // ==========================================================
                
                // Ubah "Durasi Respon" -> "Tunggu Teknisi"
                if (dataTable.Columns.Contains("Durasi Respon"))
                    dataTable.Columns["Durasi Respon"].ColumnName = "Tunggu Teknisi";

                // Menggunakan nama asli dari database yaitu "Waktu Tunggu Part"
                if (dataTable.Columns.Contains("Waktu Tunggu Part"))
                    dataTable.Columns["Waktu Tunggu Part"].ColumnName = "Tunggu Part";

                // Ubah "Durasi Trial Run" -> "Tunggu Operator"
                if (dataTable.Columns.Contains("Durasi Trial Run"))
                    dataTable.Columns["Durasi Trial Run"].ColumnName = "Tunggu Operator";

                // Mengatur urutan: "Tunggu Part" persis sebelum "Durasi Perbaikan"
                if (dataTable.Columns.Contains("Durasi Perbaikan") && dataTable.Columns.Contains("Tunggu Part"))
                {
                    int perbaikanIndex = dataTable.Columns["Durasi Perbaikan"].Ordinal;
                    dataTable.Columns["Tunggu Part"].SetOrdinal(perbaikanIndex);
                }

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
            this.lblTitle.Font = AppFonts.Header3;
            this.lblTitle.ForeColor = AppColors.TextPrimary;
            this.lblTitle.Location = new Point(0, 0);
            this.lblTitle.Text = "Buat Laporan Tiket (Excel)";
            
            // Date Start Label
            this.lblDateStart.AutoSize = true;
            this.lblDateStart.Font = AppFonts.BodySmall;
            this.lblDateStart.Location = new Point(0, 50);
            this.lblDateStart.Text = "Tanggal Mulai:";

            // Date Start Picker
            this.dateStart.Location = new Point(0, 75);
            this.dateStart.Size = new Size(200, 25);
            this.dateStart.Font = AppFonts.BodySmall;
            this.dateStart.Format = DateTimePickerFormat.Short;

            // Date End Label
            this.lblDateEnd.AutoSize = true;
            this.lblDateEnd.Font = AppFonts.BodySmall;
            this.lblDateEnd.Location = new Point(220, 50);
            this.lblDateEnd.Text = "Tanggal Akhir:";

            // Date End Picker
            this.dateEnd.Location = new Point(220, 75);
            this.dateEnd.Size = new Size(200, 25);
            this.dateEnd.Font = AppFonts.BodySmall;
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