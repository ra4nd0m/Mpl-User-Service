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

            string? email = claims.FindFirstValue(ClaimTypes.Email) ??
                           claims.FindFirstValue("email") ??
                           claims.Claims.FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value;

            if (string.IsNullOrEmpty(email))
            {
                if (claims.IsInRole("Admin") || claims.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
                {
                    email = $"{userId}@admin.com";
                }
                else
                {
                    throw new InvalidOperationException("Email not found in claims");
                }
            }
            return new User
            {
                Id = userId,
                Email = email
            };
        }
    }
}