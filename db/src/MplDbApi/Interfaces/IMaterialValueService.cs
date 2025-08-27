using MplDbApi.Models;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Interfaces;

public interface IMaterialValueService
{
    Task<MaterialValueResponseDto?> GetMaterialValueById(int id, string role);
    Task<List<MaterialDateMetrics>> GetMaterialMetricsByDateRange(MaterialDateMetricReq req, string role);
    Task<List<DateGroupedMaterialValues>> GetOverviewTableData(List<MaterialDateMetricReq> reqs, string role);
}
