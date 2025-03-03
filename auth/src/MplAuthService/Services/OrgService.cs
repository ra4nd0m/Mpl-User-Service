using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;

namespace MplAuthService.Services
{
    public class OrgService(AuthContext context, ILogger<OrgService> logger) : IOrgService
    {
        public async Task<Organization?> GetOrganization(int id)
        {
            logger.LogInformation("Getting organization with id {Id}", id);
            return await context.Organizations.FindAsync(id);
        }

        public async Task<List<Organization>> GetOrganizations()
        {
            logger.LogInformation("Getting all organizations");
            return await context.Organizations.ToListAsync();
        }
    }
}