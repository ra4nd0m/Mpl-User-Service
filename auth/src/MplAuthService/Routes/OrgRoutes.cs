using MplAuthService.Interfaces;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Routes
{
    public static class OrgRoutes
    {
        public static void MapOrgRoutes(this WebApplication app)
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("OrgRoutes");

            app.MapGet("/organizations", async (IOrgService orgService) =>
            {
                var orgs = await orgService.GetOrganizations();
                return Results.Ok(orgs.Select(o => new OrganizationDto(
                    o.Name,
                    o.Inn,
                    o.SubscriptionType,
                    o.SubscriptionStartDate,
                    o.SubscriptionEndDate,
                    o.Id
                )));
            }).RequireAuthorization("AdminOnly");

            app.MapGet("/organizations/{inn}", async (IOrgService orgService, string inn) =>
            {
                var org = await orgService.GetOrganization(inn);
                if (org == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(new OrganizationDto(
                    org.Name,
                    org.Inn,
                    org.SubscriptionType,
                    org.SubscriptionStartDate,
                    org.SubscriptionEndDate,
                    org.Id
                ));
            }).RequireAuthorization("AdminOnly");

            app.MapPut("/organizations/{id}", async (IOrgService orgService, int id, OrganizationDto orgDto) =>
            {
                try
                {
                    var result = await orgService.UpdateOrganization(id, orgDto);
                    if (result == null)
                    {
                        return Results.NotFound();
                    }
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to update organization with id {Id}", id);
                    return Results.BadRequest();
                }
            }).RequireAuthorization("AdminOnly");

            app.MapPost("/organizations", async (IOrgService orgService, OrganizationDto orgDto) =>
            {
                try
                {
                    var result = await orgService.CreateOrganization(orgDto);
                    return Results.Ok(new OrganizationDto(
                        result.Name,
                        result.Inn,
                        result.SubscriptionType,
                        result.SubscriptionStartDate,
                        result.SubscriptionEndDate,
                        result.Id
                    ));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create organization with name {Name}", orgDto.Name);
                    return Results.BadRequest();
                }
            }).RequireAuthorization("AdminOnly");

            app.MapDelete("/organizations/{id}", async (IOrgService orgService, int id) =>
            {
                try
                {
                    var success = await orgService.DeleteOrganization(id);
                    if (!success)
                    {
                        return Results.NotFound();
                    }
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete organization with id {Id}", id);
                    return Results.BadRequest();
                }
            }).RequireAuthorization("AdminOnly");

        }
    }
}