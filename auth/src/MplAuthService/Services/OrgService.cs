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
                SubscriptionStartDate = DateTime.SpecifyKind(orgDto.SubscriptionStartDate, DateTimeKind.Utc),
                SubscriptionEndDate = DateTime.SpecifyKind(orgDto.SubscriptionEndDate, DateTimeKind.Utc)
            };
            context.Organizations.Add(organization);
            await context.SaveChangesAsync();
            return organization;
        }

        public async Task<OrganizationDto?> UpdateOrganization(int id, OrganizationDto orgDto)
        {
            logger.LogInformation("Updating organization with id {Id}", id);

            var organization = await context.Organizations.FirstOrDefaultAsync(o => o.Id == id);
            if (organization == null)
            {
                logger.LogWarning("Organization with id {Id} not found", id);
                return null;
            }

            organization.Name = orgDto.Name;
            organization.Inn = orgDto.Inn;
            organization.SubscriptionType = orgDto.SubscriptionType;
            organization.SubscriptionStartDate = DateTime.SpecifyKind(orgDto.SubscriptionStartDate, DateTimeKind.Utc);
            organization.SubscriptionEndDate = DateTime.SpecifyKind(orgDto.SubscriptionEndDate, DateTimeKind.Utc);

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
        public async Task<bool> DeleteOrganization(int id)
        {
            logger.LogInformation("Deleting organization with id {Id}", id);

            var organization = await context.Organizations.FirstOrDefaultAsync(o => o.Id == id);
            if (organization == null)
            {
                logger.LogWarning("Organization with id {Id} not found", id);
                return false;
            }

            context.Organizations.Remove(organization);
            await context.SaveChangesAsync();
            return true;
        }
        public async Task<List<UserResponseDto>> GetUsersByOrganization(int orgId)
        {
            logger.LogInformation("Getting users for organization with id {OrgId}", orgId);

            var users = await context.Users
                .Where(u => u.OrganizationId == orgId)
                .Include(u => u.Organization)
                .Select(u => new UserResponseDto(
                    u.Id,
                    u.Email!,
                    u.Organization != null ? new OrganizationDto(
                        u.Organization.Name,
                        u.Organization.Inn,
                        u.Organization.SubscriptionType,
                        u.Organization.SubscriptionStartDate,
                        u.Organization.SubscriptionEndDate,
                        u.Organization.Id
                    ) : null
                ))
                .ToListAsync();

            return users;
        }
    }
}
