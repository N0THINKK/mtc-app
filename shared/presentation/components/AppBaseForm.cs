using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class AppBaseForm : Form
    {
        public AppBaseForm()
        {
            this.Font = AppFonts.Body;
            this.BackColor = AppColors.Background;
            this.ForeColor = AppColors.TextPrimary;
        }
    }
}
