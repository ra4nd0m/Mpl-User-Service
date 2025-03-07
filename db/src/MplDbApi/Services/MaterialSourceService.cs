using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MaterialService(BMplbaseContext _context) : IMaterialSource
{
    public async Task<List<MaterialSourceDto>> GetAllMaterials()
    {
        int[] propertyIds = { 1, 2, 3, 6 };
            var materialsList = await _context.MaterialSources
                .Include(m => m.Material)
                .Include(m => m.Source)
                .Include(m => m.DeliveryType)
                .Include(m => m.MaterialGroup)
                .Include(m => m.Unit)
                .Select(m => new MaterialSourceDto
                {
                    Id = m.Id,
                    MaterialName = m.Material.Name,
                    Source = m.Source.Name,
                    DeliveryType = m.DeliveryType.Name,
                    Group = m.MaterialGroup.Name,
                    Market = m.TargetMarket,
                    Unit = m.Unit.Name,
                    LastCreatedDate = _context.MaterialValues
                        .Where(mv => mv.Uid == m.Id && propertyIds.Contains(mv.PropertyId))
                        .OrderByDescending(mv => mv.CreatedOn)
                        .Select(mv => mv.CreatedOn)
                        .FirstOrDefault()
                }).ToListAsync();
            return materialsList;
    }

    public async Task<MaterialSourceDto?> GetMaterialById(int id)
    {
        var material = await _context.MaterialSources
            .Include(m => m.Material)
            .Include(m => m.Source)
            .Include(m => m.DeliveryType)
            .Include(m => m.MaterialGroup)
            .Include(m => m.Unit)
            .Where(m => m.Id == id)
            .Select(m => new MaterialSourceDto
            {
                Id = m.Id,
                MaterialName = m.Material.Name,
                Source = m.Source.Name,
                DeliveryType = m.DeliveryType.Name,
                Group = m.MaterialGroup.Name,
                Market = m.TargetMarket,
                Unit = m.Unit.Name,
            })
            .FirstOrDefaultAsync();

        return material ?? throw new KeyNotFoundException($"Material with id {id} not found.");
    }
}