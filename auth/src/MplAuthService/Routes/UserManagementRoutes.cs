using MplAuthService.Interfaces;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Routes
{
    public static class UserManagementRoutes
    {
        public static void MapUserManagementRoutes(this WebApplication app)
        {
            app.MapPost("/register", async (IUserService userService, CreateUserDto userDto) =>
            {
                try
                {
                    var user = await userService.CreateUser(userDto.Email, userDto.Password, userDto.Organization);
                    return Results.Ok(user);
                }
                catch (InvalidOperationException e)
                {
                    return Results.BadRequest(e.Message);
                }
            }).RequireAuthorization("AdminOnly");

        }
    }
}