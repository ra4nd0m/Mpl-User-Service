using MplDbApi.Interfaces;
using MplDbApi.Data;
using MplDbApi.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MplDbApi.Services
{
    public class SourceService(BMplbaseContext context) : ISourceService
    {
        public async Task<IEnumerable<IdValuePair>> GetSources()
        {
            var sources = await context.Sources
                .Select(s => new IdValuePair(s.Id, s.Name))
                .ToListAsync();
            
            return sources;
        }
    }
}