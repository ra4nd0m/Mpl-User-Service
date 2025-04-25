using MplDbApi.Models;

namespace MplDbApi.Interfaces;

public interface IMaterialSourceService
{
    Task<List<MaterialSourceResponseDto>> GetAllMaterials();
    Task<MaterialSourceResponseDto?> GetMaterialById(int id);
    Task<List<MaterialSourceResponseDto>> GetMaterialsByGroup(int groupId);
}