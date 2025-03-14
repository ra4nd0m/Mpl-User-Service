using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MaterialSourceService(BMplbaseContext _context) : IMaterialSourceService
{
    public async Task<List<MaterialSourceResponseDto>> GetAllMaterials()
    {
        int[] propertyIds = { 1, 2, 3, 6 };
        var materialsList = await _context.MaterialSources
            .Include(m => m.Material)
            .Include(m => m.Source)
            .Include(m => m.DeliveryType)
            .Include(m => m.MaterialGroup)
            .Include(m => m.Unit)
            .Select(m => new MaterialSourceResponseDto(
                m.Id,
                m.Material.Name,
                m.Source.Name,
                m.DeliveryType.Name,
                m.MaterialGroup.Name,
                m.TargetMarket,
                m.Unit.Name,
                _context.MaterialValues
                    .Where(mv => mv.Uid == m.Id && propertyIds.Contains(mv.PropertyId))
                    .OrderByDescending(mv => mv.CreatedOn)
                    .Select(mv => mv.CreatedOn)
                    .FirstOrDefault()
            )).AsNoTracking().ToListAsync();

        return materialsList;
    }

    public async Task<MaterialSourceResponseDto?> GetMaterialById(int id)
    {
        var material = await _context.MaterialSources
            .Include(m => m.Material)
            .Include(m => m.Source)
            .Include(m => m.DeliveryType)
            .Include(m => m.MaterialGroup)
            .Include(m => m.Unit)
            .Where(m => m.Id == id)
            .Select(m => new MaterialSourceResponseDto(
                m.Id,
                m.Material.Name,
                m.Source.Name,
                m.DeliveryType.Name,
                m.MaterialGroup.Name,
                m.TargetMarket,
                m.Unit.Name,
                null
            )).AsNoTracking().FirstOrDefaultAsync();

        return material ?? throw new KeyNotFoundException($"Material with id {id} not found.");
    }
}
