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
        private AppLabel lblQty;
        private AppLabel lblTicketCode;
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
            this.lblQty = new AppLabel();
            this.lblTicketCode = new AppLabel();
            this.lblRequestedAt = new AppLabel();
            this.btnReady = new AppButton();
            this.SuspendLayout();

            // 
            // Card Control
            // 
            this.BackColor = AppColors.Surface;
            this.Size = new Size(220, 280);
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
            // lblQty
            // 
            this.lblQty.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            this.lblQty.ForeColor = AppColors.TextPrimary;
            this.lblQty.Location = new Point(12, 110);
            this.lblQty.Name = "lblQty";
            this.lblQty.Size = new Size(196, 25);
            this.lblQty.Text = "Qty: 99";

            // 
            // lblTicketCode
            // 
            this.lblTicketCode.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblTicketCode.ForeColor = AppColors.TextSecondary;
            this.lblTicketCode.Location = new Point(12, 145);
            this.lblTicketCode.Name = "lblTicketCode";
            this.lblTicketCode.Size = new Size(196, 20);
            this.lblTicketCode.Text = "Tiket: MTC-00123";

            // 
            // lblRequestedAt
            // 
            this.lblRequestedAt.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.lblRequestedAt.ForeColor = AppColors.TextSecondary;
            this.lblRequestedAt.Location = new Point(12, 175);
            this.lblRequestedAt.Name = "lblRequestedAt";
            this.lblRequestedAt.Size = new Size(196, 20);
            this.lblRequestedAt.Text = "14 Jan, 10:30";

            // 
            // btnReady
            // 
            this.btnReady.Dock = DockStyle.Bottom;
            this.btnReady.Location = new Point(12, 228);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new Size(196, 40);
            this.btnReady.Text = "SET READY";
            this.btnReady.Type = AppButton.ButtonType.Primary;

            // 
            // Add Controls to parent
            // 
            this.Controls.Add(this.lblPartName);
            this.Controls.Add(this.lblQty);
            this.Controls.Add(this.lblTicketCode);
            this.Controls.Add(this.lblRequestedAt);
            this.Controls.Add(this.btnReady);
            this.ResumeLayout(false);
        }
    }
}
