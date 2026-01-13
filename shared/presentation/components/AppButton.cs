using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class AppButton : Button
    {
        public enum ButtonType
        {
            Primary,
            Secondary,
            Outline,
            Danger,
            Warning
        }

        private ButtonType _type = ButtonType.Primary;

        public AppButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.Font = AppFonts.Button;
            this.Size = new Size(120, AppDimens.ControlHeight);
            
            ApplyStyle();
            
            this.MouseEnter += (sender, e) => OnMouseEnter();
            this.MouseLeave += (sender, e) => OnMouseLeave();
        }

        public ButtonType Type
        {
            get => _type;
            set
            {
                _type = value;
                ApplyStyle();
            }
        }

        private void ApplyStyle()
        {
            switch (_type)
            {
                case ButtonType.Primary:
                    this.BackColor = AppColors.Primary;
                    this.ForeColor = Color.White;
                    this.FlatAppearance.BorderSize = 0;
                    break;
                case ButtonType.Secondary:
                    this.BackColor = AppColors.Surface;
                    this.ForeColor = AppColors.TextPrimary;
                    this.FlatAppearance.BorderSize = 1;
                    this.FlatAppearance.BorderColor = AppColors.Border;
                    break;
                case ButtonType.Outline:
                    this.BackColor = Color.Transparent;
                    this.ForeColor = AppColors.Primary;
                    this.FlatAppearance.BorderSize = 1;
                    this.FlatAppearance.BorderColor = AppColors.Primary;
                    break;
                case ButtonType.Danger:
                    this.BackColor = AppColors.Error;
                    this.ForeColor = Color.White;
                    this.FlatAppearance.BorderSize = 0;
                    break;
                case ButtonType.Warning:
                    this.BackColor = AppColors.Warning;
                    this.ForeColor = Color.White;
                    this.FlatAppearance.BorderSize = 0;
                    break;
            }
        }

        private void OnMouseEnter()
        {
            switch (_type)
            {
                case ButtonType.Primary:
                    this.BackColor = AppColors.PrimaryDark;
                    break;
                case ButtonType.Secondary:
                    this.BackColor = AppColors.Separator;
                    break;
                case ButtonType.Outline:
                    this.BackColor = AppColors.PrimaryLight;
                    break;
                case ButtonType.Danger:
                    this.BackColor = ControlPaint.Dark(AppColors.Error);
                    break;
                case ButtonType.Warning:
                    this.BackColor = ControlPaint.Dark(AppColors.Warning);
                    break;
            }
        }

        private void OnMouseLeave()
        {
            ApplyStyle();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            
            // Optional: Draw rounded corners manually if needed, 
            // but standard FlatStyle doesn't support radius easily without Region clipping.
            // For simplicity and performance, we stick to rectangular or use simple Region.
            
            // Simple rounded region
            int radius = AppDimens.CornerRadius;
            Rectangle rectangle = new Rectangle(0, 0, this.Width, this.Height);
            System.Drawing.Drawing2D.GraphicsPath graphicspath = new System.Drawing.Drawing2D.GraphicsPath();
            graphicspath.AddArc(rectangle.X, rectangle.Y, radius, radius, 180, 90);
            graphicspath.AddArc(rectangle.X + rectangle.Width - radius, rectangle.Y, radius, radius, 270, 90);
            graphicspath.AddArc(rectangle.X + rectangle.Width - radius, rectangle.Y + rectangle.Height - radius, radius, radius, 0, 90);
            graphicspath.AddArc(rectangle.X, rectangle.Y + rectangle.Height - radius, radius, radius, 90, 90);
            this.Region = new Region(graphicspath);
        }
    }
}
