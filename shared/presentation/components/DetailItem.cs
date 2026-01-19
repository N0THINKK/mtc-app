using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class DetailItem : UserControl
    {
        private Label lblTitle;
        private Label lblValue;

        [Category("Data")]
        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value;
        }

        [Category("Data")]
        public string Value
        {
            get => lblValue.Text;
            set => lblValue.Text = value;
        }

        public DetailItem()
        {
            this.Size = new Size(300, 45);
            this.Margin = new Padding(0, 0, 0, 10);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblTitle = new Label();
            this.lblValue = new Label();

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Dock = DockStyle.Top;
            this.lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold); // Should use AppFonts in future
            this.lblTitle.ForeColor = AppColors.TextSecondary;
            this.lblTitle.Padding = new Padding(0, 0, 0, 2);
            this.lblTitle.Text = "Title";

            // 
            // lblValue
            // 
            this.lblValue.AutoSize = true;
            this.lblValue.Dock = DockStyle.Top;
            this.lblValue.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblValue.ForeColor = AppColors.TextPrimary;
            this.lblValue.Text = "Value";
            
            this.Controls.Add(this.lblValue);
            this.Controls.Add(this.lblTitle);
        }
    }
}
