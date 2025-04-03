using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ExpressServicePOS.Infrastructure.Services
{
    public class DatabaseTestService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseTestService> _logger;

        public DatabaseTestService(AppDbContext context, ILogger<DatabaseTestService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");

                bool canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    string dbName = _context.Database.GetDbConnection().Database;
                    _logger.LogInformation($"Successfully connected to database: {dbName}");
                    return (true, $"تم الاتصال بقاعدة البيانات بنجاح: {dbName}");
                }
                else
                {
                    _logger.LogWarning("Could not connect to the database");
                    return (false, "فشل الاتصال بقاعدة البيانات");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while testing database connection");
                return (false, $"حدث خطأ أثناء الاتصال بقاعدة البيانات: {ex.Message}");
            }
        }
    }
}