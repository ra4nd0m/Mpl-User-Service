using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using MplDbApi.Services;

public class MaterialSourceService(BMplbaseContext _context, FilterService filterService) : IMaterialSourceService
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
        var filter = await filterService.GetFilterByRole(role) ?? throw new InvalidOperationException("Filter not found for the specified role.");
        int[] latestValuePropertyIds = { 1, 2, 3, 6 };
        int[] availablePropertyIds = { 1, 2, 3, 4, 5, 6 };
        var emptyValueList = new List<PropertyValueInfo>();

        // Step 1: Query the database for intermediate data
        var intermediateResults = await _context.MaterialSources
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
                .Max(mv => (DateOnly?)mv.CreatedOn),
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
                LatestValues = result.LatestDate == null ?
                emptyValueList :
                _context.MaterialValues
                .Where(mv => mv.Uid == result.MaterialSource.Id &&
                         mv.CreatedOn == result.LatestDate.Value &&
                         latestValuePropertyIds.Contains(mv.PropertyId))
                // Select into the named struct
                .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                // Materialize the list of the struct
                .ToList()
            })
            .AsNoTracking()
            .ToListAsync(); // Execute the database query

        // Step 2: Perform the final projection in memory
        var materialsList = intermediateResults.Select(finalResult => new MaterialSourceResponseDto(
            finalResult.MaterialSource.Id,
            finalResult.MaterialSource.Material.Name,
            finalResult.MaterialSource.Source.Name,
            finalResult.MaterialSource.DeliveryType.Name,
            finalResult.MaterialSource.MaterialGroup.Name,
            finalResult.MaterialSource.TargetMarket,
            finalResult.MaterialSource.Unit.Name,
            finalResult.LatestDate,
            // These FirstOrDefault calls now operate on the in-memory list
            finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 1).ValueDecimal,
            finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 2).ValueDecimal,
            finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 3).ValueDecimal,
            finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 6).ValueDecimal,
            finalResult.AvailableProps
        )).ToList(); // Create the final list of DTOs

        return materialsList;
    }

    public async Task<List<MaterialSourceResponseDto>> GetMaterialsByGroup(int groupId, string role)
    {
        var filter = await filterService.GetFilterByRole(role) ?? throw new InvalidOperationException("Filter not found for the specified role.");
        int[] latestValuePropertyIds = { 1, 2, 3, 6 };
        int[] availablePropertyIds = { 1, 2, 3, 4, 5, 6 };
        var emptyValueList = new List<PropertyValueInfo>();

        var intermediateResults = await _context.MaterialSources
            .Include(m => m.Material)
            .Include(m => m.Source)
            .Include(m => m.DeliveryType)
            .Include(m => m.MaterialGroup)
            .Include(m => m.Unit)
            .Where(m => filter.MaterialIds == null || !filter.MaterialIds.Contains(m.Id)) // Exclude materials from filter
            .Where(m => filter.Groups == null || !filter.Groups.Contains(m.MaterialGroupId)) // Filter by groups
            .Where(m => filter.Sources == null || !filter.Sources.Contains(m.SourceId)) // Filter by sources
            .Where(m => filter.Units == null || !filter.Units.Contains(m.UnitId)) // Filter by units
            .Where(m => m.MaterialGroupId == groupId && m.Unit.Id != 5)
            .Select(m => new // Project to find the latest date first
            {
                MaterialSource = m,
                LatestDate = _context.MaterialValues
                    .Where(mv => mv.Uid == m.Id && latestValuePropertyIds.Contains(mv.PropertyId))
                    .Max(mv => (DateOnly?)mv.CreatedOn),
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
                LatestValues = result.LatestDate == null ?
                    emptyValueList :
                    _context.MaterialValues
                        .Where(mv => mv.Uid == result.MaterialSource.Id &&
                                     mv.CreatedOn == result.LatestDate.Value &&
                                     latestValuePropertyIds.Contains(mv.PropertyId))
                        // Select into the named struct
                        .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                        // Materialize the list of the struct
                        .ToList()
            })
            .AsNoTracking()
            .ToListAsync(); // Execute the database query

        // Step 2: Perform the final projection in memory
        var materialsList = intermediateResults.Select(finalResult => new MaterialSourceResponseDto(
            finalResult.MaterialSource.Id,
            finalResult.MaterialSource.Material.Name,
            finalResult.MaterialSource.Source.Name,
            finalResult.MaterialSource.DeliveryType.Name,
            finalResult.MaterialSource.MaterialGroup.Name,
            finalResult.MaterialSource.TargetMarket,
            finalResult.MaterialSource.Unit.Name,
            finalResult.LatestDate,
            finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 1).ValueDecimal,
            finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 2).ValueDecimal,
            finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 3).ValueDecimal,
            finalResult.LatestValues.FirstOrDefault(mv => mv.PropertyId == 6).ValueDecimal,
            finalResult.AvailableProps
        )).ToList(); // Create the final list of DTOs

        return materialsList;
        //
    }

    public async Task<MaterialSourceResponseDto?> GetMaterialById(int id, string role)
    {
        int[] propertyIds = { 1, 2, 3, 6 };
        int[] availablePropertyIds = { 1, 2, 3, 4, 5, 6 };
        var emptyValueList = new List<PropertyValueInfo>();

        // Step 1: Fetch the basic material information
        var materialQuery = await _context.MaterialSources
            .Include(m => m.Material)
            .Include(m => m.Source)
            .Include(m => m.DeliveryType)
            .Include(m => m.MaterialGroup)
            .Include(m => m.Unit)
            .Where(m => m.Id == id && m.Unit.Id != 5)
            .Select(m => new
            {
                MaterialSource = m,
                LatestDate = _context.MaterialValues
                    .Where(mv => mv.Uid == m.Id && propertyIds.Contains(mv.PropertyId))
                    .Max(mv => (DateOnly?)mv.CreatedOn),
                AvailableProps = _context.MaterialProperties
                    .Where(mp => mp.Uid == m.Id && availablePropertyIds.Contains(mp.PropertyId))
                    .Select(mp => mp.PropertyId)
                    .ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (materialQuery == null)
            throw new KeyNotFoundException($"Material with id {id} not found.");

        // Step 2: Fetch the latest values if we have a date
        var latestValues = new List<PropertyValueInfo>();
        if (materialQuery.LatestDate.HasValue)
        {
            latestValues = await _context.MaterialValues
                .Where(mv => mv.Uid == materialQuery.MaterialSource.Id &&
                          mv.CreatedOn == materialQuery.LatestDate.Value &&
                          propertyIds.Contains(mv.PropertyId))
                .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                .ToListAsync();
        }

        // Step 3: Create the DTO with in-memory operations
        return new MaterialSourceResponseDto(
            materialQuery.MaterialSource.Id,
            materialQuery.MaterialSource.Material.Name,
            materialQuery.MaterialSource.Source.Name,
            materialQuery.MaterialSource.DeliveryType.Name,
            materialQuery.MaterialSource.MaterialGroup.Name,
            materialQuery.MaterialSource.TargetMarket,
            materialQuery.MaterialSource.Unit.Name,
            materialQuery.LatestDate,
            latestValues.FirstOrDefault(mv => mv.PropertyId == 1).ValueDecimal,
            latestValues.FirstOrDefault(mv => mv.PropertyId == 2).ValueDecimal,
            latestValues.FirstOrDefault(mv => mv.PropertyId == 3).ValueDecimal,
            latestValues.FirstOrDefault(mv => mv.PropertyId == 6).ValueDecimal,
            materialQuery.AvailableProps
        );
    }
}
