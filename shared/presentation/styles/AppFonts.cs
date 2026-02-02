using System.Drawing;

namespace mtc_app.shared.presentation.styles
{
    public static class AppFonts
    {
        public static readonly string FontFamily = "Segoe UI";

        // Hierarchy: +2pt to +4pt increase
        public static Font Header1 => new Font(FontFamily, 26, FontStyle.Bold); // Was 22
        public static Font Header2 => new Font(FontFamily, 22, FontStyle.Bold); // Was 18
        public static Font Header3 => new Font(FontFamily, 16.5F, FontStyle.Bold); // Was 14
        
        public static Font Title => new Font(FontFamily, 14.5F, FontStyle.Bold); // Was 12
        public static Font Subtitle => new Font(FontFamily, 13, FontStyle.Bold); // Was 11
        
        // Body: +2pt increase for readability
        public static Font Body => new Font(FontFamily, 13, FontStyle.Regular); // Was 11
        public static Font BodySmall => new Font(FontFamily, 12, FontStyle.Regular); // Was 10
        
        public static Font Caption => new Font(FontFamily, 10.5F, FontStyle.Regular); // Was 9
        
        public static Font Button => new Font(FontFamily, 13, FontStyle.Bold); // Was 11
        
        // Metric/Stat Card Fonts
        public static Font MetricLarge => new Font(FontFamily, 40, FontStyle.Bold);  // Was 36
        public static Font MetricMedium => new Font(FontFamily, 28, FontStyle.Bold); // Was 24
        public static Font MetricSmall => new Font(FontFamily, 18, FontStyle.Bold);  // Was 16
    }
}
