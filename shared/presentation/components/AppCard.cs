using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.shared.presentation.components
{
    public class AppCard : Panel
    {
        private int _cornerRadius = AppDimens.CardCornerRadius;
        private bool _showShadow = false;
        private Color _borderColor = AppColors.Border;

        [Category("Appearance")]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        [Category("Appearance")]
        public bool ShowShadow
        {
            get => _showShadow;
            set { _showShadow = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        public AppCard()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = AppColors.CardBackground;
            this.Padding = new Padding(AppDimens.PaddingStandard);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Do NOT call base.OnPaint if you want full control, but for Panel it's fine.
            // We want to draw the background ourselves to handle transparency/corners
            
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Define bounds
            Rectangle bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            if (_showShadow)
            {
                // Shrink bounds slightly to make room for shadow
                bounds.Inflate(-2, -2);
            }

            // Draw Shadow (Simple implementation)
            if (_showShadow)
            {
                // In WinForms GDI+, real shadows are hard. We simulate with a gray offset.
                using (GraphicsPath shadowPath = GraphicsUtils.GetRoundedRectangle(new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width, bounds.Height), _cornerRadius))
                {
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
                    {
                        g.FillPath(shadowBrush, shadowPath);
                    }
                }
            }

            // Draw Background
            using (GraphicsPath path = GraphicsUtils.GetRoundedRectangle(bounds, _cornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    g.FillPath(brush, path);
                }

                // Draw Border
                using (Pen pen = new Pen(_borderColor))
                {
                    g.DrawPath(pen, path);
                }
            }
        }
    }
}
