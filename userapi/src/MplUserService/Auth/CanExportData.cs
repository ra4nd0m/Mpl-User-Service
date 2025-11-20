using Microsoft.AspNetCore.Authorization;

namespace MplUserService.Auth
{
    public class CanExportDataRequirement : IAuthorizationRequirement
    {
    }

    public class CanExportDataHandler : AuthorizationHandler<CanExportDataRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanExportDataRequirement requirement)
        {
            var canExportDataClaim = context.User.FindFirst("CanExportData");

            if (canExportDataClaim != null && bool.TryParse(canExportDataClaim.Value, out var canExport) && canExport)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, "User does not have permission to export data"));
            }

            return Task.CompletedTask;
        }
    }
}