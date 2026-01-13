using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.screens
{
    public partial class AdminDashboardForm : AppBaseForm
    {
        private System.ComponentModel.IContainer components = null;
        private Panel pnlHeader;
        private Label labelTitle;
        private DataGridView gridTickets;
        private Timer timerRefresh;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle gridStyleHeader = new System.Windows.Forms.DataGridViewCellStyle();
            
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.labelTitle = new System.Windows.Forms.Label();
            this.gridTickets = new System.Windows.Forms.DataGridView();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);

            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridTickets)).BeginInit();
            this.SuspendLayout();

            // Header
            this.pnlHeader.BackColor = AppColors.Primary;
            this.pnlHeader.Controls.Add(this.labelTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 60;

            // Title
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(20, 15);
            this.labelTitle.Text = "MONITORING DASHBOARD";

            // Grid
            this.gridTickets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridTickets.BackgroundColor = AppColors.Surface;
            this.gridTickets.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridTickets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.gridTickets.RowHeadersVisible = false;
            this.gridTickets.AllowUserToAddRows = false;
            this.gridTickets.AllowUserToDeleteRows = false;
            this.gridTickets.ReadOnly = true;
            this.gridTickets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            
            // Grid Styling
            gridStyleHeader.BackColor = AppColors.Primary;
            gridStyleHeader.ForeColor = System.Drawing.Color.White;
            gridStyleHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.gridTickets.ColumnHeadersDefaultCellStyle = gridStyleHeader;
            this.gridTickets.EnableHeadersVisualStyles = false;

            // Timer
            this.timerRefresh.Interval = 5000; // 5 seconds
            this.timerRefresh.Tick += new System.EventHandler(this.TimerRefresh_Tick);

            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 600);
            this.Controls.Add(this.gridTickets);
            this.Controls.Add(this.pnlHeader);
            this.Name = "AdminDashboardForm";
            this.Text = "Admin Dashboard";
            this.WindowState = FormWindowState.Maximized;

            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridTickets)).EndInit();
            this.ResumeLayout(false);
        }
    }
}