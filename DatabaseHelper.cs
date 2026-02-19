using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace mtc_app
{
    public static class DatabaseHelper
    {
        private static IConfiguration _configuration;

        static DatabaseHelper()
        {
            LoadConfig();
        }

        private static void LoadConfig()
        {
            // [FIX] Use Process.GetCurrentProcess().MainModule.FileName to get the directory of the .exe
            // explicitly. AppDomain.CurrentDomain.BaseDirectory points to the temp extraction folder
            // in SingleFile apps.
            var processModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            var basePath = Path.GetDirectoryName(processModule?.FileName);

            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath) 
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            
            _configuration = builder.Build();
        }

        public static string ConnectionString => _configuration.GetConnectionString("DefaultConnection");

        /// <summary>
        /// Gets a database connection using the configured timeout.
        /// </summary>
        public static IDbConnection GetConnection()
        {
            // [FIX] Respect the timeout in appsettings.json (60s)
            // Do NOT force a short 3s timeout for remote connections.
            return new MySqlConnection(ConnectionString);
        }

        public static string GetMachineId()
        {
            LoadConfig(); // Ensure latest config is loaded
            return _configuration["AppSettings:MachineID"];
        }

        public static void UpdateMachineConfig(string machineId)
        {
            // Use Process.GetCurrentProcess().MainModule.FileName to get the directory of the .exe
            var processModule = Process.GetCurrentProcess().MainModule;
            var basePath = Path.GetDirectoryName(processModule?.FileName);
            string path = Path.Combine(basePath, "appsettings.json");
            string json = File.ReadAllText(path);
            
            JObject jsonObj = JObject.Parse(json);
            
            if (jsonObj["AppSettings"] == null)
            {
                jsonObj["AppSettings"] = new JObject();
            }

            jsonObj["AppSettings"]["MachineID"] = machineId;
            // LineID is no longer used

            File.WriteAllText(path, jsonObj.ToString());
            
            // Reload configuration in memory
            LoadConfig();
        }
    }
}