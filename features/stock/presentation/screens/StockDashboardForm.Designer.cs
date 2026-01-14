namespace mtc_app.features.stock.presentation.screens
{
    partial class StockDashboardForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.labelTitle = new mtc_app.shared.presentation.components.AppLabel();
            this.gridRequests = new System.Windows.Forms.DataGridView();
            this.btnReady = new mtc_app.shared.presentation.components.AppButton();
            this.btnRefresh = new mtc_app.shared.presentation.components.AppButton();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRequests)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = mtc_app.shared.presentation.styles.AppColors.Success; // Green for Stock
            this.pnlHeader.Controls.Add(this.labelTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(800, 60);
            this.pnlHeader.TabIndex = 0;
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(20, 15);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(248, 30);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Stock Control - Request";
            this.labelTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Header2;
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            // 
            // gridRequests
            // 
            this.gridRequests.AllowUserToAddRows = false;
            this.gridRequests.AllowUserToDeleteRows = false;
            this.gridRequests.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridRequests.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridRequests.BackgroundColor = mtc_app.shared.presentation.styles.AppColors.Surface;
            this.gridRequests.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridRequests.Location = new System.Drawing.Point(20, 80);
            this.gridRequests.Name = "gridRequests";
            this.gridRequests.ReadOnly = true;
            this.gridRequests.RowHeadersVisible = false;
            this.gridRequests.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridRequests.Size = new System.Drawing.Size(760, 300);
            this.gridRequests.TabIndex = 1;
            // 
            // btnReady
            // 
            this.btnReady.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReady.Location = new System.Drawing.Point(620, 400);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new System.Drawing.Size(160, 40);
            this.btnReady.TabIndex = 2;
            this.btnReady.Text = "SET READY";
            this.btnReady.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Primary;
            this.btnReady.Click += new System.EventHandler(this.btnReady_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRefresh.Location = new System.Drawing.Point(20, 400);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, 40);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Secondary;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // timerRefresh
            // 
            this.timerRefresh.Interval = 30000;
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            // 
            // StockDashboardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 460);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnReady);
            this.Controls.Add(this.gridRequests);
            this.Controls.Add(this.pnlHeader);
            this.Name = "StockDashboardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Stock Dashboard";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRequests)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private mtc_app.shared.presentation.components.AppLabel labelTitle;
        private System.Windows.Forms.DataGridView gridRequests;
        private mtc_app.shared.presentation.components.AppButton btnReady;
        private mtc_app.shared.presentation.components.AppButton btnRefresh;
        private System.Windows.Forms.Timer timerRefresh;
    }
}