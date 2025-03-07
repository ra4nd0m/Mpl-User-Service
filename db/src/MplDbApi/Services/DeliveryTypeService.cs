using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Services;

public class DeliveryTypeService : IDeliveryTypeService
{
    private readonly BMplbaseContext _context;

    public DeliveryTypeService(BMplbaseContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DeliveryTypeDto>> GetDeliveryTypesAsync()
    {
        return await _context.DeliveryTypes
            .Select(dt => new DeliveryTypeDto(dt.Id, dt.Name))
            .ToListAsync();
    }

    public async Task<DeliveryTypeDto?> GetDeliveryTypeByIdAsync(int id)
    {
        var deliveryType = await _context.DeliveryTypes.FindAsync(id);
        return deliveryType != null ? new DeliveryTypeDto(deliveryType.Id, deliveryType.Name) : null;
    }
}
