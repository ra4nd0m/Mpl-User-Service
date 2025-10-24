using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Services
{
    public class OrgService(AuthContext context, ILogger<OrgService> logger) : IOrgService
    {
        public async Task<Organization?> GetOrganization(string inn)
        {
            logger.LogInformation("Getting organization with INN {Inn}", inn);
            return await context.Organizations.FirstOrDefaultAsync(o => o.Inn == inn);
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

        public async Task<OrganizationDto?> UpdateOrganization(string inn, OrganizationDto orgDto)
        {
            logger.LogInformation("Updating organization with INN {Inn}", inn);

            var organization = await context.Organizations.FirstOrDefaultAsync(o => o.Inn == inn);
            if (organization == null)
            {
                logger.LogWarning("Organization with INN {Inn} not found", inn);
                return null;
            }

            organization.Name = orgDto.Name;
            organization.SubscriptionType = orgDto.SubscriptionType;
            organization.SubscriptionStartDate = orgDto.SubscriptionStartDate.ToUniversalTime();
            organization.SubscriptionEndDate = orgDto.SubscriptionEndDate.ToUniversalTime();

            context.Organizations.Update(organization);
            await context.SaveChangesAsync();
            var result = new OrganizationDto(
                organization.Name,
                organization.Inn,
                organization.SubscriptionType,
                organization.SubscriptionStartDate,
                organization.SubscriptionEndDate);
            return result;
        }
    }
}