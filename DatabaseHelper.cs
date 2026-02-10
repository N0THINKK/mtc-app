using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Newtonsoft.Json.Linq;

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
            // [FIX] Use BaseDirectory to ensure appsettings.json is found
            // even if the app is launched from a shortcut or different folder.
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

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
            // Pastikan path penyimpanan juga menggunakan BaseDirectory
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
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