using System.Drawing;

namespace mtc_app.shared.presentation.utils
{
    public static class TicketStatusUtils
    {
        public static (Color Background, Color Text) GetStatusBadgeColors(int statusId)
        {
            switch (statusId)
            {
                case 1: // Belum Ditangani (Not Repaired) - Red
                    return (Color.FromArgb(254, 242, 242), Color.FromArgb(185, 28, 28));
                case 2: // Sedang Diperbaiki (Repairing) - Orange
                    return (Color.FromArgb(255, 247, 237), Color.FromArgb(194, 65, 12));
                case 3: // Selesai (Done) - Green
                    return (Color.FromArgb(240, 253, 244), Color.FromArgb(21, 128, 61));
                default: // Unknown - Gray
                    return (Color.FromArgb(243, 244, 246), Color.FromArgb(55, 65, 81));
            }
        }

        public static Color GetStatusStripColor(int statusId)
        {
            switch (statusId)
            {
                case 1: return Color.FromArgb(239, 68, 68); // Red
                case 2: return Color.FromArgb(249, 115, 22); // Orange
                case 3: return Color.FromArgb(34, 197, 94);  // Green
                default: return Color.Gray;
            }
        }

        public static string GetStatusText(int statusId)
        {
            switch (statusId)
            {
                case 1: return "Belum Ditangani";
                case 2: return "Sedang Diperbaiki";
                case 3: return "Selesai";
                default: return "Unknown";
            }
        }
    }
}
