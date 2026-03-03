using System.Security.Claims;
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

            app.MapGet("/materialvalues/{id:int}", async (HttpContext context, IMaterialValueService materialValue, int id) =>
            {
                try
                {
                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var subscription = context.User.FindFirst("SubscriptionType")?.Value;
                    var extractedRole = (role == "Admin") ? "Admin" : subscription ?? "Free";

                    var materialValueItem = await materialValue.GetMaterialValueById(id, extractedRole);
                    return materialValueItem is not null
                        ? Results.Ok(materialValueItem)
                        : Results.NotFound($"Material with id {id} not found.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while fetching material value with ID {Id}", id);
                    return Results.Problem($"An error occurred while retrieving material value with ID {id}.");
                }
            }).RequireAuthorization();

            app.MapPost("/materialvalues/overview", async (HttpContext context, IMaterialValueService service, List<MaterialDateMetricReq> data) =>
            {
                try
                {
                    if (data == null || data.Count == 0)
                    {
                        return Results.BadRequest("Request data cannot be null or empty.");
                    }

                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var subscription = context.User.FindFirst("SubscriptionType")?.Value;
                    var extractedRole = (role == "Admin") ? "Admin" : subscription ?? "Free";

                    var overviewData = await service.GetOverviewTableData(data, extractedRole);
                    return Results.Ok(overviewData);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while fetching overview data");
                    return Results.Problem("An error occurred while retrieving overview data");
                }
            }).RequireAuthorization();

            app.MapPost("/materialvalues/daterange", async (HttpContext context, IMaterialValueService service, MaterialDateMetricReq data) =>
            {
                try
                {
                    if (data == null)
                    {
                        return Results.BadRequest("Request data cannot be null.");
                    }

                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var subscription = context.User.FindFirst("SubscriptionType")?.Value;
                    var extractedRole = (role == "Admin") ? "Admin" : subscription ?? "Free";

                    var valueRange = await service.GetMaterialMetricsByDateRange(data, extractedRole);
                    return Results.Ok(valueRange);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while fetching value range data");
                    return Results.Problem("An error occurred while retrieving value range data");
                }
            }).RequireAuthorization();
        }
    }
}
