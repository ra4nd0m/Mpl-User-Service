using MplDbApi.Interfaces;

namespace MplDbApi.Routes
{
    public static class MaterialPropertyRoutes
    {
        public static void MapMaterialPropertyRoutes(this WebApplication app)
        {
            app.MapGet("/material/{materialId}/properties", async (IMaterialPropService service, int materialId) =>
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
            }).RequireAuthorization();

            app.MapGet("/properties/dropdown", async (IMaterialPropService service) =>
            {
                try
                {
                    var props = await service.GetMaterialPropertiesForDropdown();
                    return Results.Ok(props);
                }
                catch
                {
                    return Results.Problem("An error occurred while retrieving material properties for dropdown");
                }
            }).RequireAuthorization();
        }
    }
}