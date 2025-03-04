using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MplUserService.Data;
using MplUserService.Extensions;
using MplUserService.Interfaces;
using MplUserService.Models;

namespace MplUserService.Services
{
    public class UserService(UserContext context) : IUserService
    {
        public async Task<User> GetOrCreateUserAsync(ClaimsPrincipal claims)
        {
            var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                throw new UnauthorizedAccessException("User ID not found in claims");
            }
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                user = claims.ClaimsPrincipalToUser();
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }
            return user;
        }
    }
}
