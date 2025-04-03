using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ExpressServicePOS.Infrastructure.Configuration
{
    /// <summary>
    /// Provides centralized configuration management for database connections.
    /// </summary>
    public static class ConnectionConfiguration
    {
        /// <summary>
        /// Gets the SQL Server connection string from configuration.
        /// </summary>
        /// <returns>The configured connection string.</returns>
        public static string GetSqlServerConnectionString()
        {
            // Load configuration from JSON file
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false);

            var configuration = builder.Build();

            // Get connection string
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured.");
            }

            return connectionString;
        }
    }
}