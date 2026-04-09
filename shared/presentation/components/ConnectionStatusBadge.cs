using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.shared.presentation.components
{
    public class ConnectionStatusBadge : UserControl
    {
        private bool _isActive = true;
        private Label lblStatus;
        private PictureBox picIndicator;

        [Category("Data")]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                UpdateVisuals();
            }
        }

        public ConnectionStatusBadge()
        {
            this.Size = new Size(150, 40);
            this.Padding = new Padding(5);
            
            InitializeComponent();
            UpdateVisuals();
        }

        private void InitializeComponent()
        {
            picIndicator = new PictureBox();
            lblStatus = new Label();
            
            // Indicator
            picIndicator.Size = new Size(12, 12);
            picIndicator.Location = new Point(10, 14);
            picIndicator.Paint += PicIndicator_Paint;
            
            // Label
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(28, 12);
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            this.Controls.Add(picIndicator);
            this.Controls.Add(lblStatus);
        }

        private void UpdateVisuals()
        {
            if (_isActive)
            {
                this.BackColor = Color.FromArgb(240, 253, 244); // Green 50
                lblStatus.Text = "Sistem Aktif";
                lblStatus.ForeColor = Color.FromArgb(21, 128, 61); // Green 700
                picIndicator.Invalidate();
            }
            else
            {
                this.BackColor = Color.FromArgb(254, 242, 242); // Red 50
                lblStatus.Text = "Sistem Error";
                lblStatus.ForeColor = Color.FromArgb(185, 28, 28); // Red 700
                picIndicator.Invalidate();
            }
        }

        private void PicIndicator_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color color = _isActive ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
            e.Graphics.FillEllipse(new SolidBrush(color), 0, 0, 10, 10);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
             base.OnPaint(e);
             using (GraphicsPath path = GraphicsUtils.GetRoundedRectangle(this.ClientRectangle, 6))
             {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 // Fill background (UserControl doesn't always fill corners cleanly if parent has different color)
                 // But BackColor handles it mostly. We can draw border if needed.
             }
        }
    }
}
