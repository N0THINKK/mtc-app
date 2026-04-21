namespace mtc_app.features.operator_worksheet.presentation.screens
{
    partial class OperatorWorksheetForm
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
            this.SuspendLayout();

            // OperatorWorksheetForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1366, 768);
            this.Name = "OperatorWorksheetForm";
            this.Text = "Lembar Kerja Operator";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.OperatorWorksheetForm_Load);

            this.ResumeLayout(false);
        }
    }
}
