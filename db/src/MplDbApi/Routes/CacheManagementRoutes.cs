using MplDbApi.Services;

namespace MplDbApi.Routes
{
    public static class CacheManagementRoutes
    {
        public static void MapCacheManagementRoutes(this WebApplication app)
        {
            var cacheGroup = app.MapGroup("/cache");
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            cacheGroup.MapPost("/clear", (CacheManagementService cacheService) =>
            {
                try
                {
                    cacheService.ClearCache();
                    return Results.Ok(new { message = "Cache cleared successfully" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error clearing cache");
                    return Results.Problem("Failed to clear cache");
                }

            });
        }
    }
}