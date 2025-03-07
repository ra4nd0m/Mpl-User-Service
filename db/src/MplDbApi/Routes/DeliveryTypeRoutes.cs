using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Routes;

public static class DeliveryTypeRoutes
{
    public static void MapDeliveryTypeRoutes(this WebApplication app)
    {
        app.MapGet("/deliverytypes", async (IDeliveryTypeService service) =>
        {
            var deliveryTypes = await service.GetDeliveryTypesAsync();
            return Results.Ok(deliveryTypes);
        });

        app.MapGet("/deliverytypes/{id}", async (IDeliveryTypeService service, int id) =>
        {
            var deliveryType = await service.GetDeliveryTypeByIdAsync(id);
            return deliveryType != null ? Results.Ok(deliveryType) : Results.NotFound();
        });
    }
}
