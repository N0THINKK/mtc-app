using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class AppLabel : Label
    {
        public enum LabelType
        {
            Header1,
            Header2,
            Header3,
            Title,
            Subtitle,
            Body,
            BodySmall,
            Caption
        }

        private LabelType _type = LabelType.Body;

        public AppLabel()
        {
            this.AutoSize = true;
            this.ForeColor = AppColors.TextPrimary;
            ApplyStyle();
        }

        public LabelType Type
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
                case LabelType.Header1:
                    this.Font = AppFonts.Header1;
                    this.ForeColor = AppColors.Primary;
                    break;
                case LabelType.Header2:
                    this.Font = AppFonts.Header2;
                    this.ForeColor = AppColors.TextPrimary;
                    break;
                case LabelType.Header3:
                    this.Font = AppFonts.Header3;
                    this.ForeColor = AppColors.TextPrimary;
                    break;
                case LabelType.Title:
                    this.Font = AppFonts.Title;
                    this.ForeColor = AppColors.TextPrimary;
                    break;
                case LabelType.Subtitle:
                    this.Font = AppFonts.Subtitle;
                    this.ForeColor = AppColors.TextSecondary;
                    break;
                case LabelType.Body:
                    this.Font = AppFonts.Body;
                    this.ForeColor = AppColors.TextPrimary;
                    break;
                case LabelType.BodySmall:
                    this.Font = AppFonts.BodySmall;
                    this.ForeColor = AppColors.TextSecondary;
                    break;
                case LabelType.Caption:
                    this.Font = AppFonts.Caption;
                    this.ForeColor = AppColors.TextDisabled;
                    break;
            }
        }
    }
}
