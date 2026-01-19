using System.Drawing;
using System.Drawing.Drawing2D;

namespace mtc_app.shared.presentation.utils
{
    public static class GraphicsUtils
    {
        public static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            
            // Adjust bounds to prevent clipping issues
            Rectangle arcBounds = new Rectangle(bounds.X, bounds.Y, diameter, diameter);

            // Top Left
            path.AddArc(arcBounds, 180, 90);
            
            // Top Right
            arcBounds.X = bounds.Right - diameter;
            path.AddArc(arcBounds, 270, 90);
            
            // Bottom Right
            arcBounds.Y = bounds.Bottom - diameter;
            path.AddArc(arcBounds, 0, 90);
            
            // Bottom Left
            arcBounds.X = bounds.X;
            path.AddArc(arcBounds, 90, 90);
            
            path.CloseFigure();
            return path;
        }

        public static void DrawSmoothPath(Graphics g, GraphicsPath path, Brush fillBrush, Pen borderPen = null)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (fillBrush != null)
                g.FillPath(fillBrush, path);
            if (borderPen != null)
                g.DrawPath(borderPen, path);
        }
    }
}
