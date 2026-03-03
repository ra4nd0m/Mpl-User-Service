using MplDbApi.Models.Dtos;
using MplDbApi.Services;

namespace MplDbApi.Routes
{
    public static class FilterConfigRoutes
    {
        public static void MapFilterConfigRoutes(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            var filterRouteGroup = app.MapGroup("/filter-config")
                .WithTags("Filter Configuration");

            filterRouteGroup.MapPost("/filter", async (FilterCreateReqDto data, FilterService filterService) =>
            {
                try
                {
                    if (data == null)
                    {
                        return Results.BadRequest("Filter data must be provided.");
                    }

                    await filterService.ModifyFilter(data);
                    return Results.Ok("Filter modified successfully.");
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "Error modifying filter.");
                    return Results.Problem("Failed to modify filter.");
                }
            }).RequireAuthorization("RequireAdmin");

            filterRouteGroup.MapGet("/filter/{role}", async (string role, FilterService filterService) =>
            {
                try
                {
                    var filter = await filterService.GetFilterByRole(role);
                    return Results.Ok(filter);
                }
                catch (KeyNotFoundException ex)
                {
                    logger.LogWarning(ex, "No filter found for role: {Role}", role);
                    return Results.NotFound($"No filter found for role: {role}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving filter for role: {Role}", role);
                    return Results.Problem("Failed to retrieve filter.");
                }
            }).RequireAuthorization("RequireAdmin");

            filterRouteGroup.MapGet("/filters", async (FilterService filterService) =>
            {
                try
                {
                    var filters = await filterService.GetAllFilters();
                    return Results.Ok(filters);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving all filters.");
                    return Results.Problem("Failed to retrieve filters.");
                }
            }).RequireAuthorization("RequireAdmin");
        }
    }
}