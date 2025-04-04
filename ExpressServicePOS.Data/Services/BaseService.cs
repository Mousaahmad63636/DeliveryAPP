using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ExpressServicePOS.Data.Services
{
    public abstract class BaseService
    {
        protected readonly IDbContextFactory<AppDbContext> DbContextFactory;
        protected readonly ILogger Logger;

        protected BaseService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger logger)
        {
            DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected async Task<T> ExecuteDbOperationAsync<T>(Func<AppDbContext, Task<T>> operation)
        {
            using var dbContext = await DbContextFactory.CreateDbContextAsync();
            return await operation(dbContext);
        }

        protected async Task ExecuteDbOperationAsync(Func<AppDbContext, Task> operation)
        {
            using var dbContext = await DbContextFactory.CreateDbContextAsync();
            await operation(dbContext);
        }
    }
}