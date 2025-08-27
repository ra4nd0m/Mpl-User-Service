using MplDbApi.Models.Dtos;

namespace MplDbApi.Interfaces
{
    public interface ISourceService
    {
        Task<IEnumerable<IdValuePair>> GetSources();
    }
}