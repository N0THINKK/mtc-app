using System;
using System.Drawing;
using System.Windows.Forms;

namespace mtc_app.features.operator_worksheet.presentation.components
{
    public static class ZoomableImagePopup
    {
        public static void Show(Image sourceImage, string title, IWin32Window owner)
        {
            var popup = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(Math.Min(Screen.PrimaryScreen.WorkingArea.Width - 100, 1200),
                               Math.Min(Screen.PrimaryScreen.WorkingArea.Height - 100, 800)),
                BackColor = Color.FromArgb(30, 30, 30),
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                KeyPreview = true
            };

            // Buat copy dari image agar tidak terganggu dispose
            var imgCopy = new Bitmap(sourceImage);
            float zoomLevel = 1.0f;
            PointF offset = PointF.Empty;
            bool isDragging = false;
            Point dragStart = Point.Empty;
            PointF offsetStart = PointF.Empty;

            // Panel gambar utama (custom paint)
            var canvas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Cursor = Cursors.Hand
            };
            canvas.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(canvas, true);

            // Label zoom di pojok
            var lblZoom = new Label
            {
                Text = "100%",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(120, 0, 0, 0),
                AutoSize = true,
                Location = new Point(12, 12),
                Padding = new Padding(6, 3, 6, 3)
            };
            canvas.Controls.Add(lblZoom);

            // Tombol close - besar dan mencolok agar mudah ditemukan
            var btnClose = new Button
            {
                Text = "X TUTUP",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 44),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Location = new Point(popup.Width - btnClose.Width - 16, 12);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Click += (s, ev) => popup.Close();
            canvas.Controls.Add(btnClose);

            // Klik kanan di mana saja juga bisa close
            canvas.MouseClick += (s, ev) => { if (ev.Button == MouseButtons.Right) popup.Close(); };

            // Label instruksi
            var lblHelp = new Label
            {
                Text = "Scroll = Zoom  |  Drag = Geser  |  Klik kanan / Esc = Tutup",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Location = new Point(12, popup.Height - 35),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            canvas.Controls.Add(lblHelp);

            // Paint
            canvas.Paint += (s, ev) =>
            {
                ev.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                ev.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float drawW = imgCopy.Width * zoomLevel;
                float drawH = imgCopy.Height * zoomLevel;

                // Center image + offset
                float cx = (canvas.Width - drawW) / 2 + offset.X;
                float cy = (canvas.Height - drawH) / 2 + offset.Y;

                ev.Graphics.DrawImage(imgCopy, cx, cy, drawW, drawH);
            };

            // Scroll = Zoom
            canvas.MouseWheel += (s, ev) =>
            {
                float oldZoom = zoomLevel;
                if (ev.Delta > 0)
                    zoomLevel = Math.Min(zoomLevel * 1.2f, 10f);
                else
                    zoomLevel = Math.Max(zoomLevel / 1.2f, 0.1f);

                lblZoom.Text = $"{(int)(zoomLevel * 100)}%";
                canvas.Invalidate();
            };

            // Drag = Pan
            canvas.MouseDown += (s, ev) =>
            {
                if (ev.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    dragStart = ev.Location;
                    offsetStart = offset;
                    canvas.Cursor = Cursors.SizeAll;
                }
            };
            canvas.MouseMove += (s, ev) =>
            {
                if (isDragging)
                {
                    offset = new PointF(
                        offsetStart.X + ev.X - dragStart.X,
                        offsetStart.Y + ev.Y - dragStart.Y);
                    canvas.Invalidate();
                }
            };
            canvas.MouseUp += (s, ev) =>
            {
                isDragging = false;
                canvas.Cursor = Cursors.Hand;
            };

            // Double click = reset zoom
            canvas.MouseDoubleClick += (s, ev) =>
            {
                zoomLevel = 1.0f;
                offset = PointF.Empty;
                lblZoom.Text = "100%";
                canvas.Invalidate();
            };

            popup.Controls.Add(canvas);
            popup.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Escape) popup.Close(); };
            popup.FormClosed += (s, ev) => imgCopy.Dispose();

            // Focus canvas agar scroll langsung bisa
            popup.Shown += (s, ev) => canvas.Focus();

            popup.ShowDialog(owner);
        }
    }
}
