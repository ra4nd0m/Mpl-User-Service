using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Services
{
    public class MaterialPropService(BMplbaseContext context, ILogger<MaterialPropService> logger) : IMaterialPropService
    {
        public async Task<List<MaterialPropertyResp>> GetMaterialProperties(int materialTd)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GetMaterialProperties");
                throw;

            }
        }
        public async Task<List<IdValuePair>> GetMaterialPropertiesForDropdown()
        {
            try
            {
                var props = await context.Properties
                    .Select(mp => new IdValuePair(
                        mp.Id,
                        mp.Name
                    ))
                    .AsNoTracking()
                    .ToListAsync();
                return props;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GetMaterialPropertiesForDropdown");
                throw;
            }
        }
    }
}