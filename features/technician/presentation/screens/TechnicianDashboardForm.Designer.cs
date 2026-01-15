namespace mtc_app.features.technician.presentation.screens
{
    partial class TechnicianDashboardForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FlowLayoutPanel pnlTicketList;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Panel panelStatusBar;
        private System.Windows.Forms.Panel panelEmptyState;
        private mtc_app.shared.presentation.components.AppLabel labelTitle;
        private System.Windows.Forms.Label lblTicketCount;
        private System.Windows.Forms.Label lblLastUpdate;
        private System.Windows.Forms.Label lblSystemStatus;
        private System.Windows.Forms.PictureBox picStatusIndicator;
        private System.Windows.Forms.Label lblEmptyTitle;
        private System.Windows.Forms.Label lblEmptyMessage;
        private System.Windows.Forms.PictureBox picEmptyIcon;
        private System.Windows.Forms.Panel panelFilters;
        private System.Windows.Forms.ComboBox cmbFilterStatus;
        private System.Windows.Forms.ComboBox cmbSortTime;
        private System.Windows.Forms.Label lblFilterStatus;
        private System.Windows.Forms.Label lblSortTime;

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
            this.components = new System.ComponentModel.Container();
            this.pnlTicketList = new System.Windows.Forms.FlowLayoutPanel();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.panelStatusBar = new System.Windows.Forms.Panel();
            this.panelEmptyState = new System.Windows.Forms.Panel();
            this.labelTitle = new mtc_app.shared.presentation.components.AppLabel();
            this.lblTicketCount = new System.Windows.Forms.Label();
            this.lblLastUpdate = new System.Windows.Forms.Label();
            this.lblSystemStatus = new System.Windows.Forms.Label();
            this.picStatusIndicator = new System.Windows.Forms.PictureBox();
            this.lblEmptyTitle = new System.Windows.Forms.Label();
            this.lblEmptyMessage = new System.Windows.Forms.Label();
            this.picEmptyIcon = new System.Windows.Forms.PictureBox();
            this.panelFilters = new System.Windows.Forms.Panel();
            this.cmbFilterStatus = new System.Windows.Forms.ComboBox();
            this.cmbSortTime = new System.Windows.Forms.ComboBox();
            this.lblFilterStatus = new System.Windows.Forms.Label();
            this.lblSortTime = new System.Windows.Forms.Label();
            
            this.panelHeader.SuspendLayout();
            this.panelStatusBar.SuspendLayout();
            this.panelEmptyState.SuspendLayout();
            this.panelFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picStatusIndicator)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picEmptyIcon)).BeginInit();
            this.SuspendLayout();

            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.White;
            this.panelHeader.Controls.Add(this.lblTicketCount);
            this.panelHeader.Controls.Add(this.labelTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 120;
            this.panelHeader.Padding = new System.Windows.Forms.Padding(30, 20, 30, 20);

            // 
            // labelTitle
            // 
            this.labelTitle.Text = "Daftar Tunggu Perbaikan";
            this.labelTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Header2;
            this.labelTitle.ForeColor = mtc_app.shared.presentation.styles.AppColors.TextPrimary;
            this.labelTitle.Location = new System.Drawing.Point(30, 25);
            this.labelTitle.AutoSize = true;

            // 
            // lblTicketCount
            // 
            this.lblTicketCount.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblTicketCount.ForeColor = mtc_app.shared.presentation.styles.AppColors.Primary;
            this.lblTicketCount.Location = new System.Drawing.Point(30, 65);
            this.lblTicketCount.AutoSize = true;
            this.lblTicketCount.Text = "0 tiket menunggu";

            // 
            // panelStatusBar
            // 
            this.panelStatusBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(253)))), ((int)(((byte)(244)))));
            this.panelStatusBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelStatusBar.Height = 50;
            this.panelStatusBar.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.panelStatusBar.Controls.Add(this.lblLastUpdate);
            this.panelStatusBar.Controls.Add(this.lblSystemStatus);
            this.panelStatusBar.Controls.Add(this.picStatusIndicator);

            // 
            // picStatusIndicator
            // 
            this.picStatusIndicator.Size = new System.Drawing.Size(12, 12);
            this.picStatusIndicator.Location = new System.Drawing.Point(30, 19);
            this.picStatusIndicator.BackColor = System.Drawing.Color.Transparent;

            // 
            // lblSystemStatus
            // 
            this.lblSystemStatus.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSystemStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(128)))), ((int)(((byte)(61)))));
            this.lblSystemStatus.Location = new System.Drawing.Point(50, 16);
            this.lblSystemStatus.AutoSize = true;
            this.lblSystemStatus.Text = "Sistem Aktif";

            // 
            // lblLastUpdate
            // 
            this.lblLastUpdate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLastUpdate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblLastUpdate.Location = new System.Drawing.Point(150, 17);
            this.lblLastUpdate.AutoSize = true;
            this.lblLastUpdate.Text = "Terakhir diperbarui: -";

            // 
            // panelFilters
            // 
            this.panelFilters.BackColor = System.Drawing.Color.White;
            this.panelFilters.Controls.Add(this.lblSortTime);
            this.panelFilters.Controls.Add(this.cmbSortTime);
            this.panelFilters.Controls.Add(this.lblFilterStatus);
            this.panelFilters.Controls.Add(this.cmbFilterStatus);
            this.panelFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilters.Location = new System.Drawing.Point(0, 170);
            this.panelFilters.Name = "panelFilters";
            this.panelFilters.Size = new System.Drawing.Size(1200, 60);
            this.panelFilters.TabIndex = 4;
            // 
            // lblFilterStatus
            // 
            this.lblFilterStatus.AutoSize = true;
            this.lblFilterStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFilterStatus.Location = new System.Drawing.Point(30, 22);
            this.lblFilterStatus.Name = "lblFilterStatus";
            this.lblFilterStatus.Size = new System.Drawing.Size(76, 15);
            this.lblFilterStatus.TabIndex = 0;
            this.lblFilterStatus.Text = "Filter Status:";
            // 
            // cmbFilterStatus
            // 
            this.cmbFilterStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilterStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbFilterStatus.FormattingEnabled = true;
            this.cmbFilterStatus.Items.AddRange(new object[] {
            "Semua",
            "Belum Ditangani",
            "Sudah Direview GL"});
            this.cmbFilterStatus.Location = new System.Drawing.Point(115, 18);
            this.cmbFilterStatus.Name = "cmbFilterStatus";
            this.cmbFilterStatus.Size = new System.Drawing.Size(160, 23);
            this.cmbFilterStatus.TabIndex = 1;
            // 
            // lblSortTime
            // 
            this.lblSortTime.AutoSize = true;
            this.lblSortTime.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSortTime.Location = new System.Drawing.Point(300, 22);
            this.lblSortTime.Name = "lblSortTime";
            this.lblSortTime.Size = new System.Drawing.Size(89, 15);
            this.lblSortTime.TabIndex = 2;
            this.lblSortTime.Text = "Urutkan Waktu:";
            // 
            // cmbSortTime
            // 
            this.cmbSortTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSortTime.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbSortTime.FormattingEnabled = true;
            this.cmbSortTime.Items.AddRange(new object[] {
            "Terbaru",
            "Terlama"});
            this.cmbSortTime.Location = new System.Drawing.Point(395, 18);
            this.cmbSortTime.Name = "cmbSortTime";
            this.cmbSortTime.Size = new System.Drawing.Size(121, 23);
            this.cmbSortTime.TabIndex = 3;

            // 
            // panelEmptyState
            // 
            this.panelEmptyState.BackColor = System.Drawing.Color.Transparent;
            this.panelEmptyState.Size = new System.Drawing.Size(400, 300);
            this.panelEmptyState.Visible = false;
            this.panelEmptyState.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.panelEmptyState.Controls.Add(this.lblEmptyMessage);
            this.panelEmptyState.Controls.Add(this.lblEmptyTitle);
            this.panelEmptyState.Controls.Add(this.picEmptyIcon);

            // 
            // picEmptyIcon
            // 
            this.picEmptyIcon.Size = new System.Drawing.Size(80, 80);
            this.picEmptyIcon.Location = new System.Drawing.Point(160, 40);
            this.picEmptyIcon.BackColor = System.Drawing.Color.Transparent;

            // 
            // lblEmptyTitle
            // 
            this.lblEmptyTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 16F, System.Drawing.FontStyle.Bold);
            this.lblEmptyTitle.ForeColor = mtc_app.shared.presentation.styles.AppColors.TextPrimary;
            this.lblEmptyTitle.Text = "Tidak Ada Tiket Menunggu";
            this.lblEmptyTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmptyTitle.Location = new System.Drawing.Point(50, 140);
            this.lblEmptyTitle.Size = new System.Drawing.Size(300, 30);

            // 
            // lblEmptyMessage
            // 
            this.lblEmptyMessage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblEmptyMessage.ForeColor = mtc_app.shared.presentation.styles.AppColors.TextSecondary;
            this.lblEmptyMessage.Text = "Semua tiket telah diproses atau belum ada\nlaporan masalah baru dari operator.";
            this.lblEmptyMessage.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.lblEmptyMessage.Location = new System.Drawing.Point(50, 180);
            this.lblEmptyMessage.Size = new System.Drawing.Size(300, 60);

            // 
            // pnlTicketList
            // 
            this.pnlTicketList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTicketList.AutoScroll = true;
            this.pnlTicketList.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlTicketList.WrapContents = true;
            this.pnlTicketList.Padding = new System.Windows.Forms.Padding(20);
            this.pnlTicketList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.pnlTicketList.Controls.Add(this.panelEmptyState);

            // 
            // TechnicianDashboardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.pnlTicketList);
            this.Controls.Add(this.panelFilters);
            this.Controls.Add(this.panelStatusBar);
            this.Controls.Add(this.panelHeader);
            this.Name = "TechnicianDashboardForm";
            this.Text = "Dashboard Teknisi - Daftar Tunggu Perbaikan";
            
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelStatusBar.ResumeLayout(false);
            this.panelStatusBar.PerformLayout();
            this.panelFilters.ResumeLayout(false);
            this.panelFilters.PerformLayout();
            this.panelEmptyState.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picStatusIndicator)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picEmptyIcon)).EndInit();
            this.ResumeLayout(false);
        }
    }
}

