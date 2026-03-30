using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Models.Filters;

namespace MplDbApi.Utils
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeFilterDatabase(IServiceProvider serviceProvider, ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();
            var filterContext = scope.ServiceProvider.GetRequiredService<FilterContext>();

            try
            {
                // Ensure the database is created
                await filterContext.Database.EnsureCreatedAsync();

                // Check if filters already exist
                if (await filterContext.Filters.AnyAsync())
                {
                    logger.LogInformation("Filter database already contains data");
                    return;
                }

                // Create default filters for each role
                var defaultFilters = new List<DataFilter>
                {
                    new DataFilter
                    {
                        AffectedRole = "Free",
                        Groups = [],
                        Sources = [],
                        Units = [],
                        MaterialIds = [],
                        Properties = []
                    },
                    new DataFilter
                    {
                        AffectedRole = "Basic",
                        Groups = [],
                        Sources = [],
                        Units = [],
                        MaterialIds = [],
                        Properties = []
                    },
                    new DataFilter
                    {
                        AffectedRole = "Premium",
                        Groups = [],
                        Sources = [],
                        Units = [],
                        MaterialIds = [],
                        Properties = []
                    },
                    new DataFilter
                    {
                        AffectedRole = "Admin",
                        Groups = [],
                        Sources = [],
                        Units = [],
                        MaterialIds = [],
                        Properties = []
                    }
                };

                await filterContext.Filters.AddRangeAsync(defaultFilters);
                await filterContext.SaveChangesAsync();

                logger.LogInformation("Default filter roles created: free, basic, premium, admin");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize filter database");
                throw;
            }
        }

        public static async Task InitializeMaterialDatabase(IServiceProvider serviceProvider, ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();
            var materialContext = scope.ServiceProvider.GetRequiredService<BMplbaseContext>();

            try
            {
                await materialContext.Database.MigrateAsync();

                logger.LogInformation("Material database initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize material database");
                throw;
            }
        }
    }
}