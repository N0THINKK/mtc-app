using System;
using System.Windows.Forms;
using mtc_app.features.authentication.presentation.screens; // Correct Namespace
using mtc_app.shared.data.utils; // For DatabaseHelper

namespace mtc_app
{
    static class Program
    {
        // Global Services - UserSession is static, no need to declare here.

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // WinForms Setup
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // UserSession is static, no initialization needed.

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
            }

            // Run Main Form (Login)
            Application.Run(new LoginForm());
        }
    }
}