using MplDbApi.Models;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Interfaces;

public interface IMaterialValueService
{
    Task<MaterialValueResponseDto?> GetMaterialValueById(int id);
    Task<List<MaterialDateMetrics>> GetMaterialMetricsByDateRange (MaterialDateMetricReq req);
    //Task<List<DateGroupedMaterialValues>> (int id, List<int> propIds, DateOnly startDate, DateOnly endDate);
}
