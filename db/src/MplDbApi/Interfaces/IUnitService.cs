using MplDbApi.Models.Dtos;

namespace MplDbApi.Interfaces
{
    public interface IUnitService
    {
        Task<IEnumerable<IdValuePair>> GetUnits();
    }
}