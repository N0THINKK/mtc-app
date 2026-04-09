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
            this.labelTitle.ForeColor = mtc_app.shared.presentation.styles.AppColors.TextInverse;
            this.labelTitle.Location = new System.Drawing.Point(15, 25);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(155, 25);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Machine History";
            this.labelTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Header3;
            // The AppLabel should handle its own color, but if on a dark background, white is needed.
            // We'll rely on the component's logic or a potential future style property. For now, this is cleaner.

            // 
            // mainLayout
            // 
            this.mainLayout.AutoScroll = true;
            this.mainLayout.BackColor = mtc_app.shared.presentation.styles.AppColors.Background;
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.mainLayout.Location = new System.Drawing.Point(0, 80);
            this.mainLayout.Name = "mainLayout";
            // Padding: Left, Top, Right, Bottom (100 for Safe Area)
            this.mainLayout.Padding = new System.Windows.Forms.Padding(20, 10, 20, 100);
            this.mainLayout.Size = new System.Drawing.Size(450, 420);
            this.mainLayout.TabIndex = 1;
            this.mainLayout.WrapContents = false;

            // 
            // panelFooter
            // 
            this.panelFooter.BackColor = mtc_app.shared.presentation.styles.AppColors.Surface;
            // this.panelFooter.Controls.Add(this.buttonSave); // Removed
            this.panelFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelFooter.Location = new System.Drawing.Point(0, 500);
            this.panelFooter.Name = "panelFooter";
            // Footer 450x60 -> 500x80
            this.panelFooter.Size = new System.Drawing.Size(500, 80);
            this.panelFooter.TabIndex = 2;
            this.panelFooter.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelFooter_Paint);

            // 
            // MachineHistoryFormOperator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // Client size 450x560 -> 500x600
            this.ClientSize = new System.Drawing.Size(500, 600);
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
    }
}