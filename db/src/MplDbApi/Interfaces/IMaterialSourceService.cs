using MplDbApi.Models;

namespace MplDbApi.Interfaces;

public interface IMaterialSourceService
{
    Task<List<MaterialSourceResponseDto>> GetAllMaterials(string role);
    Task<MaterialSourceResponseDto?> GetMaterialById(int id, string role);
    Task<List<MaterialSourceResponseDto>> GetMaterialsByGroup(int groupId, string role);
}