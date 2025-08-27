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

            app.MapGet("/materialvalues/{id:int}", async (string role, IMaterialValueService materialValue, int id) =>
            {
                try
                {
                    if(string.IsNullOrEmpty(role))
                    {
                        return Results.BadRequest("Role must be provided.");
                    }
                    var materialValueItem = await materialValue.GetMaterialValueById(id, role);
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
            app.MapPost("/materialvalues/overview", async (IMaterialValueService service, RoleEnhancedReqDto<List<MaterialDateMetricReq>> req) =>
            {
                try
                {
                    if (req.Data == null || req.Data.Count == 0)
                    {
                        return Results.BadRequest("Request data cannot be null or empty.");
                    }
                    if (string.IsNullOrEmpty(req.Role))
                    {
                        return Results.BadRequest("Role must be provided.");
                    }
                    var overviewData = await service.GetOverviewTableData(req.Data, req.Role);
                    return Results.Ok(overviewData);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while fetching overview data");
                    return Results.Problem("An error occurred while retrieving overview data");
                }
            });
            app.MapPost("/materialvalues/daterange", async (IMaterialValueService service, RoleEnhancedReqDto<MaterialDateMetricReq> req) =>
            {
                try
                {
                    if (req.Data == null)
                    {
                        return Results.BadRequest("Request data cannot be null.");
                    }
                    if (string.IsNullOrEmpty(req.Role))
                    {
                        return Results.BadRequest("Role must be provided.");
                    }
                    var valueRange = await service.GetMaterialMetricsByDateRange(req.Data, req.Role);
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
