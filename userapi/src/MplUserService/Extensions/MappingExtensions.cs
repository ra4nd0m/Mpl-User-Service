using System.Security.Claims;
using MplUserService.Models;

namespace MplUserService.Extensions
{
    public static class MappingExtensions
    {
        public static User ClaimsPrincipalToUser(this ClaimsPrincipal claims)
        {
            return new User
            {
                Id = claims.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    throw new InvalidOperationException("User ID not found in claims"),
                Email = claims.FindFirstValue(ClaimTypes.Email) ??
                    throw new InvalidOperationException("User email not found in claims")
            };
        }
    }
}