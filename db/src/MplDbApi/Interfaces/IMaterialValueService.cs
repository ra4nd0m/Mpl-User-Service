using MplDbApi.Models;

namespace MplDbApi.Interfaces;

public interface IMaterialValueService
{
    Task<MaterialValueResponseDto?> GetMaterialValueById(int id);
}
