using System;
using System.Windows.Forms;
using mtc_app.features.authentication.presentation.screens;
using mtc_app.shared.data.services;

namespace mtc_app
{
    static class Program
    {
        public static MachineMonitorService MonitorService { get; private set; }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check if Machine is configured
            string machineIdStr = DatabaseHelper.GetMachineId();

            if (string.IsNullOrWhiteSpace(machineIdStr) || machineIdStr == "0")
            {
                // Show Setup Form
                SetupForm setup = new SetupForm();
                if (setup.ShowDialog() != DialogResult.OK)
                {
                    // If user cancels setup, exit app
                    return;
                }
                // Refresh after setup
                machineIdStr = DatabaseHelper.GetMachineId();
            }

            // Start Monitoring Service if Machine ID is valid
            if (int.TryParse(machineIdStr, out int machineId) && machineId > 0)
            {
                MonitorService = new MachineMonitorService();
                MonitorService.Initialize(machineId);
            }

            // Continue to Login
            Application.Run(new LoginForm());
            
            // Cleanup on exit
            MonitorService?.Stop();
        }    
    }
}