using MplDbApi.Models.Dtos;
using MplDbApi.Services;

namespace MplDbApi.Routes
{
    public static class FilterConfigRoutes
    {
        public static void MapFilterConfigRoutes(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            app.MapPost("/filter", async (RoleEnhancedReqDto<FilterCreateReqDto> input, FilterService filterService) =>
            {
                try
                {
                    if (input.Role == null || input.Data == null)
                    {
                        return Results.BadRequest("Role and filter data must be provided.");
                    }
                    if (input.Role != "Admin")
                    {
                        return Results.Forbid();
                    }
                    await filterService.ModifyFilter(input.Data);
                    return Results.Ok("Filter modified successfully.");
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "Error modifying filter.");
                    return Results.Problem("Failed to modify filter.");
                }
            });

            app.MapGet("/filter/{role}", async (string role, FilterService filterService) =>
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
            });
        }
    }
}