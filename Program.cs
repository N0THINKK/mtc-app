using System;
using System.Windows.Forms;
using mtc_app.features.authentication.presentation.screens;

namespace mtc_app
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check if Machine is configured
            string machineId = DatabaseHelper.GetMachineId();

            if (string.IsNullOrWhiteSpace(machineId) || machineId == "0")
            {
                // Show Setup Form
                SetupForm setup = new SetupForm();
                if (setup.ShowDialog() != DialogResult.OK)
                {
                    // If user cancels setup, exit app
                    return;
                }
            }

            // Continue to Login
            Application.Run(new LoginForm());
        }    
    }
}