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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.labelTitle = new mtc_app.shared.presentation.components.AppLabel();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.flowLayoutPanelRequests = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlHeader.SuspendLayout();
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
            this.labelTitle.Size = new System.Drawing.Size(300, 30);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Stock Control - Permintaan Part";
            this.labelTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Header2;
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            // 
            // timerRefresh
            // 
            this.timerRefresh.Interval = 30000;
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            // 
            // flowLayoutPanelRequests
            // 
            this.flowLayoutPanelRequests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelRequests.AutoScroll = true;
            this.flowLayoutPanelRequests.Padding = new System.Windows.Forms.Padding(10);
            this.flowLayoutPanelRequests.Location = new System.Drawing.Point(0, 60);
            this.flowLayoutPanelRequests.Name = "flowLayoutPanelRequests";
            this.flowLayoutPanelRequests.Size = new System.Drawing.Size(800, 400);
            this.flowLayoutPanelRequests.TabIndex = 1;
            // 
            // StockDashboardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 460);
            this.Controls.Add(this.flowLayoutPanelRequests);
            this.Controls.Add(this.pnlHeader);
            this.Name = "StockDashboardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Stock Dashboard";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private mtc_app.shared.presentation.components.AppLabel labelTitle;
        private System.Windows.Forms.Timer timerRefresh;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelRequests;
    }
}