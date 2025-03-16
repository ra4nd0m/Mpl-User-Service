using MplDbApi.Interfaces;
using MplDbApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        }
    }
}
