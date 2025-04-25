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

            app.MapGet("/materials", async (IMaterialSourceService materialService) =>
            {
                try
                {
                    var materials = await materialService.GetAllMaterials();
                    return Results.Ok(materials);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error with receiving materials list.");
                    return Results.Problem("Error with receiving materials.");
                }
            });

            app.MapGet("/materials/{id:int}", async (int id, IMaterialSourceService materialService) =>
            {
                try
                {
                    var material = await materialService.GetMaterialById(id);
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
            });

            app.MapGet("/materials/bygroup/{id:int}", async (int groupId, IMaterialSourceService service) =>
            {
                try
                {
                    var materials = await service.GetMaterialsByGroup(groupId);
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
            });
        }
    }
}