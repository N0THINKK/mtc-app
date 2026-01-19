using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class AppDivider : Control
    {
        private Color _lineColor = AppColors.Separator;
        private int _thickness = 1;

        [Category("Appearance")]
        public Color LineColor
        {
            get => _lineColor;
            set { _lineColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        public int Thickness
        {
            get => _thickness;
            set { _thickness = value; Invalidate(); }
        }

        public AppDivider()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            this.Height = 10; // Default height (includes padding)
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None; // Lines should be crisp

            using (Pen pen = new Pen(_lineColor, _thickness))
            {
                int y = this.Height / 2;
                g.DrawLine(pen, 0, y, this.Width, y);
            }
        }
    }
}
