using MplDbApi.Models.Dtos;

namespace MplDbApi.Interfaces;

public interface IDeliveryTypeService
{
    Task<IEnumerable<DeliveryTypeDto>> GetDeliveryTypesAsync();
    Task<DeliveryTypeDto?> GetDeliveryTypeByIdAsync(int id);
}
