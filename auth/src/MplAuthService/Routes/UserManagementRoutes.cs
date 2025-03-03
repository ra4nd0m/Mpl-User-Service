using MplAuthService.Interfaces;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Routes
{
    public static class UserManagementRoutes
    {
        public static void MapUserManagementRoutes(this WebApplication app)
        {
            app.MapPost("/register", async (IUserService userService, CreateUserDto userDto, ILogger<Program> logger) =>
            {
                try
                {
                    logger.LogInformation("Creating user with email {Email}", userDto.Email);
                    var user = await userService.CreateUser(userDto.Email, userDto.Password, userDto.Organization);
                    return Results.Ok(new UserResponseDto(user.Id, user.Email!, user.OrganizationId));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create user with email {Email}", userDto.Email);
                    return Results.BadRequest();
                }
            }).RequireAuthorization("AdminOnly");

        }
    }
}