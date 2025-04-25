using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MaterialSourceService(BMplbaseContext _context) : IMaterialSourceService
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
    public async Task<List<MaterialSourceResponseDto>> GetAllMaterials()
    {
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

    public async Task<List<MaterialSourceResponseDto>> GetMaterialsByGroup(int groupId)
    {
        int[] latestValuePropertyIds = { 1, 2, 3, 6 };
        int[] availablePropertyIds = { 1, 2, 3, 4, 5, 6 };
        var emptyValueList = new List<PropertyValueInfo>();

        var intermediateResults = await _context.MaterialSources
            .Include(m => m.Material)
            .Include(m => m.Source)
            .Include(m => m.DeliveryType)
            .Include(m => m.MaterialGroup)
            .Include(m => m.Unit)
            .Where(m => m.MaterialGroupId == groupId)
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

    public async Task<MaterialSourceResponseDto?> GetMaterialById(int id)
    {
        int[] propertyIds = { 1, 2, 3, 6 };
        int[] availablePropertyIds = { 1, 2, 3, 4, 5, 6 };

        var emptyValueList = new List<PropertyValueInfo>();
        var material = await _context.MaterialSources
           .Include(m => m.Material)
           .Include(m => m.Source)
           .Include(m => m.DeliveryType)
           .Include(m => m.MaterialGroup)
           .Include(m => m.Unit)
           .Where(m => m.Id == id)
           .Select(m => new // Project to find the latest date first
           {
               MaterialSource = m,
               LatestDate = _context.MaterialValues
                   .Where(mv => mv.Uid == m.Id && propertyIds.Contains(mv.PropertyId))
                   .Max(mv => (DateOnly?)mv.CreatedOn), // Find the single latest date for any relevant property
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

               LatestValues = result.LatestDate == null ?
                   emptyValueList :
                   _context.MaterialValues
                       .Where(mv => mv.Uid == result.MaterialSource.Id &&
                           mv.CreatedOn == result.LatestDate.Value &&
                           propertyIds.Contains(mv.PropertyId))
                       .Select(mv => new PropertyValueInfo(mv.PropertyId, mv.ValueDecimal))
                       .ToList()
           })
           .Select(finalResult => new MaterialSourceResponseDto(
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
           ))
           .AsNoTracking().FirstOrDefaultAsync();

        return material ?? throw new KeyNotFoundException($"Material with id {id} not found.");
    }
}
