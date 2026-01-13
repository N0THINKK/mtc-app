namespace mtc_app.features.machine_history.presentation.screens
{
    partial class MachineHistoryFormOperator
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
            this.labelTitle = new mtc_app.shared.presentation.components.AppLabel();
            this.mainLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.panelFooter = new System.Windows.Forms.Panel();
            this.buttonSave = new mtc_app.shared.presentation.components.AppButton();

            this.panelHeader.SuspendLayout();
            this.panelFooter.SuspendLayout();
            this.SuspendLayout();

            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = mtc_app.shared.presentation.styles.AppColors.Primary;
            this.panelHeader.Controls.Add(this.labelTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(450, 80);
            this.panelHeader.TabIndex = 0;

            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(15, 25);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(155, 25);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Machine History";
            this.labelTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Header3;
            // Overriding color because Header3 is usually dark, but here we want white on blue
            this.labelTitle.ForeColor = System.Drawing.Color.White;

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
            this.panelFooter.Controls.Add(this.buttonSave);
            this.panelFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelFooter.Location = new System.Drawing.Point(0, 500);
            this.panelFooter.Name = "panelFooter";
            this.panelFooter.Size = new System.Drawing.Size(450, 60);
            this.panelFooter.TabIndex = 2;
            this.panelFooter.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelFooter_Paint);

            // 
            // buttonSave
            // 
            this.buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSave.Location = new System.Drawing.Point(280, 10);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(150, 40);
            this.buttonSave.TabIndex = 0;
            this.buttonSave.Text = "Panggil Teknisi";
            this.buttonSave.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Primary;
            this.buttonSave.Click += new System.EventHandler(this.SaveButton_Click);

            // 
            // MachineHistoryFormOperator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 560);
            this.Controls.Add(this.mainLayout);
            this.Controls.Add(this.panelFooter);
            this.Controls.Add(this.panelHeader);
            this.Name = "MachineHistoryFormOperator";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Machine History";
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelFooter.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private mtc_app.shared.presentation.components.AppLabel labelTitle;
        private System.Windows.Forms.FlowLayoutPanel mainLayout;
        private System.Windows.Forms.Panel panelFooter;
        private mtc_app.shared.presentation.components.AppButton buttonSave;
    }
}