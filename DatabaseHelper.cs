using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace mtc_app
{
    public static class DatabaseHelper
    {
        private static IConfiguration _configuration;

        static DatabaseHelper()
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
    }
}