using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace MplUserService.Auth
{
    /// <summary>
    /// Represents an authorization requirement that requires a specific role.
    /// </summary>
    /// <param name="requiredRole">The role required for authorization.</param>
    /// <remarks>
    /// This class implements the <see cref="IAuthorizationRequirement"/> interface to define
    /// a requirement that checks if the user has a specific role.
    /// </remarks>
    public class RoleRequirement(string requiredRole) : IAuthorizationRequirement
    {
        public string RequiredRole { get; } = requiredRole;
    }
    public class RoleRequirementHandler : AuthorizationHandler<RoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
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