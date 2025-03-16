using MplDbApi.Interfaces;

namespace MplDbApi.Routes
{
    public static class MaterialPropertyRoutes
    {
        public static void MapMaterialPropertyRoutes(this WebApplication app)
        {
            app.MapGet("/api/material/{materialId}/properties", async (IMaterialPropService service, int materialId) =>
            {
                try
                {
                    var props = await service.GetMaterialProperties(materialId);
                    return Results.Ok(props);
                }
                catch
                {
                    return Results.Problem("An error occurred while retrieving material properties");
                }
            });
        }
    }
}