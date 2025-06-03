using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using MplDbApi.Models.Dtos;

public class MaterialValueService(BMplbaseContext _context, ILogger<MaterialValueService> logger) : IMaterialValueService
{
    private class AveragesCalculated
    {
        public List<(DateOnly Date, decimal Value)> WeeklyAvgs { get; set; } = new();
        public List<(DateOnly Date, decimal Value)> MonthlyAvgs { get; set; } = new();
        public List<(DateOnly Date, decimal Value)> QuarterlyAvgs { get; set; } = new();
        public List<(DateOnly Date, decimal Value)> YearlyAvgs { get; set; } = new();
    }
    public async Task<MaterialValueResponseDto?> GetMaterialValueById(int id)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetMaterialValueById");
            throw;
        }

    }
    public async Task<List<MaterialDateMetrics>> GetMaterialMetricsByDateRange(MaterialDateMetricReq req)
    {
        try
        {
            List<DateOnly> months = GetDateRange(req.StartDate, req.EndDate);
            var valueGroups = await _context.MaterialValues
                .Where(mv =>
                    mv.Uid == req.MaterialId &&
                    months.Any(m => mv.CreatedOn.Year == m.Year && mv.CreatedOn.Month == m.Month) &&
                    req.PropertyIds.Contains(mv.PropertyId))
                .GroupBy(x => new { x.CreatedOn })
                .ToListAsync();
            var materialInfo = await GetCompactMaterialInfo(req.MaterialId);

            var values = valueGroups.Select(i =>
            {
                var propsUsed = i.Where(p => p.ValueDecimal.HasValue || !string.IsNullOrEmpty(p.ValueStr))
                    .Select(x => x.PropertyId)
                    .ToList();

                switch (req.Aggregates)
                {
                    case null:
                    case []:
                        // No aggregates requested, use only the properties in the request
                        break;
                    default:
                        // If aggregates are requested, ensure they are included in the propsUsed
                        propsUsed.AddRange(req.Aggregates.Select(a => a switch
                        {
                            // Using negative values to avoid conflicts with actual PropertyIds
                            "weekly" => -1,
                            "monthly" => -2,
                            "quarterly" => -3,
                            "yearly" => -4,
                            _ => 0 // Default case, should not happen
                        }).Where(p => p > 0 || p < 0)); // Only add valid aggregates
                        break;
                }

                return new MaterialDateMetrics(
                    Id: i.FirstOrDefault()?.Id ?? 0,
                    Date: i.Key.CreatedOn,
                    PropsUsed: propsUsed,
                    ValueAvg: i.Where(p => p.PropertyId == 1).Select(d => d.ValueDecimal).FirstOrDefault()?.ToString() ?? string.Empty,
                    ValueMin: i.Where(p => p.PropertyId == 2).Select(d => d.ValueDecimal).FirstOrDefault()?.ToString() ?? string.Empty,
                    ValueMax: i.Where(p => p.PropertyId == 3).Select(d => d.ValueDecimal).FirstOrDefault()?.ToString() ?? string.Empty,
                    PredWeekly: i.Where(p => p.PropertyId == 4).Select(d => d.ValueDecimal).FirstOrDefault()?.ToString() ?? string.Empty,
                    PredMonthly: i.Where(p => p.PropertyId == 5).Select(d => d.ValueDecimal).FirstOrDefault()?.ToString() ?? string.Empty,
                    Supply: i.Where(p => p.PropertyId == 6).Select(d => d.ValueDecimal).FirstOrDefault()?.ToString() ?? string.Empty,
                    WeeklyAvg: "",
                    MonthlyAvg: "",
                    QuarterlyAvg: "",
                    YearlyAvg: "",
                    MaterialInfo: materialInfo
                );
            }).ToList();

            if (req.Aggregates != null && req.Aggregates.Any())
            {
                var aggregates = await GetAverages((req.StartDate, req.EndDate), req.Aggregates, req.MaterialId);
                values = MergeAveragesIntoMetrics(values, aggregates);
            }

            return values;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetMaterialMetricsByDateRange");
            throw;
        }
    }
    public async Task<List<DateGroupedMaterialValues>> GetOverviewTableData(List<MaterialDateMetricReq> reqs)
    {
        try
        {
            var metrics = await GetMultipleMaterialDateMetrics(reqs);
            var groupedValues = GroupMultipleMaterialMetricsByDate(metrics);
            return groupedValues;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetOverviewTableData");
            throw;
        }

    }
    private async Task<CompactMaterialInfo?> GetCompactMaterialInfo(int id)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetCompactMaterialInfo");
            throw;
        }

    }
    private async Task<List<MaterialDateMetrics>> GetMultipleMaterialDateMetrics(List<MaterialDateMetricReq> reqs)
    {
        try
        {
            List<MaterialDateMetrics> metrics = [];
            foreach (var req in reqs)
            {
                metrics.AddRange(await GetMaterialMetricsByDateRange(req));
            }
            return metrics;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetMultipleMaterialDateMetrics");
            throw;
        }

    }
    private static List<DateGroupedMaterialValues> GroupMultipleMaterialMetricsByDate(List<MaterialDateMetrics> metrics)
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

    private async Task<List<MaterialDateMetrics>> GetMaterialDateMetrics(int materialId, (DateOnly, DateOnly) dateRange)
    {
        var req = new MaterialDateMetricReq(
        materialId,
        new List<int> { 1 },
        dateRange.Item1,
        dateRange.Item2,
        null
    );

        return await GetMaterialMetricsByDateRange(req);
    }
    private static List<MaterialDateMetrics> MergeAveragesIntoMetrics(List<MaterialDateMetrics> metrics, AveragesCalculated averages)
    {
        var merged = metrics.ToDictionary(m => m.Date);

        void AddAggregateValue(DateOnly date, string value, Func<MaterialDateMetrics, MaterialDateMetrics> applyAggregate)
        {
            if (merged.TryGetValue(date, out var existing))
            {
                merged[date] = applyAggregate(existing);
            }
            else
            {
                var template = metrics.FirstOrDefault();
                var newMetric = new MaterialDateMetrics(
                    Id: template?.Id ?? 0,
                    Date: date,
                    PropsUsed: template?.PropsUsed ?? new(),
                    ValueAvg: "",
                    ValueMin: "",
                    ValueMax: "",
                    PredWeekly: "",
                    PredMonthly: "",
                    Supply: "",
                    WeeklyAvg: "",
                    MonthlyAvg: "",
                    QuarterlyAvg: "",
                    YearlyAvg: "",
                    MaterialInfo: template?.MaterialInfo
                );
                merged[date] = applyAggregate(newMetric);
            }
        }
        foreach (var (date, value) in averages.MonthlyAvgs)
            AddAggregateValue(date, value.ToString("F2"), m => m with { MonthlyAvg = value.ToString("F2") });
        foreach (var (date, value) in averages.WeeklyAvgs)
            AddAggregateValue(date, value.ToString("F2"), m => m with { WeeklyAvg = value.ToString("F2") });
        foreach (var (date, value) in averages.QuarterlyAvgs)
            AddAggregateValue(date, value.ToString("F2"), m => m with { QuarterlyAvg = value.ToString("F2") });
        foreach (var (date, value) in averages.YearlyAvgs)
            AddAggregateValue(date, value.ToString("F2"), m => m with { YearlyAvg = value.ToString("F2") });
        return merged.Values.OrderBy(x => x.Date).ToList();
    }
    private async Task<AveragesCalculated> GetAverages((DateOnly, DateOnly) dateRange, List<string> aggregates, int materialId)
    {
        var result = new AveragesCalculated();

        if (aggregates.Contains("weekly"))
            result.WeeklyAvgs = await GetWeeklyAverage(dateRange, materialId);

        if (aggregates.Contains("monthly"))
            result.MonthlyAvgs = await GetMonthlyAverage(dateRange, materialId);

        if (aggregates.Contains("quarterly"))
            result.QuarterlyAvgs = await GetQuarterlyAverage(dateRange, materialId);

        if (aggregates.Contains("yearly"))
            result.YearlyAvgs = await GetYearlyAverage(dateRange, materialId);

        return result;
    }


    private async Task<List<(DateOnly, decimal)>> GetWeeklyAverage((DateOnly, DateOnly) dateRange, int materialId)
    {
        DateOnly GetWeekStart(DateOnly date)
        {
            int offset = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-offset);
        }
        var metrics = await GetMaterialDateMetrics(materialId, dateRange);

        var grouped = metrics
            .Where(m => decimal.TryParse(m.ValueAvg, out _))
            .GroupBy(m => GetWeekStart(m.Date))
            .Select(g => (
                Date: g.Key,
                Avg: g.Average(m => decimal.Parse(m.ValueAvg!))
            ))
            .ToList();

        var startWeek = GetWeekStart(dateRange.Item1);
        var endWeek = GetWeekStart(dateRange.Item2);

        return grouped
            .Where(g => g.Date >= startWeek && g.Date <= endWeek)
            .ToList();
    }



    private async Task<List<(DateOnly, decimal)>> GetMonthlyAverage((DateOnly, DateOnly) dateRange, int materialId)
    {
        DateOnly GetMonthStart(DateOnly date) => new DateOnly(date.Year, date.Month, 1);
        var metrics = await GetMaterialDateMetrics(materialId, dateRange);
        var grouped = metrics
            .Where(m => decimal.TryParse(m.ValueAvg, out _))
            .GroupBy(m => GetMonthStart(m.Date))
            .Select(g => (
                Date: g.Key,
                Avg: g.Average(m => decimal.Parse(m.ValueAvg!))
            ))
            .ToList();

        var startMonth = GetMonthStart(dateRange.Item1);
        var endMonth = GetMonthStart(dateRange.Item2);

        return grouped
            .Where(g => g.Date >= startMonth && g.Date <= endMonth)
            .ToList();
    }

    private async Task<List<(DateOnly, decimal)>> GetQuarterlyAverage((DateOnly, DateOnly) dateRange, int materialId)
    {
        DateOnly GetQuarterStart(DateOnly date)
        {
            int startMonth = ((date.Month - 1) / 3) * 3 + 1;
            return new DateOnly(date.Year, startMonth, 1);
        }

        var metrics = await GetMaterialDateMetrics(materialId, dateRange);

        var grouped = metrics
            .Where(m => decimal.TryParse(m.ValueAvg, out _))
            .GroupBy(m => GetQuarterStart(m.Date))
            .Select(g => (
                Date: g.Key,
                Avg: g.Average(m => decimal.Parse(m.ValueAvg!))
            ))
            .ToList();

        var startQuater = GetQuarterStart(dateRange.Item1);
        var endQuater = GetQuarterStart(dateRange.Item2);

        return grouped
            .Where(g => g.Date >= startQuater && g.Date <= endQuater)
            .ToList();
    }
    private async Task<List<(DateOnly, decimal)>> GetYearlyAverage((DateOnly, DateOnly) dateRange, int materialId)
    {
        DateOnly GetYearStart(DateOnly date) => new DateOnly(date.Year, 1, 1);
        var metrics = await GetMaterialDateMetrics(materialId, dateRange);

        var grouped = metrics
            .Where(m => decimal.TryParse(m.ValueAvg, out _))
            .GroupBy(m => GetYearStart(m.Date))
            .Select(g => (
                Date: g.Key,
                Avg: g.Average(m => decimal.Parse(m.ValueAvg!))
            ))
            .ToList();
        var startYear = GetYearStart(dateRange.Item1);
        var endYear = GetYearStart(dateRange.Item2);

        return grouped
            .Where(g => g.Date >= startYear && g.Date <= endYear)
            .ToList();
    }
}