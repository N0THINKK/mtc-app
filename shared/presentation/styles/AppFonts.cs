using System.Drawing;

namespace mtc_app.shared.presentation.styles
{
    public static class AppFonts
    {
        public static readonly string FontFamily = "Segoe UI";

        public static Font Header1 => new Font(FontFamily, 22, FontStyle.Bold);
        public static Font Header2 => new Font(FontFamily, 18, FontStyle.Bold);
        public static Font Header3 => new Font(FontFamily, 14, FontStyle.Bold);
        
        public static Font Title => new Font(FontFamily, 12, FontStyle.Bold);
        public static Font Subtitle => new Font(FontFamily, 11, FontStyle.Bold);
        
        public static Font Body => new Font(FontFamily, 11, FontStyle.Regular);
        public static Font BodySmall => new Font(FontFamily, 10, FontStyle.Regular);
        
        public static Font Caption => new Font(FontFamily, 9, FontStyle.Regular);
        
        public static Font Button => new Font(FontFamily, 11, FontStyle.Bold);
    }
}
