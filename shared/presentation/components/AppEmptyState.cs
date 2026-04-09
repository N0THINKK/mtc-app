using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.shared.presentation.components
{
    public class AppEmptyState : Panel
    {
        private Label lblTitle;
        private Label lblDescription;
        private Panel pnlIcon; // Panel to handle custom paint of icon
        
        public enum IconType
        {
            Folder,
            Box,
            Custom
        }

        private IconType _iconType = IconType.Folder;

        [Category("Appearance")]
        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value;
        }

        [Category("Appearance")]
        public string Description
        {
            get => lblDescription.Text;
            set => lblDescription.Text = value;
        }

        [Category("Appearance")]
        public IconType Icon
        {
            get => _iconType;
            set { _iconType = value; pnlIcon.Invalidate(); }
        }

        public AppEmptyState()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.BackColor = Color.White; // or AppColors.Surface
            this.Dock = DockStyle.Fill;

            // Icon Panel
            pnlIcon = new Panel
            {
                Size = new Size(60, 60), // Fixed size for icon area
                BackColor = Color.Transparent
            };
            pnlIcon.Paint += PnlIcon_Paint;

            // Title
            lblTitle = new Label
            {
                AutoSize = true,
                Font = AppFonts.Header2, // Bold 14
                ForeColor = AppColors.TextSecondary,
                Text = "No Data Found",
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Description
            lblDescription = new Label
            {
                AutoSize = true,
                Font = AppFonts.Body,
                ForeColor = AppColors.TextDisabled, // Lighter text
                Text = "There are no items to display at this time.",
                TextAlign = ContentAlignment.MiddleCenter,
                MaximumSize = new Size(400, 0)
            };

            this.Controls.Add(lblDescription);
            this.Controls.Add(lblTitle);
            this.Controls.Add(pnlIcon);

            this.Resize += (s, e) => CenterControls();

            this.ResumeLayout(false);
            CenterControls();
        }

        private void PnlIcon_Paint(object sender, PaintEventArgs e)
        {
            Color iconColor = Color.FromArgb(209, 213, 219); // Light Gray
            Rectangle bounds = new Rectangle(0, 0, pnlIcon.Width, pnlIcon.Height);
            
            // Center the drawing in the panel
            // GraphicsUtils draws at X,Y. Let's adjust bounds or pass graphics transform?
            // GraphicsUtils draw methods take bounds.
            // Let's assume a 40x40 desired icon size centered in 60x60
            Rectangle iconBounds = new Rectangle(10, 15, 40, 40); 

            if (_iconType == IconType.Folder)
            {
                GraphicsUtils.DrawEmptyFolderIcon(e.Graphics, iconBounds, iconColor);
            }
            else if (_iconType == IconType.Box)
            {
                GraphicsUtils.DrawBoxIcon(e.Graphics, iconBounds, iconColor);
            }
        }

        private void CenterControls()
        {
            if (this.Width == 0 || this.Height == 0) return;

            int totalHeight = pnlIcon.Height + 20 + lblTitle.Height + 10 + lblDescription.Height;
            int startY = (this.Height - totalHeight) / 2;
            int cx = this.Width / 2;

            pnlIcon.Location = new Point(cx - (pnlIcon.Width / 2), startY);
            lblTitle.Location = new Point(cx - (lblTitle.Width / 2), pnlIcon.Bottom + 20);
            lblDescription.Location = new Point(cx - (lblDescription.Width / 2), lblTitle.Bottom + 10);
        }
    }
}
