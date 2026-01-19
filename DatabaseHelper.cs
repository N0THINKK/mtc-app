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
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }

        public static string ConnectionString => _configuration.GetConnectionString("DefaultConnection");

        public static IDbConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public static string GetMachineId()
        {
            LoadConfig(); // Ensure latest
            return _configuration["AppSettings:MachineID"];
        }

        public static void UpdateMachineConfig(string machineId)
        {
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