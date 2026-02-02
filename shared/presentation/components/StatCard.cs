using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.shared.presentation.components
{
    public enum StatIconType
    {
        Checklist,
        Star,
        Trophy,
        None
    }

    public class StatCard : UserControl
    {
        private Label lblValue;
        private Label lblTitle;
        private PictureBox iconBox;
        
        private StatIconType _iconType = StatIconType.None;
        private Color _accentColor = AppColors.Primary;
        private Color _hoverColor = Color.FromArgb(239, 246, 255);

        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value;
        }

        public string Value
        {
            get => lblValue.Text;
            set => lblValue.Text = value;
        }

        [Category("Appearance")]
        public StatIconType IconType
        {
            get => _iconType;
            set { _iconType = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color HoverColor
        {
            get => _hoverColor;
            set => _hoverColor = value;
        }

        public StatCard()
        {
            InitializeComponent();
            SetupEvents();
        }

        private void InitializeComponent()
        {
            this.lblValue = new Label();
            this.lblTitle = new Label();
            this.iconBox = new PictureBox();
            
            this.SuspendLayout();

            // 
            // Main Control
            // 
            this.BackColor = Color.White;
            this.Size = new Size(290, 110);  // Increased height for larger fonts
            this.Padding = new Padding(0);

            // 
            // Icon
            // 
            this.iconBox.Size = new Size(48, 48);
            this.iconBox.Location = new Point(AppDimens.PaddingLarge, 26);
            this.iconBox.BackColor = Color.Transparent;
            this.iconBox.Paint += IconBox_Paint;

            // 
            // Value Label
            // 
            this.lblValue.Font = AppFonts.MetricMedium;
            this.lblValue.ForeColor = AppColors.TextPrimary;
            this.lblValue.Location = new Point(85, 15);  // Move up slightly
            this.lblValue.AutoSize = true;
            this.lblValue.Text = "0";

            // 
            // Title Label
            // 
            this.lblTitle.Font = AppFonts.BodySmall;
            this.lblTitle.ForeColor = AppColors.TextSecondary;
            this.lblTitle.Location = new Point(85, 65);  // Moved down for larger text
            this.lblTitle.AutoSize = true;
            this.lblTitle.Text = "Stat Title";

            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblValue);
            this.Controls.Add(this.iconBox);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupEvents()
        {
            this.MouseEnter += (s, e) => { this.BackColor = _hoverColor; this.Cursor = Cursors.Hand; };
            this.MouseLeave += (s, e) => { this.BackColor = Color.White; this.Cursor = Cursors.Default; };
            
            // Propagate events from children
            foreach (Control c in this.Controls)
            {
                c.MouseEnter += (s, e) => { this.BackColor = _hoverColor; this.Cursor = Cursors.Hand; };
                c.MouseLeave += (s, e) => { this.BackColor = Color.White; this.Cursor = Cursors.Default; };
                c.Click += (s, e) => this.OnClick(e);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            // Card background with rounded corners
            using (GraphicsPath path = GraphicsUtils.GetRoundedRectangle(bounds, 8))
            {
                // We fill with BackColor which handles Hover state
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    g.FillPath(brush, path);
                }
                g.DrawPath(new Pen(Color.FromArgb(230, 230, 230), 1), path);
            }

            // Top accent line
            using (Pen accentPen = new Pen(_accentColor, 3))
            {
                g.DrawLine(accentPen, 8, 0, this.Width - 8, 0);
            }
        }

        private void IconBox_Paint(object sender, PaintEventArgs e)
        {
            Rectangle bounds = new Rectangle(0, 0, iconBox.Width, iconBox.Height);
            
            switch (_iconType)
            {
                case StatIconType.Checklist:
                    GraphicsUtils.DrawChecklistIcon(e.Graphics, bounds, _accentColor);
                    break;
                case StatIconType.Star:
                    GraphicsUtils.DrawStarIcon(e.Graphics, bounds, _accentColor);
                    break;
                case StatIconType.Trophy:
                    GraphicsUtils.DrawTrophyIcon(e.Graphics, bounds, _accentColor);
                    break;
            }
        }
    }
}
