using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Services;

public class MaterialGroupService : IMaterialGroupService
{
    private readonly BMplbaseContext _context;

    public MaterialGroupService(BMplbaseContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MaterialGroupDto>> GetMaterialGroupAsync()
    {
        return await _context.MaterialGroups
            .Select(dt => new MaterialGroupDto(dt.Id, dt.Name))
            .ToListAsync();
    }
}
