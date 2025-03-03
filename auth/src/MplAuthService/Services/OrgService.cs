using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;

namespace MplAuthService.Services
{
    public class OrgService(AuthContext context) : IOrgService
    {
        public async Task<Organization?> GetOrganization(int id)
        {
            return await context.Organizations.FindAsync(id);
        }

        public async Task<List<Organization>> GetOrganizations()
        {
            return await context.Organizations.ToListAsync();
        }
    }
}