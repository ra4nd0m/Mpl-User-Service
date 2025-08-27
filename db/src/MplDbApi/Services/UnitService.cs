using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Interfaces;
using MplDbApi.Models.Dtos;

namespace MplDbApi.Services
{
    public class UnitService(BMplbaseContext context) : IUnitService
    {
        public async Task<IEnumerable<IdValuePair>> GetUnits()
        {
            var units = await context.Units
                .Select(u => new IdValuePair(u.Id, u.Name))
                .ToListAsync();
                
            return units;
        }
    }
}