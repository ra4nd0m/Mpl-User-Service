using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Interfaces;
using MplDbApi.Utils;
using Microsoft.EntityFrameworkCore;
using MplDbApi.Services;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

public class MaterialSourceService(BMplbaseContext _context, FilterService filterService, IMemoryCache memoryCache) : IMaterialSourceService
{
    private readonly struct PropertyValueInfo
    {
        public int PropertyId { get; }
        public decimal? ValueDecimal { get; }

        // Constructor needed for LINQ Select
        public PropertyValueInfo(int propertyId, decimal? valueDecimal)
        {
            PropertyId = propertyId;
            ValueDecimal = valueDecimal;
        }
    }
    public async Task<List<MaterialSourceResponseDto>> GetAllMaterials(string role)
    {
        string cacheKey = role + "AllMaterialsCacheKey";
        if (memoryCache.TryGetValue(cacheKey, out List<MaterialSourceResponseDto>? cachedMaterials) && cachedMaterials != null)
        {
            return cachedMaterials;
        }
        var filter = await filterService.GetFilterByRole(role);
        int[] latestValuePropertyIds = { 1, 2, 3, 6 };
        int[] availablePropertyIds = { 1, 2, 3, 4, 5, 6 };
        var emptyValueList = new List<PropertyValueInfo>();

        // Step 1: Query the database for intermediate data
        var intermediateResults = await _context.MaterialSources
            .AsSplitQuery()
            .Include(m => m.Material)
            .Include(m => m.Source)
            .Include(m => m.DeliveryType)
            .Include(m => m.MaterialGroup)
            .Include(m => m.Unit)
            .Where(m => filter.MaterialIds == null || !filter.MaterialIds.Contains(m.Id)) // Exclude materials from filter
            .Where(m => filter.Groups == null || !filter.Groups.Contains(m.MaterialGroupId)) // Filter by groups
            .Where(m => filter.Sources == null || !filter.Sources.Contains(m.SourceId)) // Filter by sources
            .Where(m => filter.Units == null || !filter.Units.Contains(m.UnitId)) // Filter by units
            .Select(m => new // Project to find the latest date first
            {
                MaterialSource = m,
                LatestDate = _context.MaterialValues
                    .Where(mv => mv.Uid == m.Id && latestValuePropertyIds.Contains(mv.PropertyId))
                    .GroupBy(mv => mv.CreatedOn)
                    .OrderByDescending(g => g.Key)
                    .Take(2)
                    .Select(g => g.Key)
                    .ToList(),
                AvailableProps = _context.MaterialProperties
                .Where(mp => mp.Uid == m.Id && availablePropertyIds.Contains(mp.PropertyId))
                .Select(mp => mp.PropertyId)
                .ToList() // Materialize AvailableProps list
            })
            .Select(result => new // Project to get values for that latest date
            {
                result.MaterialSource,
                result.LatestDate,
                result.AvailableProps,
                LatestValues = result.LatestDate == null || result.LatestDate.Count == 0 ?
                    emptyValueList :
                    _context.MaterialValues
                        .Where(mv => mv.Uid == result.MaterialSource.Id &&
                                     mv.CreatedOn == result.LatestDate.Max() &&
                                     latestValuePropertyIds.Contains(mv.PropertyId))
                        // Select into the named struct
                        .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                        // Materialize the list of the struct
                        .ToList(),
                PreviousValues = result.LatestDate == null || result.LatestDate.Count < 2 ?
                    emptyValueList :
                    _context.MaterialValues
                        .Where(mv => mv.Uid == result.MaterialSource.Id &&
                                    mv.CreatedOn == result.LatestDate.Min() &&
                                    latestValuePropertyIds.Contains(mv.PropertyId))
                        .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                        .ToList()
            })
            .AsNoTracking()
            .ToListAsync(); // Execute the database query


        // Step 2: Perform the final projection in memory
        var materialsList = intermediateResults.Select(finalResult =>
        {
            var latestAvgValue = finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 1).ValueDecimal;
            var previousAvgValue = finalResult.PreviousValues.FirstOrDefault(mv => mv.PropertyId == 1).ValueDecimal;

            string changePercent = "";
            if (latestAvgValue.HasValue && previousAvgValue.HasValue)
            {
                changePercent = ValueChangeFormatter.FormatValueChange(previousAvgValue.Value, latestAvgValue.Value);
            }
            DateOnly? latestDate = finalResult.LatestDate != null && finalResult.LatestDate.Count != 0
                ? finalResult.LatestDate.Max()
                : null;

            return new MaterialSourceResponseDto(
                   finalResult.MaterialSource.Id,
                   finalResult.MaterialSource.Material.Name,
                   finalResult.MaterialSource.Source.Name,
                   finalResult.MaterialSource.DeliveryType.Name,
                   finalResult.MaterialSource.MaterialGroup.Name,
                   finalResult.MaterialSource.TargetMarket,
                   finalResult.MaterialSource.Unit.Name,
                   latestDate, // Get the latest date
                   changePercent,
                   // These FirstOrDefault calls now operate on the in-memory list
                   finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 1).ValueDecimal,
                   finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 2).ValueDecimal,
                   finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 3).ValueDecimal,
                   finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 6).ValueDecimal,
                   finalResult.MaterialSource.Description,
                   finalResult.MaterialSource.RoundTo,
                   finalResult.AvailableProps
               );
        }).ToList(); // Create the final list of DTOs

        memoryCache.Set(cacheKey, materialsList, TimeSpan.FromHours(2)); // Cache the result for 10 minutes

        return materialsList;
    }

    public async Task<List<MaterialSourceResponseDto>> GetMaterialsByGroup(int groupId, string role)
    {
        string cacheKey = "MaterialsByGroupCacheKey_" + groupId;
        if (memoryCache.TryGetValue(cacheKey, out List<MaterialSourceResponseDto>? cachedMaterials) && cachedMaterials != null)
        {
            return cachedMaterials;
        }

        var filter = await filterService.GetFilterByRole(role);
        int[] latestValuePropertyIds = { 1, 2, 3, 6 };
        int[] availablePropertyIds = { 1, 2, 3, 4, 5, 6 };
        var emptyValueList = new List<PropertyValueInfo>();

        // Step 1: same pattern as GetAllMaterials (two latest distinct dates)
        var intermediateResults = await _context.MaterialSources
            .AsSplitQuery()
            .Include(m => m.Material)
            .Include(m => m.Source)
            .Include(m => m.DeliveryType)
            .Include(m => m.MaterialGroup)
            .Include(m => m.Unit)
            .Where(m => filter.MaterialIds == null || !filter.MaterialIds.Contains(m.Id))
            .Where(m => filter.Groups == null || !filter.Groups.Contains(m.MaterialGroupId))
            .Where(m => filter.Sources == null || !filter.Sources.Contains(m.SourceId))
            .Where(m => filter.Units == null || !filter.Units.Contains(m.UnitId))
            .Where(m => m.MaterialGroupId == groupId && m.Unit.Id != 5)
            .Select(m => new
            {
                MaterialSource = m,
                LatestDate = _context.MaterialValues
                    .Where(mv => mv.Uid == m.Id && latestValuePropertyIds.Contains(mv.PropertyId))
                    .GroupBy(mv => mv.CreatedOn)
                    .OrderByDescending(g => g.Key)
                    .Take(2)
                    .Select(g => g.Key)
                    .ToList(),
                AvailableProps = _context.MaterialProperties
                    .Where(mp => mp.Uid == m.Id && availablePropertyIds.Contains(mp.PropertyId))
                    .Select(mp => mp.PropertyId)
                    .ToList()
            })
            .Select(result => new
            {
                result.MaterialSource,
                result.LatestDate,
                result.AvailableProps,
                LatestValues = result.LatestDate == null || result.LatestDate.Count == 0
                    ? emptyValueList
                    : _context.MaterialValues
                        .Where(mv => mv.Uid == result.MaterialSource.Id
                                     && mv.CreatedOn == result.LatestDate.Max()
                                     && latestValuePropertyIds.Contains(mv.PropertyId))
                        .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                        .ToList(),
                PreviousValues = result.LatestDate == null || result.LatestDate.Count < 2
                    ? emptyValueList
                    : _context.MaterialValues
                        .Where(mv => mv.Uid == result.MaterialSource.Id
                                     && mv.CreatedOn == result.LatestDate.Min()
                                     && latestValuePropertyIds.Contains(mv.PropertyId))
                        .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                        .ToList()
            })
            .AsNoTracking()
            .ToListAsync();

        // Step 2: in-memory projection
        var materialsList = intermediateResults.Select(finalResult =>
        {
            var latestAvgValue = finalResult.LatestValues.FirstOrDefault(v => v.PropertyId == 1).ValueDecimal;
            var previousAvgValue = finalResult.PreviousValues.FirstOrDefault(v => v.PropertyId == 1).ValueDecimal;

            string changePercent = "";
            if (latestAvgValue.HasValue && previousAvgValue.HasValue)
            {
                changePercent = ValueChangeFormatter.FormatValueChange(previousAvgValue.Value, latestAvgValue.Value);
            }

            DateOnly? latestDate = finalResult.LatestDate != null && finalResult.LatestDate.Count != 0
                ? finalResult.LatestDate.Max()
                : null;

            return new MaterialSourceResponseDto(
                finalResult.MaterialSource.Id,
                finalResult.MaterialSource.Material.Name,
                finalResult.MaterialSource.Source.Name,
                finalResult.MaterialSource.DeliveryType.Name,
                finalResult.MaterialSource.MaterialGroup.Name,
                finalResult.MaterialSource.TargetMarket,
                finalResult.MaterialSource.Unit.Name,
                latestDate,
                changePercent,
                finalResult.LatestValues.FirstOrDefault(v => v.PropertyId == 1).ValueDecimal,
                finalResult.LatestValues.FirstOrDefault(v => v.PropertyId == 2).ValueDecimal,
                finalResult.LatestValues.FirstOrDefault(v => v.PropertyId == 3).ValueDecimal,
                finalResult.LatestValues.FirstOrDefault(v => v.PropertyId == 6).ValueDecimal,
                finalResult.MaterialSource.Description,
                finalResult.MaterialSource.RoundTo,
                finalResult.AvailableProps
            );
        }).ToList();

        memoryCache.Set(cacheKey, materialsList, TimeSpan.FromHours(2));
        return materialsList;
    }

    public async Task<MaterialSourceResponseDto?> GetMaterialById(int id, string role)
    {
        string cacheKey = "MaterialByIdCacheKey_" + id;
        if (memoryCache.TryGetValue(cacheKey, out MaterialSourceResponseDto? cachedMaterial) && cachedMaterial != null)
        {
            return cachedMaterial;
        }

        var filter = await filterService.GetFilterByRole(role);
        int[] latestValuePropertyIds = { 1, 2, 3, 6 };
        int[] availablePropertyIds = { 1, 2, 3, 4, 5, 6 };
        var emptyValueList = new List<PropertyValueInfo>();

        // Query with same pattern as GetAllMaterials: fetch up to two latest distinct dates, then values for those dates
        var intermediate = await _context.MaterialSources
            .AsSplitQuery()
            .Include(m => m.Material)
            .Include(m => m.Source)
            .Include(m => m.DeliveryType)
            .Include(m => m.MaterialGroup)
            .Include(m => m.Unit)
            .Where(m => m.Id == id)
            .Where(m => filter.MaterialIds == null || !filter.MaterialIds.Contains(m.Id))
            .Where(m => filter.Groups == null || !filter.Groups.Contains(m.MaterialGroupId))
            .Where(m => filter.Sources == null || !filter.Sources.Contains(m.SourceId))
            .Where(m => filter.Units == null || !filter.Units.Contains(m.UnitId))
            .Select(m => new
            {
                MaterialSource = m,
                LatestDate = _context.MaterialValues
                    .Where(mv => mv.Uid == m.Id && latestValuePropertyIds.Contains(mv.PropertyId))
                    .GroupBy(mv => mv.CreatedOn)
                    .OrderByDescending(g => g.Key)
                    .Take(2)
                    .Select(g => g.Key)
                    .ToList(),
                AvailableProps = _context.MaterialProperties
                    .Where(mp => mp.Uid == m.Id && availablePropertyIds.Contains(mp.PropertyId))
                    .Select(mp => mp.PropertyId)
                    .ToList()
            })
            .Select(result => new
            {
                result.MaterialSource,
                result.AvailableProps,
                result.LatestDate,
                LatestValues = result.LatestDate == null || result.LatestDate.Count == 0
                    ? emptyValueList
                    : _context.MaterialValues
                        .Where(mv => mv.Uid == result.MaterialSource.Id &&
                                     mv.CreatedOn == result.LatestDate.Max() &&
                                     latestValuePropertyIds.Contains(mv.PropertyId))
                        .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                        .ToList(),
                PreviousValues = result.LatestDate == null || result.LatestDate.Count < 2
                    ? emptyValueList
                    : _context.MaterialValues
                        .Where(mv => mv.Uid == result.MaterialSource.Id &&
                                     mv.CreatedOn == result.LatestDate.Min() &&
                                     latestValuePropertyIds.Contains(mv.PropertyId))
                        .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                        .ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (intermediate == null)
            throw new KeyNotFoundException($"Material with id {id} not found.");

        var latestAvgValue = intermediate.LatestValues.FirstOrDefault(v => v.PropertyId == 1).ValueDecimal;
        var previousAvgValue = intermediate.PreviousValues.FirstOrDefault(v => v.PropertyId == 1).ValueDecimal;

        string changePercent = "";
        if (latestAvgValue.HasValue && previousAvgValue.HasValue)
        {
            changePercent = ValueChangeFormatter.FormatValueChange(previousAvgValue.Value, latestAvgValue.Value);
        }

        DateOnly? latestDate = intermediate.LatestDate != null && intermediate.LatestDate.Count != 0
            ? intermediate.LatestDate.Max()
            : null;

        var response = new MaterialSourceResponseDto(
            intermediate.MaterialSource.Id,
            intermediate.MaterialSource.Material.Name,
            intermediate.MaterialSource.Source.Name,
            intermediate.MaterialSource.DeliveryType.Name,
            intermediate.MaterialSource.MaterialGroup.Name,
            intermediate.MaterialSource.TargetMarket,
            intermediate.MaterialSource.Unit.Name,
            latestDate,
            changePercent,
            intermediate.LatestValues.FirstOrDefault(v => v.PropertyId == 1).ValueDecimal,
            intermediate.LatestValues.FirstOrDefault(v => v.PropertyId == 2).ValueDecimal,
            intermediate.LatestValues.FirstOrDefault(v => v.PropertyId == 3).ValueDecimal,
            intermediate.LatestValues.FirstOrDefault(v => v.PropertyId == 6).ValueDecimal,
            intermediate.MaterialSource.Description,
            intermediate.MaterialSource.RoundTo,
            intermediate.AvailableProps
        );

        memoryCache.Set(cacheKey, response, TimeSpan.FromHours(2));
        return response;
    }
}

