using System.Security.Claims;
using System.Threading.Tasks;
using MplUserService.Models;

namespace MplUserService.Extensions
{
    public static class MappingExtensions
    {
        public static User ClaimsPrincipalToUser(this ClaimsPrincipal claims)
        {
            var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("User ID not found in claims");

            return new User
            {
                Id = userId,
            };
        }
    }
}