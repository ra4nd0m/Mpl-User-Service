using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Services;

public class MaterialGroupService(BMplbaseContext context, FilterService filterService) : IMaterialGroupService
{
    public async Task<IEnumerable<MaterialGroupDto>> GetMaterialGroupAsync(string role)
    {
        var filter = await filterService.GetFilterByRole(role);
        return await context.MaterialGroups
            .Where(mg => filter.Groups == null || !filter.Groups.Contains(mg.Id))
            .Select(dt => new MaterialGroupDto(dt.Id, dt.Name))
            .ToListAsync();
    }
}
