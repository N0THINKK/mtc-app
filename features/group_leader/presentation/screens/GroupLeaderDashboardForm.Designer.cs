using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.group_leader.presentation.screens
{
    partial class GroupLeaderDashboardForm
    {
        private System.ComponentModel.IContainer components = null;
        private Panel pnlHeader;
        private Label lblTitle;
        private Panel pnlFilters;
        private ComboBox cmbSortTime;
        private ComboBox cmbFilterStatus;
        private Label lblSort;
        private Label lblFilter;
        private FlowLayoutPanel flowTickets;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlFilters = new System.Windows.Forms.Panel();
            this.cmbSortTime = new System.Windows.Forms.ComboBox();
            this.cmbFilterStatus = new System.Windows.Forms.ComboBox();
            this.lblSort = new System.Windows.Forms.Label();
            this.lblFilter = new System.Windows.Forms.Label();
            this.flowTickets = new System.Windows.Forms.FlowLayoutPanel();
            
            this.pnlHeader.SuspendLayout();
            this.pnlFilters.SuspendLayout();
            this.SuspendLayout();

            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = AppColors.Primary;
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 60;
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(20, 15);
            this.lblTitle.Text = "DASHBOARD GROUP LEADER";

            // 
            // pnlFilters
            // 
            this.pnlFilters.BackColor = AppColors.Surface;
            this.pnlFilters.Controls.Add(this.cmbSortTime);
            this.pnlFilters.Controls.Add(this.lblSort);
            this.pnlFilters.Controls.Add(this.cmbFilterStatus);
            this.pnlFilters.Controls.Add(this.lblFilter);
            this.pnlFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilters.Height = 60;
            this.pnlFilters.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);

            // 
            // lblSort
            // 
            this.lblSort.AutoSize = true;
            this.lblSort.Font = AppFonts.BodySmall;
            this.lblSort.Location = new System.Drawing.Point(20, 20);
            this.lblSort.Text = "Urutkan Waktu:";

            // 
            // cmbSortTime
            // 
            this.cmbSortTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSortTime.FormattingEnabled = true;
            this.cmbSortTime.Items.AddRange(new object[] { "Terbaru", "Terlama" });
            this.cmbSortTime.Location = new System.Drawing.Point(120, 18);
            this.cmbSortTime.Width = 150;
            this.cmbSortTime.SelectedIndex = 0;
            this.cmbSortTime.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);

            // 
            // lblFilter
            // 
            this.lblFilter.AutoSize = true;
            this.lblFilter.Font = AppFonts.BodySmall;
            this.lblFilter.Location = new System.Drawing.Point(300, 20);
            this.lblFilter.Text = "Status Review:";

            // 
            // cmbFilterStatus
            // 
            this.cmbFilterStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilterStatus.FormattingEnabled = true;
            this.cmbFilterStatus.Items.AddRange(new object[] { "Semua", "Sudah Direview", "Belum Direview" });
            this.cmbFilterStatus.Location = new System.Drawing.Point(400, 18);
            this.cmbFilterStatus.Width = 150;
            this.cmbFilterStatus.SelectedIndex = 0;
            this.cmbFilterStatus.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);

            // 
            // flowTickets
            // 
            this.flowTickets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowTickets.AutoScroll = true;
            this.flowTickets.BackColor = AppColors.Background;
            this.flowTickets.Padding = new System.Windows.Forms.Padding(20);
            this.flowTickets.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowTickets.WrapContents = true;

            // 
            // Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 600);
            this.Controls.Add(this.flowTickets);
            this.Controls.Add(this.pnlFilters);
            this.Controls.Add(this.pnlHeader);
            this.Name = "GroupLeaderDashboardForm";
            this.Text = "Group Leader Dashboard";
            this.WindowState = FormWindowState.Maximized;

            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlFilters.ResumeLayout(false);
            this.pnlFilters.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
