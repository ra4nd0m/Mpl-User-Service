using MplAuthService.Interfaces;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Routes
{
    public static class OrgRoutes
    {
        public static void MapOrgRoutes(this WebApplication app)
        {
            app.MapGet("/organizations", async (IOrgService orgService) =>
            {
                var orgs = await orgService.GetOrganizations();
                return Results.Ok(orgs.Select(o => new OrganizationDto(
                    o.Name,
                    o.Inn,
                    o.SubscriptionType,
                    o.SubscriptionStartDate,
                    o.SubscriptionEndDate
                )));
            }).RequireAuthorization("AdminOnly");

            app.MapGet("/organizations/{id}", async (IOrgService orgService, int id) =>
            {
                var org = await orgService.GetOrganization(id);
                if (org == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(new OrganizationDto(
                    org.Name,
                    org.Inn,
                    org.SubscriptionType,
                    org.SubscriptionStartDate,
                    org.SubscriptionEndDate
                ));
            }).RequireAuthorization("AdminOnly");

            app.MapPost("/organizations", async (IOrgService orgService, OrganizationDto orgDto, ILogger<Program> logger) =>
            {
                try
                {
                    var result = await orgService.CreateOrganization(orgDto);
                    return Results.Ok(new OrganizationDto(
                        result.Name,
                        result.Inn,
                        result.SubscriptionType,
                        result.SubscriptionStartDate,
                        result.SubscriptionEndDate
                    ));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create organization with name {Name}", orgDto.Name);
                    return Results.BadRequest();
                }
            }).RequireAuthorization("AdminOnly");

        }
    }
}