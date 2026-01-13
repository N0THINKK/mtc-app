using System.Drawing;

namespace mtc_app.shared.presentation.styles
{
    public static class AppColors
    {
        // Brand Colors
        public static readonly Color Primary = Color.FromArgb(0, 120, 212); // Office Blue / Windows Blue
        public static readonly Color PrimaryDark = Color.FromArgb(0, 90, 158);
        public static readonly Color PrimaryLight = Color.FromArgb(202, 233, 255);

        public static readonly Color Secondary = Color.FromArgb(43, 87, 154);
        
        // Semantic Colors
        public static readonly Color Success = Color.FromArgb(16, 124, 16);
        public static readonly Color Warning = Color.FromArgb(255, 140, 0);
        public static readonly Color Error = Color.FromArgb(209, 52, 56);
        public static readonly Color Info = Color.FromArgb(0, 99, 177);

        // Neutral Colors
        public static readonly Color TextPrimary = Color.FromArgb(32, 31, 30);   // Almost Black
        public static readonly Color TextSecondary = Color.FromArgb(96, 94, 92); // Dark Gray
        public static readonly Color TextDisabled = Color.FromArgb(161, 159, 157);

        public static readonly Color Background = Color.FromArgb(250, 250, 250); // Off-white for app background
        public static readonly Color Surface = Color.White;                // White for cards/panels
        
        public static readonly Color Border = Color.FromArgb(225, 223, 221);
        public static readonly Color BorderFocus = Primary;
        
        public static readonly Color Separator = Color.FromArgb(237, 235, 233);
    }
}
