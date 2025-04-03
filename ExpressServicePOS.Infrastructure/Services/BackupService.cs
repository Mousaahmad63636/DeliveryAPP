using System;
using System.IO;
using System.Threading.Tasks;
using ExpressServicePOS.Data.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpressServicePOS.Infrastructure.Services
{
    public class BackupService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<BackupService> _logger;

        public BackupService(AppDbContext dbContext, ILogger<BackupService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a backup of the database.
        /// </summary>
        /// <param name="backupPath">The path where the backup file should be saved</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> CreateBackupAsync(string backupPath)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Get database name from connection string
                var connection = _dbContext.Database.GetDbConnection() as SqlConnection;
                if (connection == null)
                {
                    _logger.LogError("Failed to get SQL connection for backup");
                    return false;
                }

                var databaseName = connection.Database;

                // Create backup command
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $@"
                        BACKUP DATABASE [{databaseName}] 
                        TO DISK = '{backupPath}' 
                        WITH FORMAT, MEDIANAME = 'ExpressServicePOS_Backup', 
                        NAME = 'Full Backup of ExpressServicePOS DB';";

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    await command.ExecuteNonQueryAsync();

                    _logger.LogInformation($"Database backed up successfully to: {backupPath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database backup");
                return false;
            }
        }

        /// <summary>
        /// Restores the database from a backup file.
        /// </summary>
        /// <param name="backupPath">The path to the backup file</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    _logger.LogError($"Backup file not found: {backupPath}");
                    return false;
                }

                // Get database name from connection string
                var connection = _dbContext.Database.GetDbConnection() as SqlConnection;
                if (connection == null)
                {
                    _logger.LogError("Failed to get SQL connection for restore");
                    return false;
                }

                var databaseName = connection.Database;

                // First, close all existing connections
                using (var commandClose = new SqlCommand())
                {
                    commandClose.Connection = connection;
                    commandClose.CommandText = $@"
                        ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    await commandClose.ExecuteNonQueryAsync();
                }

                // Restore the database
                using (var commandRestore = new SqlCommand())
                {
                    commandRestore.Connection = connection;
                    commandRestore.CommandText = $@"
                        RESTORE DATABASE [{databaseName}] 
                        FROM DISK = '{backupPath}' 
                        WITH REPLACE;";

                    await commandRestore.ExecuteNonQueryAsync();
                }

                // Set database back to multi-user mode
                using (var commandMulti = new SqlCommand())
                {
                    commandMulti.Connection = connection;
                    commandMulti.CommandText = $@"
                        ALTER DATABASE [{databaseName}] SET MULTI_USER;";

                    await commandMulti.ExecuteNonQueryAsync();
                }

                _logger.LogInformation($"Database restored successfully from: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring database backup");
                return false;
            }
        }

        /// <summary>
        /// Exports all database data to a file (for simpler backup approach).
        /// </summary>
        /// <param name="exportPath">The path where the export file should be saved</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ExportDataAsync(string exportPath)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(exportPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create script generation command
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

                    if (command.Connection.State != System.Data.ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        using (var writer = new StreamWriter(exportPath, false))
                        {
                            writer.WriteLine("-- Express Service POS Database Export");
                            writer.WriteLine($"-- Date: {DateTime.Now}");
                            writer.WriteLine();

                            while (await reader.ReadAsync())
                            {
                                var tableName = reader["TABLE_NAME"].ToString();

                                // Skip system tables
                                if (tableName.StartsWith("sys") || tableName == "__EFMigrationsHistory")
                                    continue;

                                await ExportTableData(tableName, writer);
                            }
                        }
                    }
                }

                _logger.LogInformation($"Database data exported successfully to: {exportPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting database data");
                return false;
            }
        }

        private async Task ExportTableData(string tableName, StreamWriter writer)
        {
            writer.WriteLine($"-- Table: {tableName}");
            writer.WriteLine();

            // Get column names
            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = $"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var columns = new System.Collections.Generic.List<string>();
                    while (await reader.ReadAsync())
                    {
                        columns.Add(reader["COLUMN_NAME"].ToString());
                    }

                    writer.WriteLine($"-- Columns: {string.Join(", ", columns)}");
                    writer.WriteLine();
                }
            }

            // Get data
            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = $"SELECT * FROM [{tableName}]";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    int rowCount = 0;
                    var columns = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        columns[i] = reader.GetName(i);
                    }

                    while (await reader.ReadAsync())
                    {
                        var values = new string[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (!reader.IsDBNull(i))
                            {
                                if (reader.GetFieldType(i) == typeof(string) ||
                                    reader.GetFieldType(i) == typeof(DateTime) ||
                                    reader.GetFieldType(i) == typeof(Guid))
                                {
                                    values[i] = $"'{reader.GetValue(i).ToString().Replace("'", "''")}'";
                                }
                                else
                                {
                                    values[i] = reader.GetValue(i).ToString();
                                }
                            }
                            else
                            {
                                values[i] = "NULL";
                            }
                        }

                        string columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
                        string valueList = string.Join(", ", values);
                        writer.WriteLine($"INSERT INTO [{tableName}] ({columnList}) VALUES ({valueList});");
                        rowCount++;
                    }

                    writer.WriteLine();
                    writer.WriteLine($"-- {rowCount} rows exported from {tableName}");
                    writer.WriteLine();
                }
            }
        }
    }
}