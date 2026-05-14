using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.features.operator_worksheet.data.dtos;

namespace mtc_app.features.operator_worksheet.presentation.components
{
    public class WireVisualizerPanel : Panel
    {
        private PrdmstDto _masterData;
        private Font _labelFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        private Font _kombinasiFont = new Font("Segoe UI", 10F, FontStyle.Bold);

        public WireVisualizerPanel()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(241, 245, 249); // slate-100 fallback
            this.Margin = new Padding(0);
        }

        public void UpdateData(PrdmstDto masterData)
        {
            _masterData = masterData;
            this.Invalidate(); // trigger repaint
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_masterData == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Colors
            Color wireColor = Color.Black;
            Color terminalColor = Color.LightGray;
            Color terminalOutline = Color.DimGray;
            Color stripColor = Color.Orange;
            Color textDark = Color.FromArgb(15, 23, 42);

            int centerY = this.Height / 2 + 10;
            int marginX = 20;
            
            // Wire properties
            int wireThickness = 8;
            int terminalWidth = 30;
            int terminalHeight = 16;
            int stripLength = 20;

            int leftX = marginX + terminalWidth; // start of wire (from left)
            int rightX = this.Width - marginX - terminalWidth; // end of wire (from left)

            string termAMode = _masterData.HasTerminalA; // "2" = Terminal, "Y" = Strip Only
            string termBMode = _masterData.HasTerminalB;

            // DRAW CENTRAL WIRE
            using (var p = new Pen(wireColor, wireThickness))
            {
                g.DrawLine(p, leftX, centerY, rightX, centerY);
            }

            // DRAW KOMBINASI WIRE TEXT
            string kombinasi = _masterData.KombinasiWire;
            if (!string.IsNullOrEmpty(kombinasi))
            {
                SizeF size = g.MeasureString(kombinasi, _kombinasiFont);
                g.DrawString(kombinasi, _kombinasiFont, new SolidBrush(textDark), (this.Width - size.Width) / 2, centerY - 25);
            }

            // DRAW CUT LENGTH TEXT (DI BAWAH KABEL)
            string cutL = _masterData.CutLength;
            if (!string.IsNullOrEmpty(cutL))
            {
                string textCutL = $"CutL: {cutL}";
                SizeF sizeCutL = g.MeasureString(textCutL, _labelFont);
                g.DrawString(textCutL, _labelFont, new SolidBrush(Color.DimGray), (this.Width - sizeCutL.Width) / 2, centerY + 15);
            }

            // --- LEFT SIDE (TERMINAL A) ---
            DrawEnd(g, true, leftX, centerY, termAMode, _masterData.TerminalA, _masterData.SealA, terminalWidth, terminalHeight, stripLength, wireThickness, terminalColor, terminalOutline, stripColor, textDark);

            // --- RIGHT SIDE (TERMINAL B) ---
            DrawEnd(g, false, rightX, centerY, termBMode, _masterData.TerminalB, _masterData.SealB, terminalWidth, terminalHeight, stripLength, wireThickness, terminalColor, terminalOutline, stripColor, textDark);
        }

        private void DrawEnd(Graphics g, bool isLeft, int wireX, int centerY, string mode, string terminalName, string sealName, int terminalWidth, int terminalHeight, int stripLength, int wireThickness, Color termColor, Color outlineColor, Color stripColor, Color textColor)
        {
            int sign = isLeft ? -1 : 1;
            
            bool hasTerminal = mode == "2" || (!string.IsNullOrWhiteSpace(terminalName) && terminalName != "-" && terminalName != mode);

            if (hasTerminal) // Has Terminal
            {
                // Draw Terminal Polygon
                int termStartX = wireX;
                int termEndX = wireX + (terminalWidth * sign);
                
                int topY = centerY - (terminalHeight / 2);
                
                // Simple terminal shape (rectangle with a bump)
                Rectangle termRect = new Rectangle(Math.Min(termStartX, termEndX), topY, terminalWidth, terminalHeight);
                using (var b = new SolidBrush(termColor))
                using (var p = new Pen(outlineColor, 1))
                {
                    g.FillRectangle(b, termRect);
                    g.DrawRectangle(p, termRect);
                    
                    // Add small bump representing crimp section
                    int bumpWidth = 10;
                    Rectangle bumpRect = new Rectangle(
                        isLeft ? termEndX : termStartX - bumpWidth, 
                        topY - 4, 
                        bumpWidth, 
                        terminalHeight + 8
                    );
                    g.FillRectangle(new SolidBrush(Color.DarkGray), bumpRect);
                    g.DrawRectangle(p, bumpRect);
                }

                // Draw Terminal Name
                string tName = string.IsNullOrEmpty(terminalName) ? "-" : terminalName;
                DrawCenteredText(g, tName, _labelFont, textColor, isLeft ? termEndX + (terminalWidth/2) : termEndX - (terminalWidth/2), centerY - 30);
            }
            else // "Y" or default -> Strip Only
            {
                // Draw strip line
                int stripEndX = wireX + (stripLength * sign);
                using (var p = new Pen(stripColor, wireThickness - 2))
                {
                    g.DrawLine(p, wireX, centerY, stripEndX, centerY);
                }

                DrawCenteredText(g, "STRIP ONLY", _labelFont, textColor, isLeft ? stripEndX + (stripLength/2) : stripEndX - (stripLength/2), centerY - 30);
            }

            // Draw Seal (below wire)
            if (!string.IsNullOrEmpty(sealName))
            {
                DrawCenteredText(g, "Seal: " + sealName, _labelFont, Color.DimGray, isLeft ? wireX - 10 : wireX + 10, centerY + 15);
            }
        }

        private void DrawCenteredText(Graphics g, string text, Font font, Color color, int centerX, int y)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, new SolidBrush(color), centerX - (size.Width / 2), y);
        }
    }
}
