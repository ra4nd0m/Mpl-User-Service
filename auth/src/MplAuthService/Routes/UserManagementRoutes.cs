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
                    UserResponseDto result;
                    if (user.Organization != null)
                    {
                        var organizationDto = new OrganizationDto(user.Organization.Name, user.Organization.Inn,
                            user.Organization.SubscriptionType, user.Organization.SubscriptionStartDate,
                            user.Organization.SubscriptionEndDate);
                        result = new UserResponseDto(user.Id, user.Email!, organizationDto);
                    }
                    else
                    {
                        result = new UserResponseDto(user.Id, user.Email!, null);
                    }
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create user with email {Email}", userDto.Email);
                    return Results.BadRequest();
                }
            }).RequireAuthorization("AdminOnly");

            app.MapGet("/users", async (IUserService userService) =>
            {
                var users = await userService.GetUsers();
                UserResponseDto result;
                return Results.Ok(users.Select(u =>
                {
                    if (u.Organization != null)
                    {
                        var organizationDto = new OrganizationDto(u.Organization.Name, u.Organization.Inn,
                            u.Organization.SubscriptionType, u.Organization.SubscriptionStartDate,
                            u.Organization.SubscriptionEndDate);
                        result = new UserResponseDto(u.Id, u.Email!, organizationDto);
                    }
                    else
                    {
                        result = new UserResponseDto(u.Id, u.Email!, null);
                    }
                    return result;
                }));
            }).RequireAuthorization("AdminOnly");

            app.MapGet("/users/{email}", async (IUserService userService, string email) =>
            {
                var user = await userService.GetUserByEmail(email);
                if (user == null)
                {
                    return Results.NotFound();
                }
                UserResponseDto result;
                if (user.Organization != null)
                {
                    var organizationDto = new OrganizationDto(user.Organization.Name, user.Organization.Inn,
                        user.Organization.SubscriptionType, user.Organization.SubscriptionStartDate,
                        user.Organization.SubscriptionEndDate);
                    result = new UserResponseDto(user.Id, user.Email!, organizationDto);
                }
                else
                {
                    result = new UserResponseDto(user.Id, user.Email!, null);
                }
                return Results.Ok(result);
            }).RequireAuthorization("AdminOnly");
        }
    }
}