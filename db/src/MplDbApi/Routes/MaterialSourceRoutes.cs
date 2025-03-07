using MplDbApi.Interfaces;
using MplDbApi.Models;

namespace MplDbApi.Routes
{
    public static class MaterialSourceRoutes
    {
        public static void MapMaterialSourceRoutes(this WebApplication app)
        {
            app.MapGet("/materials", async (IMaterialSource materialService) =>
            {
                var materials = await materialService.GetAllMaterials();
                return Results.Ok(materials);
            });

            app.MapGet("/materials/{id:int}", async (int id, IMaterialSource materialService) =>
            {
                var material = await materialService.GetMaterialById(id);
                return Results.Ok(material);
            });
        }
    }
}