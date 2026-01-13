namespace mtc_app.features.authentication.presentation.screens
{
    partial class SetupForm
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
            this.label1 = new mtc_app.shared.presentation.components.AppLabel();
            this.txtMachineID = new mtc_app.shared.presentation.components.AppInput();
            this.txtLineID = new mtc_app.shared.presentation.components.AppInput();
            this.btnSave = new mtc_app.shared.presentation.components.AppButton();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(263, 30);
            this.label1.TabIndex = 0;
            this.label1.Text = "Konfigurasi Awal Mesin";
            this.label1.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Header2;
            // 
            // txtMachineID
            // 
            this.txtMachineID.LabelText = "ID Mesin (Contoh: MC-01):";
            this.txtMachineID.Location = new System.Drawing.Point(20, 50);
            this.txtMachineID.Name = "txtMachineID";
            this.txtMachineID.Size = new System.Drawing.Size(260, 85);
            this.txtMachineID.TabIndex = 3;
            // 
            // txtLineID
            // 
            this.txtLineID.LabelText = "Line Produksi (Contoh: A):";
            this.txtLineID.Location = new System.Drawing.Point(20, 130);
            this.txtLineID.Name = "txtLineID";
            this.txtLineID.Size = new System.Drawing.Size(260, 85);
            this.txtLineID.TabIndex = 4;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(20, 220);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(260, 40);
            this.btnSave.TabIndex = 5;
            this.btnSave.Text = "SIMPAN KONFIGURASI";
            this.btnSave.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Primary;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // SetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = mtc_app.shared.presentation.styles.AppColors.Background;
            this.ClientSize = new System.Drawing.Size(304, 280);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtLineID);
            this.Controls.Add(this.txtMachineID);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SetupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Setup Awal";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private mtc_app.shared.presentation.components.AppLabel label1;
        private mtc_app.shared.presentation.components.AppInput txtMachineID;
        private mtc_app.shared.presentation.components.AppInput txtLineID;
        private mtc_app.shared.presentation.components.AppButton btnSave;
    }
}