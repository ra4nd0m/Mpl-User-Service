using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;
using Microsoft.Extensions.Logging;

namespace MplDbApi.Routes;

public static class DeliveryTypeRoutes
{
    public static void MapDeliveryTypeRoutes(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        app.MapGet("/deliverytypes", async (IDeliveryTypeService service) =>
        {
            try
            {
                var deliveryTypes = await service.GetDeliveryTypesAsync();
                return Results.Ok(deliveryTypes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error with receiving delivery types list.");
                return Results.Problem("Error with receiving delivery types.");
            }
        }).RequireAuthorization();

        app.MapGet("/deliverytypes/{id}", async (IDeliveryTypeService service, int id) =>
        {
            try
            {
                var deliveryType = await service.GetDeliveryTypeByIdAsync(id);
                if (deliveryType != null)
                {
                    return Results.Ok(deliveryType);
                }

                logger.LogWarning("Delivery type with ID {Id} not found.", id);
                return Results.NotFound($"Delivery type with ID {id} not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error with receiving delivery type with ID {Id}.", id);
                return Results.Problem("Error with receving delivery type.");
            }
        }).RequireAuthorization();
    }
}
