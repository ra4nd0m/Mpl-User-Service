using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace MplUserService.Auth
{
    public class InternalRequirement(string requiredRole) : IAuthorizationRequirement
    {
        public string RequiredRole { get; } = requiredRole;
    }
    public class InternalRequirementHandler : AuthorizationHandler<InternalRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, InternalRequirement requirement)
        {
            if (context.User.IsInRole(requirement.RequiredRole) || context.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == requirement.RequiredRole))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, "User does not have required role"));
            }
            return Task.CompletedTask;
        }
    }
}