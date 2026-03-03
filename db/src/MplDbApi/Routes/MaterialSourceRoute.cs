using System.Security.Claims;
using MplDbApi.Interfaces;
using MplDbApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MplDbApi.Routes
{
    public static class MaterialSourceRoutes
    {
        public static void MapMaterialSourceRoutes(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            app.MapGet("/materials", async (HttpContext context, IMaterialSourceService materialService) =>
            {
                try
                {
                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var subscription = context.User.FindFirst("SubscriptionType")?.Value;
                    var extractedRole = (role == "Admin") ? "Admin" : subscription ?? "Free";

                    var materials = await materialService.GetAllMaterials(extractedRole);
                    return Results.Ok(materials);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error with receiving materials list.");
                    return Results.Problem("Error with receiving materials.");
                }
            }).RequireAuthorization();

            app.MapGet("/materials/{id:int}", async (HttpContext context, int id, IMaterialSourceService materialService) =>
            {
                try
                {
                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var subscription = context.User.FindFirst("SubscriptionType")?.Value;
                    var extractedRole = (role == "Admin") ? "Admin" : subscription ?? "Free";

                    var material = await materialService.GetMaterialById(id, extractedRole);
                    return Results.Ok(material);
                }
                catch (KeyNotFoundException ex)
                {
                    logger.LogWarning(ex, "Meterial with ID {Id} was not found.", id);
                    return Results.NotFound($"Material with ID {id} was not found.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error with receiving material with ID {Id}.", id);
                    return Results.Problem("Error while receiving the material.");
                }
            }).RequireAuthorization();

            app.MapGet("/materials/bygroup/{groupId:int}", async (HttpContext context, int groupId, IMaterialSourceService service) =>
            {
                try
                {
                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var subscription = context.User.FindFirst("SubscriptionType")?.Value;
                    var extractedRole = (role == "Admin") ? "Admin" : subscription ?? "Free";

                    var materials = await service.GetMaterialsByGroup(groupId, extractedRole);
                    return Results.Ok(materials);
                }
                catch (KeyNotFoundException ex)
                {
                    logger.LogWarning(ex, "Meterial with group ID {Id} was not found.", groupId);
                    return Results.NotFound($"Material with group ID {groupId} was not found.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error with receiving materials with group ID {Id}.", groupId);
                    return Results.Problem("Error while receiving the material.");
                }
            }).RequireAuthorization();
        }
    }
}