using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;

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

        public async Task<Organization> CreateOrganization(OrganizationDto orgDto)
        {
            logger.LogInformation("Creating new organization with name {Name}", orgDto.Name);

            var existingOrg = await context.Organizations.FirstOrDefaultAsync(o => o.Inn == orgDto.Inn);
            if (existingOrg != null)
            {
                throw new InvalidOperationException($"Organization with INN {orgDto.Inn} already exists");
            }
            
            var organization = new Organization
            {
                Name = orgDto.Name,
                Inn = orgDto.Inn,
                SubscriptionType = orgDto.SubscriptionType,
                SubscriptionStartDate = orgDto.SubscriptionStartDate.ToUniversalTime(),
                SubscriptionEndDate = orgDto.SubscriptionEndDate.ToUniversalTime()
            };
            context.Organizations.Add(organization);
            await context.SaveChangesAsync();
            return organization;
        }
    }
}