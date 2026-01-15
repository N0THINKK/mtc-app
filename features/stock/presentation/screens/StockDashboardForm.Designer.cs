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
            this.pnlStatusCards = new System.Windows.Forms.Panel();
            this.cardCompleted = new mtc_app.features.stock.presentation.components.StockStatusCard();
            this.cardReady = new mtc_app.features.stock.presentation.components.StockStatusCard();
            this.cardPending = new mtc_app.features.stock.presentation.components.StockStatusCard();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.emptyStatePanel = new mtc_app.features.stock.presentation.components.EmptyStatePanel();
            this.flowLayoutPanelRequests = new System.Windows.Forms.FlowLayoutPanel();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.pnlHeader.SuspendLayout();
            this.pnlStatusCards.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(58)))), ((int)(((byte)(64)))));
            this.pnlHeader.Controls.Add(this.lblLastUpdate);
            this.pnlHeader.Controls.Add(this.labelTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.pnlHeader.Size = new System.Drawing.Size(1200, 70);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblLastUpdate
            // 
            this.lblLastUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLastUpdate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLastUpdate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(212)))), ((int)(((byte)(218)))));
            this.lblLastUpdate.Location = new System.Drawing.Point(900, 25);
            this.lblLastUpdate.Name = "lblLastUpdate";
            this.lblLastUpdate.Size = new System.Drawing.Size(280, 20);
            this.lblLastUpdate.TabIndex = 1;
            this.lblLastUpdate.Text = "Last updated: --:--:--";
            this.lblLastUpdate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(20, 20);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(280, 30);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "📦 Stock Control Dashboard";
            this.labelTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Header2;
            // 
            // pnlStatusCards
            // 
            this.pnlStatusCards.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlStatusCards.Controls.Add(this.cardCompleted);
            this.pnlStatusCards.Controls.Add(this.cardReady);
            this.pnlStatusCards.Controls.Add(this.cardPending);
            this.pnlStatusCards.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStatusCards.Location = new System.Drawing.Point(0, 70);
            this.pnlStatusCards.Name = "pnlStatusCards";
            this.pnlStatusCards.Padding = new System.Windows.Forms.Padding(20, 20, 20, 20);
            this.pnlStatusCards.Size = new System.Drawing.Size(1200, 160);
            this.pnlStatusCards.TabIndex = 1;
            // 
            // cardCompleted
            // 
            this.cardCompleted.BackColor = System.Drawing.Color.White;
            this.cardCompleted.Location = new System.Drawing.Point(460, 20);
            this.cardCompleted.Name = "cardCompleted";
            this.cardCompleted.Size = new System.Drawing.Size(200, 120);
            this.cardCompleted.TabIndex = 2;
            this.cardCompleted.Title = "Completed Today";
            this.cardCompleted.Type = mtc_app.features.stock.presentation.components.StockStatusCard.StatusType.Completed;
            this.cardCompleted.Value = "0";
            // 
            // cardReady
            // 
            this.cardReady.BackColor = System.Drawing.Color.White;
            this.cardReady.Location = new System.Drawing.Point(240, 20);
            this.cardReady.Name = "cardReady";
            this.cardReady.Size = new System.Drawing.Size(200, 120);
            this.cardReady.TabIndex = 1;
            this.cardReady.Title = "Ready for Pickup";
            this.cardReady.Type = mtc_app.features.stock.presentation.components.StockStatusCard.StatusType.Ready;
            this.cardReady.Value = "0";
            // 
            // cardPending
            // 
            this.cardPending.BackColor = System.Drawing.Color.White;
            this.cardPending.Location = new System.Drawing.Point(20, 20);
            this.cardPending.Name = "cardPending";
            this.cardPending.Size = new System.Drawing.Size(200, 120);
            this.cardPending.TabIndex = 0;
            this.cardPending.Title = "Pending Requests";
            this.cardPending.Type = mtc_app.features.stock.presentation.components.StockStatusCard.StatusType.Pending;
            this.cardPending.Value = "0";
            // 
            // pnlContent
            // 
            this.pnlContent.BackColor = System.Drawing.Color.White;
            this.pnlContent.Controls.Add(this.emptyStatePanel);
            this.pnlContent.Controls.Add(this.flowLayoutPanelRequests);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 230);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(20);
            this.pnlContent.Size = new System.Drawing.Size(1200, 470);
            this.pnlContent.TabIndex = 2;
            // 
            // emptyStatePanel
            // 
            this.emptyStatePanel.BackColor = System.Drawing.Color.White;
            this.emptyStatePanel.Description = "All requests have been processed. The system is working correctly.";
            this.emptyStatePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.emptyStatePanel.Icon = "📦";
            this.emptyStatePanel.Location = new System.Drawing.Point(20, 20);
            this.emptyStatePanel.Name = "emptyStatePanel";
            this.emptyStatePanel.Size = new System.Drawing.Size(1160, 430);
            this.emptyStatePanel.TabIndex = 1;
            this.emptyStatePanel.Title = "No Pending Requests";
            this.emptyStatePanel.Visible = false;
            // 
            // flowLayoutPanelRequests
            // 
            this.flowLayoutPanelRequests.AutoScroll = true;
            this.flowLayoutPanelRequests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelRequests.Location = new System.Drawing.Point(20, 20);
            this.flowLayoutPanelRequests.Name = "flowLayoutPanelRequests";
            this.flowLayoutPanelRequests.Padding = new System.Windows.Forms.Padding(10);
            this.flowLayoutPanelRequests.Size = new System.Drawing.Size(1160, 430);
            this.flowLayoutPanelRequests.TabIndex = 0;
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
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlStatusCards);
            this.Controls.Add(this.pnlHeader);
            this.Name = "StockDashboardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Stock Control Dashboard";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlStatusCards.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblLastUpdate;
        private mtc_app.shared.presentation.components.AppLabel labelTitle;
        private System.Windows.Forms.Panel pnlStatusCards;
        private mtc_app.features.stock.presentation.components.StockStatusCard cardPending;
        private mtc_app.features.stock.presentation.components.StockStatusCard cardReady;
        private mtc_app.features.stock.presentation.components.StockStatusCard cardCompleted;
        private System.Windows.Forms.Panel pnlContent;
        private mtc_app.features.stock.presentation.components.EmptyStatePanel emptyStatePanel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelRequests;
        private System.Windows.Forms.Timer timerRefresh;
    }
}