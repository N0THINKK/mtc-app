namespace mtc_app.features.stock.presentation.screens
{
    partial class StockDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblLastUpdate = new System.Windows.Forms.Label();
            this.labelTitle = new mtc_app.shared.presentation.components.AppLabel();
            this.pnlFilters = new System.Windows.Forms.Panel();
            this.btnSortDesc = new mtc_app.shared.presentation.components.AppButton();
            this.btnSortAsc = new mtc_app.shared.presentation.components.AppButton();
            this.lblSortLabel = new System.Windows.Forms.Label();
            this.btnFilterAll = new mtc_app.shared.presentation.components.AppButton();
            this.btnFilterReady = new mtc_app.shared.presentation.components.AppButton();
            this.btnFilterPending = new mtc_app.shared.presentation.components.AppButton();
            this.lblFilterLabel = new System.Windows.Forms.Label();
            this.pnlStatusCards = new System.Windows.Forms.Panel();

            this.pnlContent = new System.Windows.Forms.Panel();
            this.gridRequests = new System.Windows.Forms.DataGridView();

            this.pnlActions = new System.Windows.Forms.Panel();
            this.btnReady = new mtc_app.shared.presentation.components.AppButton();
            this.btnRefresh = new mtc_app.shared.presentation.components.AppButton();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.pnlHeader.SuspendLayout();
            this.pnlFilters.SuspendLayout();
            this.pnlStatusCards.SuspendLayout();
            this.pnlContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRequests)).BeginInit();
            this.pnlActions.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = mtc_app.shared.presentation.styles.AppColors.Success;
            this.pnlHeader.Controls.Add(this.lblLastUpdate);
            this.pnlHeader.Controls.Add(this.labelTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(25, 18, 25, 18);
            this.pnlHeader.Size = new System.Drawing.Size(1200, 80);
            this.pnlHeader.TabIndex = 0;
            
            // 
            // lblLastUpdate
            // 
            this.lblLastUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLastUpdate.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.lblLastUpdate.ForeColor = System.Drawing.Color.White;
            this.lblLastUpdate.Location = new System.Drawing.Point(880, 28);
            this.lblLastUpdate.Name = "lblLastUpdate";
            this.lblLastUpdate.Size = new System.Drawing.Size(295, 24);
            this.lblLastUpdate.TabIndex = 1;
            this.lblLastUpdate.Text = "🕐 Terakhir diperbarui: --:--:--";
            this.lblLastUpdate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(20, 22);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(380, 37);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "📦 Kontrol Stok - Permintaan Part";
            
            // 
            // pnlStatusCards
            // 
            this.pnlStatusCards.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));

            this.pnlStatusCards.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStatusCards.Location = new System.Drawing.Point(0, 80);
            this.pnlStatusCards.Name = "pnlStatusCards";
            this.pnlStatusCards.Padding = new System.Windows.Forms.Padding(20, 25, 20, 25);
            this.pnlStatusCards.Size = new System.Drawing.Size(1200, 170);
            this.pnlStatusCards.TabIndex = 1;
            

            
            // 
            // pnlFilters
            // 
            this.pnlFilters.BackColor = System.Drawing.Color.White;
            this.pnlFilters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlFilters.Controls.Add(this.btnSortDesc);
            this.pnlFilters.Controls.Add(this.btnSortAsc);
            this.pnlFilters.Controls.Add(this.lblSortLabel);
            this.pnlFilters.Controls.Add(this.btnFilterAll);
            this.pnlFilters.Controls.Add(this.btnFilterReady);
            this.pnlFilters.Controls.Add(this.btnFilterPending);
            this.pnlFilters.Controls.Add(this.lblFilterLabel);
            this.pnlFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilters.Location = new System.Drawing.Point(0, 250);
            this.pnlFilters.Name = "pnlFilters";
            this.pnlFilters.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.pnlFilters.Size = new System.Drawing.Size(1200, 70);
            this.pnlFilters.TabIndex = 2;
            
            // 
            // lblFilterLabel
            // 
            this.lblFilterLabel.AutoSize = true;
            this.lblFilterLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblFilterLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblFilterLabel.Location = new System.Drawing.Point(20, 15);
            this.lblFilterLabel.Name = "lblFilterLabel";
            this.lblFilterLabel.Size = new System.Drawing.Size(50, 19);
            this.lblFilterLabel.TabIndex = 0;
            this.lblFilterLabel.Text = "Filter:";
            
            // 
            // btnFilterPending
            // 
            this.btnFilterPending.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterPending.Location = new System.Drawing.Point(80, 10);
            this.btnFilterPending.Name = "btnFilterPending";
            this.btnFilterPending.Size = new System.Drawing.Size(130, 38);
            this.btnFilterPending.TabIndex = 1;
            this.btnFilterPending.Text = "⏳ Pending";
            this.btnFilterPending.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Primary;
            this.btnFilterPending.Click += new System.EventHandler(this.btnFilterPending_Click);
            
            // 
            // btnFilterReady
            // 
            this.btnFilterReady.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterReady.Location = new System.Drawing.Point(220, 10);
            this.btnFilterReady.Name = "btnFilterReady";
            this.btnFilterReady.Size = new System.Drawing.Size(130, 38);
            this.btnFilterReady.TabIndex = 2;
            this.btnFilterReady.Text = "✓ Siap";
            this.btnFilterReady.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Secondary;
            this.btnFilterReady.Click += new System.EventHandler(this.btnFilterReady_Click);
            
            // 
            // btnFilterAll
            // 
            this.btnFilterAll.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterAll.Location = new System.Drawing.Point(360, 10);
            this.btnFilterAll.Name = "btnFilterAll";
            this.btnFilterAll.Size = new System.Drawing.Size(130, 38);
            this.btnFilterAll.TabIndex = 3;
            this.btnFilterAll.Text = "📋 Semua";
            this.btnFilterAll.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Secondary;
            this.btnFilterAll.Click += new System.EventHandler(this.btnFilterAll_Click);
            
            // 
            // lblSortLabel
            // 
            this.lblSortLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSortLabel.AutoSize = true;
            this.lblSortLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSortLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblSortLabel.Location = new System.Drawing.Point(850, 15);
            this.lblSortLabel.Name = "lblSortLabel";
            this.lblSortLabel.Size = new System.Drawing.Size(55, 19);
            this.lblSortLabel.TabIndex = 4;
            this.lblSortLabel.Text = "Urutkan:";
            
            // 
            // btnSortAsc
            // 
            this.btnSortAsc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSortAsc.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnSortAsc.Location = new System.Drawing.Point(915, 10);
            this.btnSortAsc.Name = "btnSortAsc";
            this.btnSortAsc.Size = new System.Drawing.Size(120, 38);
            this.btnSortAsc.TabIndex = 5;
            this.btnSortAsc.Text = "↑ Terlama";
            this.btnSortAsc.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Secondary;
            this.btnSortAsc.Click += new System.EventHandler(this.btnSortAsc_Click);
            
            // 
            // btnSortDesc
            // 
            this.btnSortDesc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSortDesc.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnSortDesc.Location = new System.Drawing.Point(1045, 10);
            this.btnSortDesc.Name = "btnSortDesc";
            this.btnSortDesc.Size = new System.Drawing.Size(120, 38);
            this.btnSortDesc.TabIndex = 6;
            this.btnSortDesc.Text = "↓ Terbaru";
            this.btnSortDesc.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Primary;
            this.btnSortDesc.Click += new System.EventHandler(this.btnSortDesc_Click);
            
            // 
            // pnlContent
            // 
            this.pnlContent.BackColor = System.Drawing.Color.White;

            this.pnlContent.Controls.Add(this.gridRequests);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 320);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(25, 20, 25, 20);
            this.pnlContent.Size = new System.Drawing.Size(1200, 310);
            this.pnlContent.TabIndex = 3;
            
            // 
            // gridRequests
            // 
            this.gridRequests.AllowUserToAddRows = false;
            this.gridRequests.AllowUserToDeleteRows = false;
            this.gridRequests.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridRequests.BackgroundColor = System.Drawing.Color.White;
            this.gridRequests.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridRequests.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.gridRequests.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.gridRequests.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridRequests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridRequests.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(226)))), ((int)(((byte)(230)))));
            this.gridRequests.Location = new System.Drawing.Point(25, 20);
            this.gridRequests.Name = "gridRequests";
            this.gridRequests.ReadOnly = true;
            this.gridRequests.RowHeadersVisible = false;
            this.gridRequests.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridRequests.Size = new System.Drawing.Size(1150, 270);
            this.gridRequests.TabIndex = 0;
            

            
            // 
            // pnlActions
            // 
            this.pnlActions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlActions.Controls.Add(this.btnReady);
            this.pnlActions.Controls.Add(this.btnRefresh);
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlActions.Location = new System.Drawing.Point(0, 630);
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.Padding = new System.Windows.Forms.Padding(25, 18, 25, 18);
            this.pnlActions.Size = new System.Drawing.Size(1200, 70);
            this.pnlActions.TabIndex = 4;
            
            // 
            // btnRefresh
            // 
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            this.btnRefresh.Location = new System.Drawing.Point(25, 15);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(130, 40);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "🔄 Refresh";
            this.btnRefresh.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Secondary;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            
            // 
            // btnReady
            // 
            this.btnReady.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReady.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnReady.Location = new System.Drawing.Point(1015, 15);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new System.Drawing.Size(160, 40);
            this.btnReady.TabIndex = 1;
            this.btnReady.Text = "✓ TANDAI SIAP";
            this.btnReady.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Primary;
            this.btnReady.Click += new System.EventHandler(this.btnReady_Click);
            
            // 
            // timerRefresh
            // 
            this.timerRefresh.Interval = 30000;
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            
            // 
            // StockDashboardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlFilters);
            this.Controls.Add(this.pnlActions);
            this.Controls.Add(this.pnlStatusCards);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "StockDashboardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Kontrol Stok - Permintaan Part";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlFilters.ResumeLayout(false);
            this.pnlFilters.PerformLayout();
            this.pnlStatusCards.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridRequests)).EndInit();
            this.pnlActions.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblLastUpdate;
        private mtc_app.shared.presentation.components.AppLabel labelTitle;
        private System.Windows.Forms.Panel pnlStatusCards;

        private System.Windows.Forms.Panel pnlFilters;
        private System.Windows.Forms.Label lblFilterLabel;
        private mtc_app.shared.presentation.components.AppButton btnFilterPending;
        private mtc_app.shared.presentation.components.AppButton btnFilterReady;
        private mtc_app.shared.presentation.components.AppButton btnFilterAll;
        private System.Windows.Forms.Label lblSortLabel;
        private mtc_app.shared.presentation.components.AppButton btnSortAsc;
        private mtc_app.shared.presentation.components.AppButton btnSortDesc;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.DataGridView gridRequests;

        private System.Windows.Forms.Panel pnlActions;
        private mtc_app.shared.presentation.components.AppButton btnReady;
        private mtc_app.shared.presentation.components.AppButton btnRefresh;
        private System.Windows.Forms.Timer timerRefresh;
    }
}