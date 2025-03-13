using MplDbApi.Models;

namespace MplDbApi.Interfaces;

public interface IMaterialValue
{
    Task<MaterialValueResponseDto?> GetMaterialValueById(int id);
}
