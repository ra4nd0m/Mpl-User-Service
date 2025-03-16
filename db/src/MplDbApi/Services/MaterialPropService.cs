using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Services
{
    public class MaterialPropService(BMplbaseContext context) : IMaterialPropService
    {
        public async Task<List<MaterialPropertyResp>> GetMaterialProperties(int materialTd)
        {
            var props = await context.MaterialProperties
                .Include(mp => mp.Property)
                .Where(mp => mp.Uid == materialTd)
                .Select(mp => new MaterialPropertyResp(
                    mp.PropertyId,
                    mp.Property.Name
                ))
                .AsNoTracking()
                .ToListAsync();
            return props;
        }
    }
}