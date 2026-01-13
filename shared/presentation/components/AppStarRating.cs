using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class AppStarRating : UserControl
    {
        private int _rating = 0;
        private int _hoverRating = 0;
        private bool _readOnly = false;
        
        // Star config
        private const int StarCount = 5;
        private const int StarSize = 24;
        private const int Spacing = 4;

        public event EventHandler RatingChanged;

        public AppStarRating()
        {
            this.DoubleBuffered = true;
            this.Size = new Size((StarSize * StarCount) + (Spacing * (StarCount - 1)) + 10, StarSize + 10);
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;
        }

        [Category("App Properties")]
        public int Rating
        {
            get { return _rating; }
            set
            {
                if (value < 0) _rating = 0;
                else if (value > StarCount) _rating = StarCount;
                else _rating = value;
                
                this.Invalidate();
                RatingChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Category("App Properties")]
        public bool ReadOnly
        {
            get { return _readOnly; }
            set
            {
                _readOnly = value;
                this.Cursor = value ? Cursors.Default : Cursors.Hand;
                this.Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_readOnly) return;

            int newHover = GetStarFromLocation(e.Location);
            if (newHover != _hoverRating)
            {
                _hoverRating = newHover;
                this.Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_readOnly) return;
            
            _hoverRating = 0;
            this.Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_readOnly) return;

            int clickedRating = GetStarFromLocation(e.Location);
            if (clickedRating > 0)
            {
                Rating = clickedRating;
            }
        }

        private int GetStarFromLocation(Point p)
        {
            for (int i = 0; i < StarCount; i++)
            {
                int startX = i * (StarSize + Spacing);
                // Broad hit detection
                if (p.X >= startX && p.X <= startX + StarSize + Spacing)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int activeRating = (_hoverRating > 0 && !_readOnly) ? _hoverRating : _rating;

            for (int i = 0; i < StarCount; i++)
            {
                PointF location = new PointF(i * (StarSize + Spacing), 0);
                bool isFilled = (i < activeRating);
                
                DrawStar(e.Graphics, location, StarSize, isFilled);
            }
        }

        private void DrawStar(Graphics g, PointF loc, float size, bool filled)
        {
            // Simple 5-point star path
            PointF[] points = CalculateStarPoints(loc, size, size / 2.5f);
            
            Color starColor = filled ? Color.Gold : Color.Gray; 
            // Or use AppColors if we want strict theme:
            // Color.Gold is standard for stars.
            
            if (filled)
            {
                using (Brush brush = new SolidBrush(starColor))
                {
                    g.FillPolygon(brush, points);
                }
            }
            
            // Outline
            using (Pen pen = new Pen(starColor, 1.5f))
            {
                g.DrawPolygon(pen, points);
            }
        }

        private PointF[] CalculateStarPoints(PointF origin, float outerRadius, float innerRadius)
        {
            PointF[] points = new PointF[10];
            double angle = Math.PI / 2; // Start at top
            double step = Math.PI / 5;  // 36 degrees

            // Adjust origin to center of square
            float centerX = origin.X + outerRadius / 2;
            float centerY = origin.Y + outerRadius / 2;

            // Reduce radius slightly to fit in box with padding
            float rOut = (outerRadius / 2) - 2;
            float rIn = (innerRadius / 2) - 2;

            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? rOut : rIn;
                // Subtract angle because Y grows downwards
                points[i] = new PointF(
                    (float)(centerX + Math.Cos(angle) * r), // Cos for X (rotated -90deg effectively if started at 0)
                    // Standard math: 0 is right. We want top (-90deg or 270deg).
                    // Actually let's use standard formulation:
                    // Angle 0 = right. Top = -PI/2.
                    // Loop: 
                    // i=0 (Top): -PI/2
                    // i=1 (Inner): -PI/2 + PI/5 ...
                    //
                    // My var `angle` started at PI/2? Wait.
                    // Math.Cos(-PI/2) = 0. Math.Sin(-PI/2) = -1. Correct for top.
                    (float)(centerY + Math.Sin(angle) * r)  // Sin for Y
                );
                angle -= step; // Clockwise
            }
            return points;
        }
        
        // Re-implement star points to be sure about orientation
        private PointF[] CalculateStarPoints_Corrected(PointF origin, float size)
        {
             // ... actually the first one was roughly okay but let's be precise.
             // Center
             float cx = origin.X + size / 2;
             float cy = origin.Y + size / 2;
             
             float rOuter = size / 2;
             float rInner = rOuter * 0.4f;

             PointF[] pts = new PointF[10];
             double theta = -Math.PI / 2; // Up
             double dTheta = Math.PI / 5;

             for(int i=0; i<10; i++)
             {
                 float r = (i % 2 == 0) ? rOuter : rInner;
                 pts[i] = new PointF(
                     cx + (float)(r * Math.Cos(theta)),
                     cy + (float)(r * Math.Sin(theta))
                 );
                 theta += dTheta;
             }
             return pts;
        }
    }
}
