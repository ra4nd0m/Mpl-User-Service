using MplDbApi.Models;

namespace MplDbApi.Interfaces;

public interface IMaterialSource
{
    Task<List<MaterialSourceDto>> GetAllMaterials();
    Task<MaterialSourceDto?> GetMaterialById(int id);
}