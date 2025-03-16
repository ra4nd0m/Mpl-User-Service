using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using MplDbApi.Models.Dtos;

public class MaterialValueService(BMplbaseContext _context, ILogger<MaterialValueService> logger) : IMaterialValueService
{
    public async Task<MaterialValueResponseDto?> GetMaterialValueById(int id)
    {
        var materialvalue = await _context.MaterialValues
            .Where(mv => mv.Id == id)
            .Select(mv => new MaterialValueResponseDto(
                mv.Id,
                mv.Uid,
                mv.PropertyId,
                mv.ValueDecimal ?? 0,
                mv.ValueStr ?? string.Empty,
                mv.CreatedOn,
                mv.LastUpdated
            ))
            .AsNoTracking()
            .FirstOrDefaultAsync();
        return materialvalue;
    }
    public async Task<List<MaterialDateMetrics>> GetMaterialMetricsByDateRange(MaterialDateMetricReq req)
    {
        List<DateOnly> months = GetDateRange(req.StartDate, req.EndDate);
        var valueGroups = await _context.MaterialValues
            .Where(mv => mv.Uid == req.MaterialId && months.Any(m => mv.CreatedOn.Year == m.Year && mv.CreatedOn.Month == m.Month) && req.PropertyIds.Contains(mv.PropertyId))
            .GroupBy(x => new { x.CreatedOn })
            .ToListAsync();
        var materialInfo = await GetCompactMaterialInfo(req.MaterialId);

        var values = valueGroups.Select(i =>
        {
            var propsUsed = i.Select(x => x.PropertyId).ToList();
            return new MaterialDateMetrics(
                Id: i.FirstOrDefault()?.Id ?? 0,
                Date: i.Key.CreatedOn,
                PropsUsed: propsUsed,
                ValueAvg: i.Where(p => p.PropertyId == 1).Select(d => d.ValueDecimal).FirstOrDefault().ToString() ?? string.Empty,
                ValueMin: i.Where(p => p.PropertyId == 2).Select(d => d.ValueDecimal).FirstOrDefault().ToString() ?? string.Empty,
                ValueMax: i.Where(p => p.PropertyId == 3).Select(d => d.ValueDecimal).FirstOrDefault().ToString() ?? string.Empty,
                PredWeekly: i.Where(p => p.PropertyId == 4).Select(d => d.ValueDecimal).FirstOrDefault().ToString() ?? string.Empty,
                PredMonthly: i.Where(p => p.PropertyId == 5).Select(d => d.ValueDecimal).FirstOrDefault().ToString() ?? string.Empty,
                Supply: i.Where(p => p.PropertyId == 6).Select(d => d.ValueDecimal).FirstOrDefault().ToString() ?? string.Empty,
                MonthlyAvg: "",
                MaterialInfo: materialInfo
            );
        }).ToList();
        return values;
    }
    public async Task<List<DateGroupedMaterialValues>> GetOverviewTableData(List<MaterialDateMetricReq> reqs)
    {
        var metrics = await GetMultipleMaterialDateMetrics(reqs);
        var groupedValues = GroupMultipleMaterialMetricsByDate(metrics);
        return groupedValues;
    }
    private async Task<CompactMaterialInfo?> GetCompactMaterialInfo(int id)
    {
        var materialInf = await _context.MaterialSources
            .Where(ms => ms.Id == id)
            .Include(ms => ms.DeliveryType)
            .Include(ms => ms.Material)
            .Include(ms => ms.Unit)
            .Select(ms => new CompactMaterialInfo(
                ms.Id,
                ms.Material.Name,
                ms.DeliveryType.Name,
                ms.TargetMarket,
                ms.Unit.Name
            )).AsNoTracking().FirstOrDefaultAsync();
        return materialInf;
    }

    private async Task<List<MaterialDateMetrics>> GetMultipleMaterialDateMetrics(List<MaterialDateMetricReq> reqs)
    {
        List<MaterialDateMetrics> metrics = [];
        foreach (var req in reqs)
        {
            metrics.AddRange(await GetMaterialMetricsByDateRange(req));
        }
        return metrics;
    }

    private List<DateGroupedMaterialValues> GroupMultipleMaterialMetricsByDate(List<MaterialDateMetrics> metrics)
    {
        var groupedValues = metrics
            .GroupBy(x => x.Date)
            .Select(i => new DateGroupedMaterialValues(
                Date: i.Key,
                MaterialValues: [.. i]
            ))
            .ToList();
        return groupedValues;
    }

    private static List<DateGroupedMaterialValues> GetDateGroupedMaterialValues(List<MaterialDateMetrics> values)
    {
        var groupedValues = values.GroupBy(x => x.Date).Select(i =>
        {
            return new DateGroupedMaterialValues(
                Date: i.Key,
                MaterialValues: [.. i]
            );
        });
        return [.. groupedValues];
    }

    private static List<DateOnly> GetDateRange(DateOnly startDate, DateOnly endDate)
    {
        List<DateOnly> months = [];
        DateOnly currentDate = new(startDate.Year, startDate.Month, 1);
        DateOnly finishDate = new(endDate.Year, endDate.Month, 1);
        while (currentDate <= finishDate)
        {
            months.Add(currentDate);
            currentDate = currentDate.AddMonths(1);
        }
        return months;
    }
}
