using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class AppCard : Panel
    {
        public AppCard()
        {
            this.BackColor = AppColors.Surface;
            this.Padding = new Padding(AppDimens.SpacingMedium);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw shadow or border
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            int radius = AppDimens.CornerRadiusLarge;
            Rectangle rectangle = this.ClientRectangle;
            rectangle.Width -= 1;
            rectangle.Height -= 1;

            using (var path = GetRoundedPath(rectangle, radius))
            using (var pen = new Pen(AppColors.Border, 1))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;

            path.AddArc(rectangle.X, rectangle.Y, d, d, 180, 90);
            path.AddArc(rectangle.X + rectangle.Width - d, rectangle.Y, d, d, 270, 90);
            path.AddArc(rectangle.X + rectangle.Width - d, rectangle.Y + rectangle.Height - d, d, d, 0, 90);
            path.AddArc(rectangle.X, rectangle.Y + rectangle.Height - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
