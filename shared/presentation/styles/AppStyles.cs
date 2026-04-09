using System.Drawing;
using System.Reflection;

namespace mtc_app.shared.presentation.styles
{
    public static class AppStyles
    {
        public static readonly Icon AppIcon;

        static AppStyles()
        {
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon.ico");
                if (stream != null)
                {
                    AppIcon = new Icon(stream);
                }
                else
                {
                    AppIcon = SystemIcons.Application;
                }
            }
            catch
            {
                // Fallback to default if somehow missing
                AppIcon = SystemIcons.Application;
            }
        }
    }
}
