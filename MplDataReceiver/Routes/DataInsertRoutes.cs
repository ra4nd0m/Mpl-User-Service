using MplDataReceiver.Models.DTOs;
using MplDataReceiver.Services;

namespace MplDataReceiver.Routes;

public static class DataInsertRoutes
{
    public static void MapDataInsertRoutes(this WebApplication app)
    {
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("MaterialValueRoutes");

        app.MapPost("/insertBatch", async (List<MaterialUpdate> updates, DataInsertService dataInsertService) =>
        {
            try
            {
                await dataInsertService.InsertValuesRange(updates);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inserting data");
                return Results.Problem("Error inserting data");
            }

        });
    }
}