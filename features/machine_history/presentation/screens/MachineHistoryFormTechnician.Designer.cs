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
            this.labelFinished = new System.Windows.Forms.Label();
            this.labelFinishedTitle = new System.Windows.Forms.Label();
            this.labelArrival = new System.Windows.Forms.Label();
            this.labelArrivalTitle = new System.Windows.Forms.Label();
            this.mainLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.panelFooter = new System.Windows.Forms.Panel();
            this.buttonSendSparepartFooter = new System.Windows.Forms.Button();
            this.buttonRepairComplete = new System.Windows.Forms.Button();
            this.panelHeader.SuspendLayout();
            this.panelFooter.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(118)))), ((int)(((byte)(210)))));
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
            // 
            // labelFinishedTitle
            // 
            this.labelFinishedTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFinishedTitle.AutoSize = true;
            this.labelFinishedTitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.labelFinishedTitle.ForeColor = System.Drawing.Color.White;
            this.labelFinishedTitle.Location = new System.Drawing.Point(280, 15);
            this.labelFinishedTitle.Name = "labelFinishedTitle";
            this.labelFinishedTitle.Size = new System.Drawing.Size(104, 19);
            this.labelFinishedTitle.TabIndex = 2;
            this.labelFinishedTitle.Text = "Selesai Reparasi";
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
            // 
            // labelArrivalTitle
            // 
            this.labelArrivalTitle.AutoSize = true;
            this.labelArrivalTitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.labelArrivalTitle.ForeColor = System.Drawing.Color.White;
            this.labelArrivalTitle.Location = new System.Drawing.Point(15, 15);
            this.labelArrivalTitle.Name = "labelArrivalTitle";
            this.labelArrivalTitle.Size = new System.Drawing.Size(123, 19);
            this.labelArrivalTitle.TabIndex = 0;
            this.labelArrivalTitle.Text = "Kedatangan Teknisi";
            // 
            // mainLayout
            // 
            this.mainLayout.AutoScroll = true;
            this.mainLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
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
            this.panelFooter.BackColor = System.Drawing.Color.White;
            this.panelFooter.Controls.Add(this.buttonSendSparepartFooter);
            this.panelFooter.Controls.Add(this.buttonRepairComplete);
            this.panelFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelFooter.Location = new System.Drawing.Point(0, 500);
            this.panelFooter.Name = "panelFooter";
            this.panelFooter.Size = new System.Drawing.Size(450, 60);
            this.panelFooter.TabIndex = 2;
            this.panelFooter.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelFooter_Paint);
            // 
            // buttonSendSparepartFooter
            // 
            this.buttonSendSparepartFooter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(152)))), ((int)(((byte)(0)))));
            this.buttonSendSparepartFooter.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonSendSparepartFooter.FlatAppearance.BorderSize = 0;
            this.buttonSendSparepartFooter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSendSparepartFooter.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.buttonSendSparepartFooter.ForeColor = System.Drawing.Color.White;
            this.buttonSendSparepartFooter.Location = new System.Drawing.Point(20, 10);
            this.buttonSendSparepartFooter.Name = "buttonSendSparepartFooter";
            this.buttonSendSparepartFooter.Size = new System.Drawing.Size(200, 40);
            this.buttonSendSparepartFooter.TabIndex = 1;
            this.buttonSendSparepartFooter.Text = "Kirimkan Permintaan Sparepart";
            this.buttonSendSparepartFooter.UseVisualStyleBackColor = false;
            this.buttonSendSparepartFooter.Enabled = false;
            this.buttonSendSparepartFooter.Click += new System.EventHandler(this.buttonSendSparepart_Click);
            // 
            // buttonRepairComplete
            // 
            this.buttonRepairComplete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRepairComplete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(118)))), ((int)(((byte)(210)))));
            this.buttonRepairComplete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonRepairComplete.FlatAppearance.BorderSize = 0;
            this.buttonRepairComplete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonRepairComplete.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.buttonRepairComplete.ForeColor = System.Drawing.Color.White;
            this.buttonRepairComplete.Location = new System.Drawing.Point(280, 10);
            this.buttonRepairComplete.Name = "buttonRepairComplete";
            this.buttonRepairComplete.Size = new System.Drawing.Size(150, 40);
            this.buttonRepairComplete.TabIndex = 0;
            this.buttonRepairComplete.Text = "Perbaikan Selesai";
            this.buttonRepairComplete.UseVisualStyleBackColor = false;
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
        private System.Windows.Forms.Label labelArrivalTitle;
        private System.Windows.Forms.Label labelArrival;
        private System.Windows.Forms.Label labelFinishedTitle;
        private System.Windows.Forms.Label labelFinished;
        private System.Windows.Forms.FlowLayoutPanel mainLayout;
        private System.Windows.Forms.Panel panelFooter;
        private System.Windows.Forms.Button buttonRepairComplete;
        private System.Windows.Forms.Button buttonSendSparepartFooter;
    }
}