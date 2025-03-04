using System.Security.Claims;
using MplUserService.Models;

namespace MplUserService.Interfaces
{
    public interface IUserService
    {
        Task<User> GetOrCreateUserAsync(ClaimsPrincipal claims);
    }
}