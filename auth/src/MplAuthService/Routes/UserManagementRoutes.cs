using Microsoft.AspNetCore.Identity;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Routes
{
    public static class UserManagementRoutes
    {
        public static void MapUserManagementRoutes(this WebApplication app)
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("UserManagementRoutes");

            app.MapPost("/register", async (IUserService userService, CreateUserDto userDto) =>
            {
                try
                {
                    logger.LogInformation("Creating user with email {Email}", userDto.Email);
                    var user = await userService.CreateUser(userDto);
                    UserResponseDto result;
                    if (user.Organization != null)
                    {
                        var organizationDto = new OrganizationDto(user.Organization.Name, user.Organization.Inn,
                            user.Organization.SubscriptionType, user.Organization.SubscriptionStartDate,
                            user.Organization.SubscriptionEndDate);
                        result = new UserResponseDto(user.Id, user.Email!, organizationDto, null, user.CanExportData);
                    }
                    else
                    {
                        result = new UserResponseDto(user.Id, user.Email!, null, null, user.CanExportData);
                    }
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create user with email {Email}", userDto.Email);
                    return Results.BadRequest();
                }
            }).RequireAuthorization("AdminOnly");

            app.MapPut("/users/{email}", async (string email, UpdateUserDto updateUser, IUserService userService) =>
            {
                try
                {
                    var user = await userService.GetUserByEmail(email);
                    if (user == null)
                    {
                        return Results.NotFound();
                    }
                    var result = await userService.UpdateUser(user, updateUser);
                    UserResponseDto resp;
                    if (result.Organization != null)
                    {
                        var organizationDto = new OrganizationDto(result.Organization.Name, result.Organization.Inn,
                            result.Organization.SubscriptionType, result.Organization.SubscriptionStartDate,
                            result.Organization.SubscriptionEndDate, result.Organization.Id);
                        resp = new UserResponseDto(result.Id, result.Email!, organizationDto, null, result.CanExportData);
                    }
                    else
                    {
                        if (result.IndividualSubscription != null)
                        {
                            var subscriptionDto = new SubscriptionDataDto(
                                result.IndividualSubscription.SubscriptionType,
                                result.IndividualSubscription.SubscriptionStartDate,
                                result.IndividualSubscription.SubscriptionEndDate
                            );
                            resp = new UserResponseDto(result.Id, result.Email!, null, subscriptionDto, result.CanExportData);
                        }
                        else
                            resp = new UserResponseDto(result.Id, result.Email!, null, null, result.CanExportData);
                    }
                    return Results.Ok(resp);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to update user with email {Email}", email);
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
                            u.Organization.SubscriptionEndDate, u.Organization.Id);
                        result = new UserResponseDto(u.Id, u.Email!, organizationDto, null, u.CanExportData);
                    }
                    else
                    {
                        if (u.IndividualSubscription != null)
                        {
                            var subscriptionDto = new SubscriptionDataDto(
                                u.IndividualSubscription.SubscriptionType,
                                u.IndividualSubscription.SubscriptionStartDate,
                                u.IndividualSubscription.SubscriptionEndDate
                            );
                            result = new UserResponseDto(u.Id, u.Email!, null, subscriptionDto, u.CanExportData);
                        }
                        else
                            result = new UserResponseDto(u.Id, u.Email!, null, null, u.CanExportData);
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
                        user.Organization.SubscriptionEndDate, user.Organization.Id);
                    result = new UserResponseDto(user.Id, user.Email!, organizationDto, null, user.CanExportData);
                }
                else
                {
                    result = new UserResponseDto(user.Id, user.Email!, null, null, user.CanExportData);
                }
                return Results.Ok(result);
            }).RequireAuthorization("AdminOnly");

            app.MapDelete("/users/{email}", async (IUserService service, UserManager<User> manager,
                string email) =>
            {
                try
                {
                    logger.LogInformation("Deleting user with email {Email}", email);
                    var user = await manager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        logger.LogWarning("User with email {Email} not found", email);
                        return Results.NotFound("User not found");
                    }
                    await service.DeleteUser(user);
                    logger.LogInformation("User with email {Email} deleted", email);
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete user with email {Email}", email);
                    return Results.BadRequest();
                }
            }).RequireAuthorization("AdminOnly");
        }
    }
}