using System.Security.Claims;
using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;
using Microsoft.Extensions.Logging;

namespace MplDbApi.Routes;

public static class MaterialGroupRoutes
{
    public static void MapMaterialGroupRoutes(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        app.MapGet("/materialgroups", async(HttpContext context, IMaterialGroupService service) =>
        {
            try
            {
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                var subscription = context.User.FindFirst("SubscriptionType")?.Value;
                var extractedRole = (role == "Admin") ? "Admin" : subscription ?? "Free";

                var groups = await service.GetMaterialGroupAsync(extractedRole);
                return Results.Ok(groups);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error with receiving material group list.");
                return Results.Problem("Error with receiving material groups.");
            }
        }).RequireAuthorization();
    }
}
