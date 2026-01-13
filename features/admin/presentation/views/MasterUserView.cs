using System.Windows.Forms;
using System.Drawing;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.admin.presentation.views
{
    public partial class MasterUserView : UserControl
    {
        private System.ComponentModel.IContainer components = null;

        public MasterUserView()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
        }

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
            
            var lblPlaceholder = new Label();
            lblPlaceholder.Text = "Halaman Manajemen User";
            lblPlaceholder.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblPlaceholder.ForeColor = AppColors.TextPrimary;
            lblPlaceholder.AutoSize = true;
            lblPlaceholder.Location = new Point(0, 0);

            this.Controls.Add(lblPlaceholder);
            this.Name = "MasterUserView";
            this.Size = new System.Drawing.Size(860, 600);
            this.ResumeLayout(false);
        }
    }
}
