using System.Drawing;

namespace mtc_app.shared.presentation.styles
{
    public static class AppColors
    {
        // Primary Brand Color (Modern Blue)
        public static readonly Color Primary = Color.FromArgb(25, 118, 210); 
        public static readonly Color PrimaryDark = Color.FromArgb(13, 71, 161);
        public static readonly Color PrimaryLight = Color.FromArgb(187, 222, 251);

        // Functional Colors
        public static readonly Color Success = Color.FromArgb(46, 125, 50);  // Green
        public static readonly Color Warning = Color.FromArgb(255, 160, 0);  // Amber/Orange
        public static readonly Color Danger = Color.FromArgb(211, 47, 47);   // Red
        public static readonly Color Info = Color.FromArgb(2, 136, 209);     // Light Blue
        
        // Aliases for compatibility with existing components
        public static readonly Color Error = Danger;

        // Neutral Colors
        public static readonly Color TextPrimary = Color.FromArgb(33, 33, 33);
        public static readonly Color TextSecondary = Color.FromArgb(117, 117, 117);
        public static readonly Color TextDisabled = Color.FromArgb(189, 189, 189); // Added
        
        public static readonly Color Background = Color.White;
        public static readonly Color Surface = Color.FromArgb(245, 245, 245); // Light Gray
        public static readonly Color Separator = Color.FromArgb(224, 224, 224);

        // Borders
        public static readonly Color Border = Color.FromArgb(224, 224, 224);      // Added
        public static readonly Color BorderFocus = Primary;                      // Added
        
        // Icons
        public static readonly Color IconColor = Primary;
    }
}