using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using System.Windows.Forms;
using System.Drawing;

namespace mtc_app.features.authentication.presentation.screens
{
    partial class SetupForm
    {
        private System.ComponentModel.IContainer components = null;
        private AppLabel lblTitle;
        private AppInput txtMachineID;
        private AppInput txtLineID;
        private AppButton btnSave;
        private Panel pnlMain;

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
            this.lblTitle = new AppLabel();
            this.txtMachineID = new AppInput();
            this.txtLineID = new AppInput();
            this.btnSave = new AppButton();
            this.pnlMain = new Panel();
            this.pnlMain.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // pnlMain
            // 
            this.pnlMain.BackColor = AppColors.Surface;
            this.pnlMain.Controls.Add(this.lblTitle);
            this.pnlMain.Controls.Add(this.txtMachineID);
            this.pnlMain.Controls.Add(this.txtLineID);
            this.pnlMain.Controls.Add(this.btnSave);
            this.pnlMain.Location = new System.Drawing.Point(200, 75);
            this.pnlMain.Size = new System.Drawing.Size(400, 300);
            this.pnlMain.Name = "pnlMain";

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(50, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(300, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Konfigurasi Awal Mesin";
            this.lblTitle.Type = AppLabel.LabelType.Header2;
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;

            // 
            // txtMachineID
            // 
            this.txtMachineID.LabelText = "ID Mesin (Contoh: 1 atau 25):";
            this.txtMachineID.Location = new System.Drawing.Point(50, 70);
            this.txtMachineID.Name = "txtMachineID";
            this.txtMachineID.Size = new System.Drawing.Size(300, 55);
            this.txtMachineID.TabIndex = 1;

            // 
            // txtLineID
            // 
            this.txtLineID.LabelText = "Line Produksi (Contoh: A):";
            this.txtLineID.Location = new System.Drawing.Point(50, 140);
            this.txtLineID.Name = "txtLineID";
            this.txtLineID.Size = new System.Drawing.Size(300, 55);
            this.txtLineID.TabIndex = 2;

            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(50, 220);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(300, 45);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "SIMPAN & LANJUTKAN";
            this.btnSave.Type = AppButton.ButtonType.Primary;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            
            // 
            // SetupForm
            // 
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.pnlMain);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "SetupForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Setup Awal";
            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
