using MplAuthService.Models;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserByEmail(string email);
        Task<List<User>> GetUsers();
        Task<User> CreateUser(string email, string password, OrganizationDto organization);
        Task<User> CreateAdmin(string email, string password);
        Task<User> UpdateUser(User user, UpdateUserDto updateUser);
        Task DeleteUser(User user);
    }
}