using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;
using Microsoft.Extensions.Logging;

namespace MplDbApi.Routes;

public static class MaterialGroupRoutes
{
    public static void MapMaterialGroupRoutes(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        app.MapGet("/materialgroups", async(string? role, IMaterialGroupService service) =>
        {
            try
            {
                var groups = await service.GetMaterialGroupAsync(role);
                return Results.Ok(groups);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error with receiving material group list.");
                return Results.Problem("Error with receiving material groups.");
            }
        });
    }
}
