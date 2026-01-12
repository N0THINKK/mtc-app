namespace mtc_app.features.authentication.presentation.screens
{
    partial class SetupForm
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

        private void InitializeComponent()
        {
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelMachine = new System.Windows.Forms.Label();
            this.txtMachineId = new System.Windows.Forms.TextBox();
            this.labelLine = new System.Windows.Forms.Label();
            this.txtLineId = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.labelTitle.Location = new System.Drawing.Point(30, 20);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(220, 21);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Konfigurasi Awal Mesin";
            // 
            // labelMachine
            // 
            this.labelMachine.AutoSize = true;
            this.labelMachine.Location = new System.Drawing.Point(30, 60);
            this.labelMachine.Name = "labelMachine";
            this.labelMachine.Size = new System.Drawing.Size(70, 13);
            this.labelMachine.TabIndex = 1;
            this.labelMachine.Text = "ID Mesin (Angka)";
            // 
            // txtMachineId
            // 
            this.txtMachineId.Location = new System.Drawing.Point(33, 76);
            this.txtMachineId.Name = "txtMachineId";
            this.txtMachineId.Size = new System.Drawing.Size(200, 20);
            this.txtMachineId.TabIndex = 2;
            // 
            // labelLine
            // 
            this.labelLine.AutoSize = true;
            this.labelLine.Location = new System.Drawing.Point(30, 110);
            this.labelLine.Name = "labelLine";
            this.labelLine.Size = new System.Drawing.Size(65, 13);
            this.labelLine.TabIndex = 3;
            this.labelLine.Text = "Line (Contoh: A)";
            // 
            // txtLineId
            // 
            this.txtLineId.Location = new System.Drawing.Point(33, 126);
            this.txtLineId.Name = "txtLineId";
            this.txtLineId.Size = new System.Drawing.Size(200, 20);
            this.txtLineId.TabIndex = 4;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(118)))), ((int)(((byte)(210)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(33, 170);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(200, 35);
            this.btnSave.TabIndex = 5;
            this.btnSave.Text = "SIMPAN KONFIGURASI";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // SetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 241);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtLineId);
            this.Controls.Add(this.labelLine);
            this.Controls.Add(this.txtMachineId);
            this.Controls.Add(this.labelMachine);
            this.Controls.Add(this.labelTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Setup";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelMachine;
        private System.Windows.Forms.TextBox txtMachineId;
        private System.Windows.Forms.Label labelLine;
        private System.Windows.Forms.TextBox txtLineId;
        private System.Windows.Forms.Button btnSave;
    }
}