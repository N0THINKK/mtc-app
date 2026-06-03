using System.IO;

namespace mtc_app.features.operator_worksheet.data.providers
{
    public class MachineConfigurationProvider
    {
        public string BaseDirectory { get; }

        public MachineConfigurationProvider(string explicitBaseDir = null)
        {
            if (explicitBaseDir != null)
            {
                BaseDirectory = explicitBaseDir;
            }
            else
            {
                if (Directory.Exists(@"C:\AC90HMI\prg\"))
                    BaseDirectory = @"C:\AC90HMI\prg\";
                else if (Directory.Exists(@"C:\AC80HMI\"))
                    BaseDirectory = @"C:\AC80HMI\";
                else
                    BaseDirectory = @"C:\AC90HMI\prg\"; // Fallback default
            }
        }

        public string GetFilePath(string fileName, string[] customFallbacks = null)
        {
            string defaultPath = Path.Combine(BaseDirectory, fileName);
            if (File.Exists(defaultPath))
            {
                return defaultPath;
            }

            if (customFallbacks != null)
            {
                foreach (var fb in customFallbacks)
                {
                    if (File.Exists(fb))
                    {
                        return fb;
                    }
                }
            }

            return null;
        }
    }
}
