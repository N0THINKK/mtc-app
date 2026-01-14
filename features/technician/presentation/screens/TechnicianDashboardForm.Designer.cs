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
            
            this.panelHeader.SuspendLayout();
            this.panelStatusBar.SuspendLayout();
            this.panelEmptyState.SuspendLayout();
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
            this.Controls.Add(this.panelStatusBar);
            this.Controls.Add(this.panelHeader);
            this.Name = "TechnicianDashboardForm";
            this.Text = "Dashboard Teknisi - Daftar Tunggu Perbaikan";
            
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelStatusBar.ResumeLayout(false);
            this.panelStatusBar.PerformLayout();
            this.panelEmptyState.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picStatusIndicator)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picEmptyIcon)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
