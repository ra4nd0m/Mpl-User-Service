using MplDbApi.Models.Dtos;

namespace MplDbApi.Interfaces
{
    public interface IMaterialPropService
    {
        Task<List<MaterialPropertyResp>> GetMaterialProperties(int materialTd);
        Task<List<IdValuePair>> GetMaterialPropertiesForDropdown();
    }
}