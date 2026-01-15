// using System.Drawing;
// using System.Windows.Forms;
// using mtc_app.shared.presentation.components;
// using mtc_app.shared.presentation.styles;

// namespace mtc_app.features.stock.presentation.components
// {
//     partial class StockRequestCardControl
//     {
//         private System.ComponentModel.IContainer components = null;
//         private Panel pnlCard;
//         private AppLabel lblPartName;
//         private AppLabel lblTechnicianName;
//         private AppLabel lblRequestedAt;
//         private AppButton btnReady;
//         private Panel pnlDivider;

//         protected override void Dispose(bool disposing)
//         {
//             if (disposing && (components != null))
//             {
//                 components.Dispose();
//             }
//             base.Dispose(disposing);
//         }

//         private void InitializeComponent()
//         {
//             this.components = new System.ComponentModel.Container();
//             this.pnlCard = new Panel();
//             this.lblPartName = new AppLabel();
//             this.lblTechnicianName = new AppLabel();
//             this.lblRequestedAt = new AppLabel();
//             this.pnlDivider = new Panel();
//             this.btnReady = new AppButton();
            
//             this.pnlCard.SuspendLayout();
//             this.SuspendLayout();

//             // 
//             // Card UserControl
//             // 
//             this.BackColor = Color.Transparent;
//             this.Size = new Size(240, 240);
//             this.Margin = new Padding(10);
//             this.Padding = new Padding(0);

//             // 
//             // pnlCard
//             // 
//             this.pnlCard.BackColor = AppColors.Surface;
//             this.pnlCard.Dock = DockStyle.Fill;
//             this.pnlCard.Padding = new Padding(16);
//             this.pnlCard.BorderStyle = BorderStyle.FixedSingle;
//             this.pnlCard.Controls.Add(this.lblPartName);
//             this.pnlCard.Controls.Add(this.lblTechnicianName);
//             this.pnlCard.Controls.Add(this.lblRequestedAt);
//             this.pnlCard.Controls.Add(this.pnlDivider);
//             this.pnlCard.Controls.Add(this.btnReady);

//             // 
//             // lblPartName
//             // 
//             this.lblPartName.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
//             this.lblPartName.ForeColor = AppColors.TextPrimary;
//             this.lblPartName.Location = new Point(16, 16);
//             this.lblPartName.Name = "lblPartName";
//             this.lblPartName.Size = new Size(206, 80);
//             this.lblPartName.Text = "Part Name";
//             this.lblPartName.AutoEllipsis = false;
//             this.lblPartName.MaximumSize = new Size(206, 80);

//             // 
//             // lblTechnicianName
//             // 
//             this.lblTechnicianName.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
//             this.lblTechnicianName.ForeColor = AppColors.TextSecondary;
//             this.lblTechnicianName.Location = new Point(16, 105);
//             this.lblTechnicianName.Name = "lblTechnicianName";
//             this.lblTechnicianName.Size = new Size(206, 22);
//             this.lblTechnicianName.Text = "👤 By: Technician";

//             // 
//             // lblRequestedAt
//             // 
//             this.lblRequestedAt.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
//             this.lblRequestedAt.ForeColor = AppColors.TextSecondary;
//             this.lblRequestedAt.Location = new Point(16, 132);
//             this.lblRequestedAt.Name = "lblRequestedAt";
//             this.lblRequestedAt.Size = new Size(206, 20);
//             this.lblRequestedAt.Text = "🕐 14 Jan, 10:30";

//             // 
//             // pnlDivider
//             // 
//             this.pnlDivider.BackColor = Color.FromArgb(220, 220, 220);
//             this.pnlDivider.Location = new Point(16, 162);
//             this.pnlDivider.Name = "pnlDivider";
//             this.pnlDivider.Size = new Size(206, 1);
//             this.pnlDivider.TabIndex = 3;

//             // 
//             // btnReady
//             // 
//             this.btnReady.Location = new Point(16, 178);
//             this.btnReady.Name = "btnReady";
//             this.btnReady.Size = new Size(206, 44);
//             this.btnReady.TabIndex = 4;
//             this.btnReady.Text = "✓ SET READY";
//             this.btnReady.Type = AppButton.ButtonType.Primary;
//             this.btnReady.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

//             // 
//             // Add Controls
//             // 
//             this.Controls.Add(this.pnlCard);
            
//             this.pnlCard.ResumeLayout(false);
//             this.ResumeLayout(false);
//         }
//     }
// }