using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MplUserService.Models.Enums;

namespace MplUserService.Auth
{
    public class SubscriptionRequirement : IAuthorizationRequirement
    {
        public SubscriptionType MinimumSubscriptionType { get; }

        public SubscriptionRequirement(SubscriptionType minimumSubscriptionType)
        {
            MinimumSubscriptionType = minimumSubscriptionType;
        }
    }
    public class SubscriptionHandler : AuthorizationHandler<SubscriptionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SubscriptionRequirement requirement)
        {
            if (context.User.IsInRole("Admin") || context.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            
            var subscriptionClaim = context.User.FindFirst("SubscriptionType");
            var subscriptionEndClaim = context.User.FindFirst("SubscriptionEnd");

            if (subscriptionClaim == null || subscriptionEndClaim == null)
            {
                context.Fail(new AuthorizationFailureReason(this, "Subscription information not found"));
                return Task.CompletedTask;
            }
            if (DateTime.Parse(subscriptionEndClaim.Value) < DateTime.UtcNow)
            {
                context.Fail(new AuthorizationFailureReason(this, "Subscription expired"));
                return Task.CompletedTask;
            }
            var userSubscription = Enum.Parse<SubscriptionType>(subscriptionClaim.Value);
            if (userSubscription >= requirement.MinimumSubscriptionType)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, "Insufficient subscription level"));
            }
            return Task.CompletedTask;
        }
    }
}