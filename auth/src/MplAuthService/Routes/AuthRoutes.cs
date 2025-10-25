using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;
using MplAuthService.Utils;

namespace MplAuthService.Routes
{
    public static class AuthRoutes
    {
        public static void MapAuthRoutes(this WebApplication app)
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("AuthRoutes");

            app.MapPost("/login", async (IJwtService jwtService, IRefreshTokenService refreshTokenService,
            UserManager<User> userManager, LoginDto loginDto, HttpContext context, AuthContext dbContext) =>
            {
                try
                {
                    logger.LogInformation("Logging in user with email {Email}", EmailObfuscator.ObfuscateEmail(loginDto.Email));
                    var user = await userManager.FindByEmailAsync(loginDto.Email);
                    if (user == null || !await userManager.CheckPasswordAsync(user, loginDto.Password))
                    {
                        logger.LogWarning("Failed to login user with email {Email}", loginDto.Email);
                        return Results.BadRequest("Invalid email or password");
                    }

                    user = await dbContext.Users
                        .Include(u => u.Organization)
                        .Include(u => u.IndividualSubscription)
                        .FirstOrDefaultAsync(u => u.Id == user.Id);

                    var roles = await userManager.GetRolesAsync(user!);
                    if (!roles.Contains("Admin"))
                    {
                        DateTime? subscriptionEnd = null;

                        if (user!.Organization != null)
                        {
                            subscriptionEnd = user.Organization.SubscriptionEndDate;
                        } 
                        else if (user.IndividualSubscription != null)
                        {
                            subscriptionEnd = user.IndividualSubscription.SubscriptionEndDate;
                        }
                        if (subscriptionEnd.HasValue && subscriptionEnd.Value < DateTime.UtcNow)
                        {
                            logger.LogWarning("User {Email} organization subscription has expired", EmailObfuscator.ObfuscateEmail(loginDto.Email));
                            return Results.Problem(
                            detail: "Your subscription has expired. Please contact support to renew.",
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Subscription Expired");
                        }
                    }

                    logger.LogInformation("User {Email} logged in", EmailObfuscator.ObfuscateEmail(loginDto.Email));
                    string token = await jwtService.GenerateJwtToken(user!);
                    RefreshToken refreshToken = await refreshTokenService.GenerateRefreshToken(user!);
                    context.Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Strict,
                        Secure = true,
                        Expires = refreshToken.Expires,
                    });
                    return Results.Ok(new { Token = token });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to login user with email {Email}", EmailObfuscator.ObfuscateEmail(loginDto.Email));
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                }
            });

            app.MapPost("/refresh", async (IJwtService jwtService, IRefreshTokenService refreshTokenService,
            UserManager<User> userManager, HttpContext context, AuthContext authContext) =>
            {
                try
                {
                    logger.LogInformation("Token refresh attempt");
                    if (context.Request.Cookies.TryGetValue("refreshToken", out string? token))
                    {
                        var refreshToken = await refreshTokenService.ValidateRefreshToken(token);
                        if (refreshToken != null)
                        {
                            var user = await userManager.FindByIdAsync(refreshToken.UserId);
                            if (user != null)
                            {
                                user = await authContext.Users
                                    .Include(u => u.Organization)
                                    .Include(u => u.IndividualSubscription)
                                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                                var roles = await userManager.GetRolesAsync(user!);
                                if (!roles.Contains("Admin"))
                                {
                                    DateTime? subscriptionEnd = null;
                                    if (user!.Organization != null)
                                    {
                                        subscriptionEnd = user.Organization.SubscriptionEndDate;
                                    }
                                    else if (user.IndividualSubscription != null)
                                    {
                                        subscriptionEnd = user.IndividualSubscription.SubscriptionEndDate;
                                    }
                                    if (subscriptionEnd.HasValue && subscriptionEnd.Value < DateTime.UtcNow)
                                    {
                                        logger.LogWarning("User {Email} organization subscription has expired during token refresh", EmailObfuscator.ObfuscateEmail(user.Email!));
                                        return Results.Problem(
                                        detail: "Your subscription has expired. Please contact support to renew.",
                                        statusCode: StatusCodes.Status403Forbidden,
                                        title: "Subscription Expired");
                                    }
                                }
                                string newToken = await jwtService.GenerateJwtToken(user!);
                                return Results.Ok(new { Token = newToken });
                            }
                        }
                        else
                        {
                            logger.LogWarning("Invalid refresh token used");
                        }
                    }
                    return Results.Unauthorized();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to refresh token");
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                }
            });

            app.MapPost("/logout", async (IRefreshTokenService refreshTokenService, HttpContext context) =>
            {
                try
                {
                    logger.LogInformation("Logging out user");
                    if (context.Request.Cookies.TryGetValue("refreshToken", out string? token))
                    {
                        await refreshTokenService.RecallRefreshToken(token);
                        context.Response.Cookies.Delete("refreshToken");
                    }
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to logout user");
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                }
            });



        }
    }
}