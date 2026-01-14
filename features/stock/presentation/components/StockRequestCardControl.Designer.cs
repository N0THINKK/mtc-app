using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.stock.presentation.components
{
    partial class StockRequestCardControl
    {
        private System.ComponentModel.IContainer components = null;
        private AppLabel lblPartName;
        private AppLabel lblTechnicianName;
        private AppLabel lblRequestedAt;
        private AppButton btnReady;

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
            this.components = new System.ComponentModel.Container();
            this.lblPartName = new AppLabel();
            this.lblTechnicianName = new AppLabel();
            this.lblRequestedAt = new AppLabel();
            this.btnReady = new AppButton();
            this.SuspendLayout();

            // 
            // Card Control
            // 
            this.BackColor = AppColors.Surface;
            this.Size = new Size(220, 220); // Made shorter
            this.Padding = new Padding(12);
            this.Margin = new Padding(10);
            this.BorderStyle = BorderStyle.FixedSingle;
            
            // 
            // lblPartName
            // 
            this.lblPartName.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblPartName.ForeColor = AppColors.TextPrimary;
            this.lblPartName.Location = new Point(12, 12);
            this.lblPartName.Name = "lblPartName";
            this.lblPartName.Size = new Size(196, 90); // Allow multiple lines
            this.lblPartName.Text = "Nama Barang Panjang Sekali Contohnya";
            
            // 
            // lblTechnicianName
            // 
            this.lblTechnicianName.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblTechnicianName.ForeColor = AppColors.TextSecondary;
            this.lblTechnicianName.Location = new Point(12, 110);
            this.lblTechnicianName.Name = "lblTechnicianName";
            this.lblTechnicianName.Size = new Size(196, 20);
            this.lblTechnicianName.Text = "By: Vemas Satria";

            // 
            // lblRequestedAt
            // 
            this.lblRequestedAt.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.lblRequestedAt.ForeColor = AppColors.TextSecondary;
            this.lblRequestedAt.Location = new Point(12, 140);
            this.lblRequestedAt.Name = "lblRequestedAt";
            this.lblRequestedAt.Size = new Size(196, 20);
            this.lblRequestedAt.Text = "14 Jan, 10:30";

            // 
            // btnReady
            // 
            this.btnReady.Dock = DockStyle.Bottom;
            this.btnReady.Location = new Point(12, 168);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new Size(196, 40);
            this.btnReady.Text = "SET READY";
            this.btnReady.Type = AppButton.ButtonType.Primary;

            // 
            // Add Controls to parent
            // 
            this.Controls.Add(this.lblPartName);
            this.Controls.Add(this.lblTechnicianName);
            this.Controls.Add(this.lblRequestedAt);
            this.Controls.Add(this.btnReady);
            this.ResumeLayout(false);
        }
    }
}