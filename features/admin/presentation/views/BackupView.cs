using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using MySqlConnector;

namespace mtc_app.features.admin.presentation.views
{
    public partial class BackupView : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblTitle;
        private Label lblDescription;
        private AppButton btnCreateBackup;

        public BackupView()
        {
            InitializeComponent();
        }

        private void BtnCreateBackup_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "SQL File (*.sql)|*.sql";
                saveFileDialog.Title = "Simpan Backup Database";
                saveFileDialog.FileName = $"backup_db_maintenance_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.sql";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var connectionStringBuilder = new MySqlConnectionStringBuilder(DatabaseHelper.ConnectionString);
                        string server = connectionStringBuilder.Server;
                        string user = connectionStringBuilder.UserID;
                        string password = connectionStringBuilder.Password;
                        string database = connectionStringBuilder.Database;
                        string filePath = saveFileDialog.FileName;

                        string command = $"--user={user} --password={password} --host={server} --protocol=tcp --port=3306 --default-character-set=utf8 --routines --events --result-file=\"{filePath}\" \"{database}\" ";

                        Process process = new Process();
                        process.StartInfo.FileName = "mysqldump"; // Assumes mysqldump is in PATH
                        process.StartInfo.Arguments = command;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();
                        
                        string stderr = process.StandardError.ReadToEnd();
                        
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            MessageBox.Show($"Backup database berhasil disimpan di:\n{filePath}", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                             // Check for common error "command not found"
                            if (stderr.Contains("is not recognized") || stderr.Contains("No such file or directory"))
                            {
                                 MessageBox.Show("Error: Perintah 'mysqldump' tidak ditemukan.\n\nPastikan folder 'bin' dari instalasi MariaDB/MySQL sudah ditambahkan ke dalam PATH environment variable sistem Anda, lalu restart aplikasi.", "Konfigurasi Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show($"Backup Gagal.\nError: {stderr}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
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
            this.lblDescription = new Label();
            this.btnCreateBackup = new AppButton();

            // Title
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = AppFonts.Header3;
            this.lblTitle.ForeColor = AppColors.TextPrimary;
            this.lblTitle.Location = new Point(0, 0);
            this.lblTitle.Text = "Backup Database";
            
            // Description
            this.lblDescription.AutoSize = true;
            this.lblDescription.Font = AppFonts.BodySmall;
            this.lblDescription.ForeColor = AppColors.TextSecondary;
            this.lblDescription.Location = new Point(0, 35);
            this.lblDescription.MaximumSize = new Size(600, 0);
            this.lblDescription.Text = "Fitur ini akan membuat salinan lengkap dari database 'db_maintenance' (termasuk struktur tabel dan semua datanya) ke dalam sebuah file .sql. Anda bisa menggunakan file ini untuk restore jika terjadi masalah.";
            
            // Backup Button
            this.btnCreateBackup.Text = "Buat & Simpan Backup...";
            this.btnCreateBackup.Location = new Point(0, 100);
            this.btnCreateBackup.Size = new Size(250, 50);
            this.btnCreateBackup.Click += BtnCreateBackup_Click;

            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.btnCreateBackup);
            this.Name = "BackupView";
            this.Dock = DockStyle.Fill;
            this.ResumeLayout(false);
        }
    }
}
