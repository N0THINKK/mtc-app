using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.group_leader.presentation.screens
{
    partial class GroupLeaderDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        // Deklarasi Variabel UI (Disesuaikan dengan nama variabel di logic)
        private Panel panelHeader;
        private Panel panelStatusBar;
        private Panel panelFilters;
        private Panel panelEmptyState;
        private Label labelTitle;
        private Label lblTicketStats;
        private Label lblLastUpdate;
        private Label lblSystemStatus;
        private PictureBox picStatusIndicator;
        private ComboBox cmbSortTime;
        private ComboBox cmbFilterStatus;
        private Label lblSort;
        private Label lblFilter;
        private FlowLayoutPanel flowTickets;
        private Label lblEmptyTitle;
        private Label lblEmptyMessage;
        private PictureBox picEmptyIcon;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.panelStatusBar = new System.Windows.Forms.Panel();
            this.panelFilters = new System.Windows.Forms.Panel();
            this.panelEmptyState = new System.Windows.Forms.Panel();
            this.labelTitle = new System.Windows.Forms.Label();
            this.lblTicketStats = new System.Windows.Forms.Label();
            this.lblLastUpdate = new System.Windows.Forms.Label();
            this.lblSystemStatus = new System.Windows.Forms.Label();
            this.picStatusIndicator = new System.Windows.Forms.PictureBox();
            this.cmbSortTime = new System.Windows.Forms.ComboBox();
            this.cmbFilterStatus = new System.Windows.Forms.ComboBox();
            this.lblSort = new System.Windows.Forms.Label();
            this.lblFilter = new System.Windows.Forms.Label();
            this.flowTickets = new System.Windows.Forms.FlowLayoutPanel();
            this.lblEmptyTitle = new System.Windows.Forms.Label();
            this.lblEmptyMessage = new System.Windows.Forms.Label();
            this.picEmptyIcon = new System.Windows.Forms.PictureBox();

            this.panelHeader.SuspendLayout();
            this.panelStatusBar.SuspendLayout();
            this.panelFilters.SuspendLayout();
            this.panelEmptyState.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picStatusIndicator)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picEmptyIcon)).BeginInit();
            this.SuspendLayout();

            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.White;
            this.panelHeader.Controls.Add(this.lblTicketStats);
            this.panelHeader.Controls.Add(this.labelTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 120;
            this.panelHeader.Padding = new System.Windows.Forms.Padding(30, 20, 30, 20);
            this.panelHeader.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230)),
                    0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };

            // 
            // labelTitle
            // 
            this.labelTitle.Text = "Dashboard Group Leader";
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = AppColors.TextPrimary;
            this.labelTitle.Location = new System.Drawing.Point(30, 25);
            this.labelTitle.AutoSize = true;

            // 
            // lblTicketStats
            // 
            this.lblTicketStats.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblTicketStats.ForeColor = AppColors.Primary;
            this.lblTicketStats.Location = new System.Drawing.Point(30, 70);
            this.lblTicketStats.AutoSize = true;
            this.lblTicketStats.Text = "Total: 0 | Direview: 0 | Belum: 0";

            // 
            // panelStatusBar
            // 
            this.panelStatusBar.BackColor = System.Drawing.Color.FromArgb(240, 253, 244);
            this.panelStatusBar.Controls.Add(this.lblLastUpdate);
            this.panelStatusBar.Controls.Add(this.lblSystemStatus);
            this.panelStatusBar.Controls.Add(this.picStatusIndicator);
            this.panelStatusBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelStatusBar.Height = 50;
            this.panelStatusBar.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);

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
            this.lblSystemStatus.ForeColor = System.Drawing.Color.FromArgb(21, 128, 61);
            this.lblSystemStatus.Location = new System.Drawing.Point(50, 16);
            this.lblSystemStatus.AutoSize = true;
            this.lblSystemStatus.Text = "Sistem Aktif";

            // 
            // lblLastUpdate
            // 
            this.lblLastUpdate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLastUpdate.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblLastUpdate.Location = new System.Drawing.Point(150, 17);
            this.lblLastUpdate.AutoSize = true;
            this.lblLastUpdate.Text = "Terakhir diperbarui: -";

            // 
            // panelFilters
            // 
            this.panelFilters.BackColor = System.Drawing.Color.White;
            this.panelFilters.Controls.Add(this.cmbFilterStatus);
            this.panelFilters.Controls.Add(this.lblFilter);
            this.panelFilters.Controls.Add(this.cmbSortTime);
            this.panelFilters.Controls.Add(this.lblSort);
            this.panelFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilters.Height = 80;
            this.panelFilters.Padding = new System.Windows.Forms.Padding(30, 20, 30, 20);

            // 
            // lblSort
            // 
            this.lblSort.AutoSize = true;
            this.lblSort.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSort.ForeColor = AppColors.TextPrimary;
            this.lblSort.Location = new System.Drawing.Point(30, 25);
            this.lblSort.Text = "Urutkan:";

            // 
            // cmbSortTime
            // 
            this.cmbSortTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSortTime.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbSortTime.FormattingEnabled = true;
            this.cmbSortTime.Items.AddRange(new object[] { "Terbaru", "Terlama" });
            this.cmbSortTime.Location = new System.Drawing.Point(110, 22);
            this.cmbSortTime.Width = 150;
            this.cmbSortTime.SelectedIndex = 0;
            this.cmbSortTime.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);

            // 
            // lblFilter
            // 
            this.lblFilter.AutoSize = true;
            this.lblFilter.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblFilter.ForeColor = AppColors.TextPrimary;
            this.lblFilter.Location = new System.Drawing.Point(300, 25);
            this.lblFilter.Text = "Status:";

            // 
            // cmbFilterStatus
            // 
            this.cmbFilterStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilterStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbFilterStatus.FormattingEnabled = true;
            this.cmbFilterStatus.Items.AddRange(new object[] { "Semua", "Sudah Direview", "Belum Direview" });
            this.cmbFilterStatus.Location = new System.Drawing.Point(365, 22);
            this.cmbFilterStatus.Width = 180;
            this.cmbFilterStatus.SelectedIndex = 0;
            this.cmbFilterStatus.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);

            // 
            // panelEmptyState
            // 
            this.panelEmptyState.BackColor = System.Drawing.Color.Transparent;
            this.panelEmptyState.Controls.Add(this.lblEmptyMessage);
            this.panelEmptyState.Controls.Add(this.lblEmptyTitle);
            this.panelEmptyState.Controls.Add(this.picEmptyIcon);
            this.panelEmptyState.Size = new System.Drawing.Size(400, 300);
            this.panelEmptyState.Visible = false;
            this.panelEmptyState.Anchor = System.Windows.Forms.AnchorStyles.None;

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
            this.lblEmptyTitle.ForeColor = AppColors.TextPrimary;
            this.lblEmptyTitle.Text = "Tidak Ada Data Tiket";
            this.lblEmptyTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmptyTitle.Location = new System.Drawing.Point(50, 140);
            this.lblEmptyTitle.Size = new System.Drawing.Size(300, 30);

            // 
            // lblEmptyMessage
            // 
            this.lblEmptyMessage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblEmptyMessage.ForeColor = AppColors.TextSecondary;
            this.lblEmptyMessage.Text = "Belum ada tiket yang dapat ditampilkan.\nData akan muncul setelah teknisi menyelesaikan perbaikan.";
            this.lblEmptyMessage.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.lblEmptyMessage.Location = new System.Drawing.Point(50, 180);
            this.lblEmptyMessage.Size = new System.Drawing.Size(300, 60);

            // 
            // flowTickets
            // 
            this.flowTickets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowTickets.AutoScroll = true;
            this.flowTickets.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowTickets.WrapContents = true;
            this.flowTickets.Padding = new System.Windows.Forms.Padding(20);
            this.flowTickets.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.flowTickets.Controls.Add(this.panelEmptyState);

            // 
            // GroupLeaderDashboardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 800);
            this.Controls.Add(this.flowTickets);
            this.Controls.Add(this.panelFilters);
            this.Controls.Add(this.panelStatusBar);
            this.Controls.Add(this.panelHeader);
            this.Name = "GroupLeaderDashboardForm";
            this.Text = "Dashboard Group Leader";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);

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