using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class SectionHeader : UserControl
    {
        private AppLabel lblTitle;
        private AppDivider divider;

        [Category("Data")]
        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value;
        }

        public SectionHeader()
        {
            this.Size = new Size(400, 45); // Height includes margin around title
            this.Margin = new Padding(0, 10, 0, 10);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblTitle = new AppLabel();
            this.divider = new AppDivider();

            this.SuspendLayout();

            // 
            // lblTitle
            // 
            this.lblTitle.Type = AppLabel.LabelType.Title;
            this.lblTitle.Dock = DockStyle.Top;
            this.lblTitle.Padding = new Padding(0, 0, 0, 5);
            this.lblTitle.Text = "Section Title";

            // 
            // divider
            // 
            this.divider.Dock = DockStyle.Top;
            this.divider.Height = 2; // Visual height
            this.divider.LineColor = AppColors.Separator;
            
            // Layout: Title on top, Divider below
            // Since Dock=Top packs from top, we add Divider first (so it's below Title? No. Last added is top? No. WinForms Dock=Top order is reverse of addition usually, or depends on z-order.)
            // Actually: controls added with Dock=Top are stacked. First added = Bottom-most of the Top stack.
            // Let's verify. 
            // If I add A (Top), then B (Top).
            // A takes top. B takes top of remaining. So B is below A.
            
            this.Controls.Add(this.divider);
            this.Controls.Add(this.lblTitle);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
