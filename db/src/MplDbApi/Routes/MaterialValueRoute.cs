using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Routes
{
    public static class MaterialValueRoutes
    {
        public static void MapMaterialValueRoutes(this WebApplication app)
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("MaterialValueRoutes");

            app.MapGet("/materialvalues/{id:int}", async (IMaterialValueService materialValue, int id) =>
            {
                try
                {
                    var materialValueItem = await materialValue.GetMaterialValueById(id);
                    return materialValueItem is not null
                        ? Results.Ok(materialValueItem)
                        : Results.NotFound($"Material with id {id} not found.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while fetching material value with ID {Id}", id);
                    return Results.Problem($"An error occurred while retrieving material value with ID {id}.");
                }
            });
            app.MapPost("/materialvalues/overview", async (IMaterialValueService service, List<MaterialDateMetricReq> req) =>
            {
                try
                {
                    var overviewData = await service.GetOverviewTableData(req);
                    return Results.Ok(overviewData);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while fetching overview data");
                    return Results.Problem("An error occurred while retrieving overview data");
                }
            });
            app.MapPost("/materialvalues/valuerange", async (IMaterialValueService service, MaterialDateMetricReq req) =>
            {
                try
                {
                    var valueRange = await service.GetMaterialMetricsByDateRange(req);
                    return Results.Ok(valueRange);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while fetching value range data");
                    return Results.Problem("An error occurred while retrieving value range data");
                }
            });
        }
    }
}
