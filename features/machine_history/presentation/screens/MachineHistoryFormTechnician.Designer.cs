namespace mtc_app.features.machine_history.presentation.screens
{
    partial class MachineHistoryFormTechnician
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
            this.panelHeader = new System.Windows.Forms.Panel();
            this.labelFinished = new mtc_app.shared.presentation.components.AppLabel();
            this.labelFinishedTitle = new mtc_app.shared.presentation.components.AppLabel();
            this.labelArrival = new mtc_app.shared.presentation.components.AppLabel();
            this.labelArrivalTitle = new mtc_app.shared.presentation.components.AppLabel();
            this.mainLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.panelFooter = new System.Windows.Forms.Panel();
            this.buttonRequestSparepart = new mtc_app.shared.presentation.components.AppButton();
            this.buttonRepairComplete = new mtc_app.shared.presentation.components.AppButton();
            this.panelHeader.SuspendLayout();
            this.panelFooter.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = mtc_app.shared.presentation.styles.AppColors.Primary;
            this.panelHeader.Controls.Add(this.labelFinished);
            this.panelHeader.Controls.Add(this.labelFinishedTitle);
            this.panelHeader.Controls.Add(this.labelArrival);
            this.panelHeader.Controls.Add(this.labelArrivalTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(450, 80);
            this.panelHeader.TabIndex = 0;
            // 
            // labelFinished
            // 
            this.labelFinished.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFinished.AutoSize = true;
            this.labelFinished.Font = new System.Drawing.Font("Consolas", 14F, System.Drawing.FontStyle.Bold);
            this.labelFinished.ForeColor = System.Drawing.Color.White;
            this.labelFinished.Location = new System.Drawing.Point(280, 40);
            this.labelFinished.Name = "labelFinished";
            this.labelFinished.Size = new System.Drawing.Size(100, 22);
            this.labelFinished.TabIndex = 3;
            this.labelFinished.Text = "00:00:00";
            this.labelFinished.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Body;
            this.labelFinished.ForeColor = System.Drawing.Color.White;
            // 
            // labelFinishedTitle
            // 
            this.labelFinishedTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFinishedTitle.AutoSize = true;
            this.labelFinishedTitle.ForeColor = System.Drawing.Color.White;
            this.labelFinishedTitle.Location = new System.Drawing.Point(280, 15);
            this.labelFinishedTitle.Name = "labelFinishedTitle";
            this.labelFinishedTitle.Size = new System.Drawing.Size(104, 19);
            this.labelFinishedTitle.TabIndex = 2;
            this.labelFinishedTitle.Text = "Selesai Reparasi";
            this.labelFinishedTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.BodySmall;
            this.labelFinishedTitle.ForeColor = System.Drawing.Color.White;
            // 
            // labelArrival
            // 
            this.labelArrival.AutoSize = true;
            this.labelArrival.Font = new System.Drawing.Font("Consolas", 14F, System.Drawing.FontStyle.Bold);
            this.labelArrival.ForeColor = System.Drawing.Color.White;
            this.labelArrival.Location = new System.Drawing.Point(15, 40);
            this.labelArrival.Name = "labelArrival";
            this.labelArrival.Size = new System.Drawing.Size(20, 22);
            this.labelArrival.TabIndex = 1;
            this.labelArrival.Text = "-";
            this.labelArrival.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Body;
            this.labelArrival.ForeColor = System.Drawing.Color.White;
            // 
            // labelArrivalTitle
            // 
            this.labelArrivalTitle.AutoSize = true;
            this.labelArrivalTitle.ForeColor = System.Drawing.Color.White;
            this.labelArrivalTitle.Location = new System.Drawing.Point(15, 15);
            this.labelArrivalTitle.Name = "labelArrivalTitle";
            this.labelArrivalTitle.Size = new System.Drawing.Size(123, 19);
            this.labelArrivalTitle.TabIndex = 0;
            this.labelArrivalTitle.Text = "Kedatangan Teknisi";
            this.labelArrivalTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.BodySmall;
            this.labelArrivalTitle.ForeColor = System.Drawing.Color.White;
            // 
            // mainLayout
            // 
            this.mainLayout.AutoScroll = true;
            this.mainLayout.BackColor = mtc_app.shared.presentation.styles.AppColors.Background;
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.mainLayout.Location = new System.Drawing.Point(0, 80);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(20);
            this.mainLayout.Size = new System.Drawing.Size(450, 420);
            this.mainLayout.TabIndex = 1;
            this.mainLayout.WrapContents = false;
            // 
            // panelFooter
            // 
            this.panelFooter.BackColor = mtc_app.shared.presentation.styles.AppColors.Surface;
            this.panelFooter.Controls.Add(this.buttonRequestSparepart);
            this.panelFooter.Controls.Add(this.buttonRepairComplete);
            this.panelFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelFooter.Location = new System.Drawing.Point(0, 500);
            this.panelFooter.Name = "panelFooter";
            this.panelFooter.Size = new System.Drawing.Size(450, 60);
            this.panelFooter.TabIndex = 2;
            this.panelFooter.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelFooter_Paint);
            // 
            // buttonRequestSparepart
            // 
            this.buttonRequestSparepart.Location = new System.Drawing.Point(20, 10);
            this.buttonRequestSparepart.Name = "buttonRequestSparepart";
            this.buttonRequestSparepart.Size = new System.Drawing.Size(200, 40);
            this.buttonRequestSparepart.TabIndex = 1;
            this.buttonRequestSparepart.Text = "Kirimkan Permintaan Sparepart";
            this.buttonRequestSparepart.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Warning;
            this.buttonRequestSparepart.Enabled = false;
            this.buttonRequestSparepart.Click += new System.EventHandler(this.buttonRequestSparepart_Click);
            // 
            // buttonRepairComplete
            // 
            this.buttonRepairComplete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRepairComplete.Location = new System.Drawing.Point(280, 10);
            this.buttonRepairComplete.Name = "buttonRepairComplete";
            this.buttonRepairComplete.Size = new System.Drawing.Size(150, 40);
            this.buttonRepairComplete.TabIndex = 0;
            this.buttonRepairComplete.Text = "Perbaikan Selesai";
            this.buttonRepairComplete.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Primary;
            this.buttonRepairComplete.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // MachineHistoryFormTechnician
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 560);
            this.Controls.Add(this.mainLayout);
            this.Controls.Add(this.panelFooter);
            this.Controls.Add(this.panelHeader);
            this.Name = "MachineHistoryFormTechnician";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Machine History - Technician";
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelFooter.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private mtc_app.shared.presentation.components.AppLabel labelArrivalTitle;
        private mtc_app.shared.presentation.components.AppLabel labelArrival;
        private mtc_app.shared.presentation.components.AppLabel labelFinishedTitle;
        private mtc_app.shared.presentation.components.AppLabel labelFinished;
        private System.Windows.Forms.FlowLayoutPanel mainLayout;
        private System.Windows.Forms.Panel panelFooter;
        private mtc_app.shared.presentation.components.AppButton buttonRepairComplete;
        private mtc_app.shared.presentation.components.AppButton buttonRequestSparepart;
    }
}