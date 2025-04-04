using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ExpressServicePOS.Infrastructure.Services
{
    public class DatabaseTestService : BaseService
    {
        public DatabaseTestService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<DatabaseTestService> logger)
            : base(dbContextFactory, logger)
        {
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                Logger.LogInformation("Testing database connection...");

                return await ExecuteDbOperationAsync(async (dbContext) =>
                {
                    bool canConnect = await dbContext.Database.CanConnectAsync();

                    if (canConnect)
                    {
                        string dbName = dbContext.Database.GetDbConnection().Database;
                        Logger.LogInformation($"Successfully connected to database: {dbName}");
                        return (true, $"تم الاتصال بقاعدة البيانات بنجاح: {dbName}");
                    }
                    else
                    {
                        Logger.LogWarning("Could not connect to the database");
                        return (false, "فشل الاتصال بقاعدة البيانات");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error occurred while testing database connection");
                return (false, $"حدث خطأ أثناء الاتصال بقاعدة البيانات: {ex.Message}");
            }
        }
    }
}