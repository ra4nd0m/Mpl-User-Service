using MplDbApi.Models.Dtos;

namespace MplDbApi.Interfaces;

public interface IMaterialGroupService
{
    Task<IEnumerable<MaterialGroupDto>> GetMaterialGroupAsync();
}