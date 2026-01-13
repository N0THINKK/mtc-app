using System.Drawing;

namespace mtc_app.shared.presentation.styles
{
    public static class AppFonts
    {
        public static readonly string BaseFontName = "Segoe UI";

        public static Font Header1 => new Font(BaseFontName, 20, FontStyle.Bold);
        public static Font Header2 => new Font(BaseFontName, 16, FontStyle.Bold);
        public static Font Header3 => new Font(BaseFontName, 14, FontStyle.Bold);

        public static Font Title => new Font(BaseFontName, 12, FontStyle.Bold);
        public static Font Subtitle => new Font(BaseFontName, 10, FontStyle.Bold);

        public static Font Body => new Font(BaseFontName, 10, FontStyle.Regular);
        public static Font BodySmall => new Font(BaseFontName, 9, FontStyle.Regular);

        public static Font Caption => new Font(BaseFontName, 8, FontStyle.Regular);

        public static Font Button => new Font(BaseFontName, 10, FontStyle.Bold);
    }
}
