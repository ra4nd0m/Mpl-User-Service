using MplDbApi.Interfaces;

namespace MplDbApi.Routes
{
    public static class UnitRoutes
    {
        public static void MapUnitRoutes(this WebApplication app)
        {
            app.MapGet("/units", async (IUnitService unitService) =>
            {
                try
                {
                    var units = await unitService.GetUnits();
                    return Results.Ok(units);
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error with receiving units list.");
                    return Results.Problem("Error with receiving units.");
                }

            });
        }
    }
}