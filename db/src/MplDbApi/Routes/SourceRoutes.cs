using MplDbApi.Interfaces;

namespace MplDbApi.Routes
{
    public static class SourceRoutes
    {
        public static void MapSourceRoutes(this WebApplication app)
        {
            app.MapGet("/sources", async (ISourceService sourceService) =>
            {
                try
                {
                    var sources = await sourceService.GetSources();
                    return Results.Ok(sources);
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error with receiving sources list.");
                    return Results.Problem("Error with receiving sources.");
                }

            }).RequireAuthorization();
        }
    }
}