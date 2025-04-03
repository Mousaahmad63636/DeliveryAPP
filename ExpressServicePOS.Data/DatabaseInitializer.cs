using ExpressServicePOS.Data.Context;
using Microsoft.Extensions.Logging;
using System;

namespace ExpressServicePOS.Data
{
    public class DatabaseInitializer
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Initialize()
        {
            try
            {
                _logger.LogInformation("Initializing database...");

                // Ensure database is created
                _context.Database.EnsureCreated();

                _logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }
    }
}