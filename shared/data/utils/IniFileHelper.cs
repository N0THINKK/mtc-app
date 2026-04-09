using System;
using System.Runtime.InteropServices;
using System.Text;

namespace mtc_app.shared.data.utils
{
    public static class IniFileHelper
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// Reads a value from an INI file.
        /// </summary>
        /// <param name="section">The section name (without brackets).</param>
        /// <param name="key">The key name.</param>
        /// <param name="filePath">The full path to the INI file.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>The value read from the INI file, or default value.</returns>
        public static string ReadValue(string section, string key, string filePath, string defaultValue = "")
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, retVal, 255, filePath);
            return retVal.ToString();
        }

        /// <summary>
        /// Writes a value to an INI file.
        /// </summary>
        public static void WriteValue(string section, string key, string value, string filePath)
        {
            WritePrivateProfileString(section, key, value, filePath);
        }
    }
}
