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
        private AppInput comboMachineType;
        private AppInput comboMachineArea;
        private AppInput txtMachineNumber;
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
            this.comboMachineType = new AppInput();
            this.comboMachineArea = new AppInput();
            this.txtMachineNumber = new AppInput();
            this.btnSave = new AppButton();
            this.pnlMain = new Panel();
            this.pnlMain.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // pnlMain
            // 
            this.pnlMain.BackColor = AppColors.Surface;
            this.pnlMain.Controls.Add(this.lblTitle);
            this.pnlMain.Controls.Add(this.comboMachineType);
            this.pnlMain.Controls.Add(this.comboMachineArea);
            this.pnlMain.Controls.Add(this.txtMachineNumber);
            this.pnlMain.Controls.Add(this.btnSave);
            this.pnlMain.Location = new System.Drawing.Point(100, 50); // Adjusted position
            this.pnlMain.Size = new System.Drawing.Size(600, 450); // Increased size
            this.pnlMain.Name = "pnlMain";

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(150, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(300, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Konfigurasi Awal Mesin";
            this.lblTitle.Type = AppLabel.LabelType.Header2;
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;

            // 
            // comboMachineType
            // 
            this.comboMachineType.LabelText = "Tipe Mesin (Kode):";
            this.comboMachineType.Location = new System.Drawing.Point(50, 70);
            this.comboMachineType.Name = "comboMachineType";
            this.comboMachineType.Size = new System.Drawing.Size(500, 55);
            this.comboMachineType.TabIndex = 1;
            this.comboMachineType.InputType = AppInput.InputTypeEnum.Dropdown;

            // 
            // comboMachineArea
            // 
            this.comboMachineArea.LabelText = "Area Mesin:";
            this.comboMachineArea.Location = new System.Drawing.Point(50, 140);
            this.comboMachineArea.Name = "comboMachineArea";
            this.comboMachineArea.Size = new System.Drawing.Size(500, 55);
            this.comboMachineArea.TabIndex = 2;
            this.comboMachineArea.InputType = AppInput.InputTypeEnum.Dropdown;

            // 
            // txtMachineNumber
            // 
            this.txtMachineNumber.LabelText = "Nomor Urut (Contoh: 01):";
            this.txtMachineNumber.Location = new System.Drawing.Point(50, 210);
            this.txtMachineNumber.Name = "txtMachineNumber";
            this.txtMachineNumber.Size = new System.Drawing.Size(500, 55);
            this.txtMachineNumber.TabIndex = 3;

            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(50, 290); // Moved up
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(500, 45);
            this.btnSave.TabIndex = 4;
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
