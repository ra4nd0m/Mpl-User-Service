using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;

namespace MplAuthService.Utils
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabase(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthContext>();

            try
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migrated");

                if (configuration.GetValue<bool>("AdminInitialization:Enabled"))
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    await InitializeAdminUser(userService, configuration, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database");
            }
        }
        private static async Task InitializeAdminUser(
            IUserService userService,
            IConfiguration configuration,
            ILogger logger)
        {
            var adminEmail = configuration["AdminInitialization:Email"];
            var adminPassword = configuration["AdminInitialization:Password"];
            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                logger.LogWarning("Admin initialization is enabled but credentials are not provided");
                return;
            }

            try
            {
                await userService.CreateAdmin(adminEmail, adminPassword);
                logger.LogInformation("Admin user created");
            }
            catch (InvalidOperationException e)
            {
                logger.LogError(e, "Failed to create admin user");
            }
        }
    }
}