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
        public bool IsReadOnly
        {
            get { return _readOnly; }
            set 
            { 
                ReadOnly = value; 
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
            PointF[] points = CalculateStarPoints(loc, size);
            
            Color starColor = filled ? Color.Gold : Color.Gray; 
            
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

        private PointF[] CalculateStarPoints(PointF origin, float size)
        {
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
