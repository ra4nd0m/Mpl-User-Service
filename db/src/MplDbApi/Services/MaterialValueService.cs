using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MaterialValueService(BMplbaseContext _context) : IMaterialValueService
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
}
