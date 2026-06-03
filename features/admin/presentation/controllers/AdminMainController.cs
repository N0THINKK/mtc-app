using System.Windows.Forms;

namespace mtc_app.features.admin.presentation.controllers
{
    public class AdminMainController
    {
        private readonly IAdminMainView _view;

        public AdminMainController(IAdminMainView view)
        {
            _view = view;
        }

        public void NavigateTo(UserControl targetView)
        {
            _view.LoadView(targetView);
        }
    }
}
