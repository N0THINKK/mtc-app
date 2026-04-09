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

        public static void DrawChecklistIcon(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 3))
            {
                int x = bounds.X;
                int y = bounds.Y;
                // Clipboard outline
                g.DrawRectangle(pen, x + 8, y + 12, 32, 28);
                // Clip
                g.DrawRectangle(pen, x + 18, y + 8, 12, 6);
                // Checkmark
                g.DrawLine(pen, x + 16, y + 24, x + 20, y + 28);
                g.DrawLine(pen, x + 20, y + 28, x + 32, y + 16);
            }
        }

        public static void DrawStarIcon(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int x = bounds.X;
            int y = bounds.Y;
            
            PointF[] starPoints = new PointF[]
            {
                new PointF(x + 24, y + 8),      // Top
                new PointF(x + 28, y + 18),     // Top right inner
                new PointF(x + 38, y + 18),     // Right
                new PointF(x + 30, y + 26),     // Bottom right inner
                new PointF(x + 32, y + 36),     // Bottom right
                new PointF(x + 24, y + 30),     // Bottom inner
                new PointF(x + 16, y + 36),     // Bottom left
                new PointF(x + 18, y + 26),     // Bottom left inner
                new PointF(x + 10, y + 18),     // Left
                new PointF(x + 20, y + 18)      // Top left inner
            };

            using (Brush brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, starPoints);
            }
        }

        public static void DrawTrophyIcon(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 3))
            {
                int x = bounds.X;
                int y = bounds.Y;
                // Cup
                g.DrawArc(pen, x + 14, y + 10, 20, 20, 0, 180);
                g.DrawLine(pen, x + 14, y + 20, x + 16, y + 28);
                g.DrawLine(pen, x + 34, y + 20, x + 32, y + 28);
                // Base
                g.DrawLine(pen, x + 16, y + 28, x + 32, y + 28);
                g.DrawLine(pen, x + 20, y + 28, x + 20, y + 32);
                g.DrawLine(pen, x + 28, y + 28, x + 28, y + 32);
                g.DrawLine(pen, x + 16, y + 32, x + 32, y + 32);
                // Handles
                g.DrawArc(pen, x + 8, y + 12, 8, 12, 90, 180);
                g.DrawArc(pen, x + 32, y + 12, 8, 12, 270, 180);
            }
        }

        public static void DrawMachineIcon(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 2))
            {
                int x = bounds.X; 
                int y = bounds.Y;
                
                // Cog body
                g.DrawEllipse(pen, x + 2, y + 2, 12, 12);
                // Center hole
                g.DrawEllipse(pen, x + 6, y + 6, 4, 4);
                // Teeth (simplified)
                g.DrawLine(pen, x + 8, y + 0, x + 8, y + 2); // Top
                g.DrawLine(pen, x + 8, y + 14, x + 8, y + 16); // Bottom
                g.DrawLine(pen, x + 0, y + 8, x + 2, y + 8); // Left
                g.DrawLine(pen, x + 14, y + 8, x + 16, y + 8); // Right
            }
        }

        public static void DrawClockIcon(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 2))
            {
                int x = bounds.X;
                int y = bounds.Y;
                
                // Circle
                g.DrawEllipse(pen, x + 1, y + 1, 14, 14);
                // Hands
                g.DrawLine(pen, x + 8, y + 8, x + 8, y + 4); // Hour
                g.DrawLine(pen, x + 8, y + 8, x + 11, y + 8); // Minute
            }
        }

        public static void DrawUserIcon(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 2))
            {
                int x = bounds.X;
                int y = bounds.Y;
                // Head
                g.DrawEllipse(pen, x + 4, y + 0, 8, 8);
                // Body
                g.DrawArc(pen, x + 0, y + 10, 16, 12, 0, 180); // Arc body
            }
        }

        public static void DrawEmptyFolderIcon(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 2))
            {
                int x = bounds.X;
                int y = bounds.Y;

                // Folder Shape
                // M 0 10 L 0 30 L 40 30 L 40 10 L 20 10 L 15 0 L 0 0 Z (Roughly)
                Point[] points = {
                    new Point(x, y + 10),
                    new Point(x, y + 30),
                    new Point(x + 40, y + 30),
                    new Point(x + 40, y + 10),
                    new Point(x + 20, y + 10),
                    new Point(x + 15, y + 0),
                    new Point(x, y + 0)
                };
                g.DrawPolygon(pen, points);
            }
        }

        public static void DrawBoxIcon(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 2))
            {
                int x = bounds.X + 5;
                int y = bounds.Y + 5;
                int w = 30;
                int h = 25;

                // Box 3D-ish view
                // Front face
                g.DrawRectangle(pen, x, y + 5, w, h - 5);
                // Top flaps
                g.DrawLine(pen, x, y + 5, x + 5, y);
                g.DrawLine(pen, x + w, y + 5, x + w - 5, y);
                g.DrawLine(pen, x + 5, y, x + w - 5, y);
                // Center line
                g.DrawLine(pen, x + w / 2, y + 5, x + w / 2, y + h);
            }
        }
    }
}
