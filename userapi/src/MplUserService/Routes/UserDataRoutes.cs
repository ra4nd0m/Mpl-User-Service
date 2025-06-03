using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MplUserService.Data;
using MplUserService.Interfaces;

namespace MplUserService.Routes
{
    public static class UserDataRoutes
    {
        public static void MapUserDataRoutes(this WebApplication app)
        {
            app.MapGet("/favorites", async (IUserService userService, HttpContext context, ILogger<Program> logger) =>
            {
                try
                {
                    logger.LogInformation("Getting user favorites");
                    var user = await userService.GetOrCreateUserAsync(context.User);
                    return Results.Ok(user.FavouriteIds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to get user favorites");
                    return Results.BadRequest();
                }
            }).RequireAuthorization();

            app.MapPut("/favorites/{itemId}", async (int itemId, IUserService userService, HttpContext context,
                UserContext dbContext, ILogger<Program> logger) =>
            {
                try
                {
                    if (itemId <= 0)
                    {
                        return Results.BadRequest("Item ID is required");
                    }
                    var user = await userService.GetOrCreateUserAsync(context.User);
                    if (!user.FavouriteIds.Contains(itemId))
                    {
                        user.FavouriteIds.Add(itemId);
                        // Explicitly tell EF Core that the entity has been modified
                        dbContext.Entry(user).State = EntityState.Modified;
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation("Added item to favorites");
                    }
                    return Results.Ok(user.FavouriteIds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to add item to favorites");
                    return Results.BadRequest();
                }

            }).RequireAuthorization();

            app.MapDelete("/favorites/{itemId}", async (int itemId, IUserService userService, HttpContext context,
                UserContext dbContext, ILogger<Program> logger) =>
            {
                try
                {
                    if (itemId <= 0)
                    {
                        return Results.BadRequest("Item ID is required");
                    }
                    var user = await userService.GetOrCreateUserAsync(context.User);
                    if (user.FavouriteIds.Contains(itemId))
                    {
                        user.FavouriteIds.Remove(itemId);
                        dbContext.Entry(user).State = EntityState.Modified;
                        await dbContext.SaveChangesAsync();
                    }
                    return Results.Ok(user.FavouriteIds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to remove item from favorites");
                    return Results.BadRequest();
                }

            }).RequireAuthorization();

            app.MapGet("/settings", async (IUserService userService, HttpContext context, UserContext dbContext, ILogger<Program> logger) =>
            {
                try
                {
                    var user = await userService.GetOrCreateUserAsync(context.User);
                    if (user.SettingsJson == null)
                    {
                        return Results.NotFound("User settings not found");
                    }
                    return Results.Ok(user.SettingsJson);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to get user settings");
                    return Results.BadRequest();
                }
            }).RequireAuthorization();

            app.MapPut("/settings", async (IUserService userService, HttpContext context, UserContext dbContext, ILogger<Program> logger) =>
            {
                try
                {
                    var jsonDocument = await JsonDocument.ParseAsync(context.Request.Body);
                    var settingsBlob = JsonSerializer.Serialize(jsonDocument);

                    if (string.IsNullOrEmpty(settingsBlob))
                    {
                        return Results.BadRequest("Settings cannot be empty");
                    }

                    var user = await userService.GetOrCreateUserAsync(context.User);
                    user.SettingsJson = settingsBlob;
                    dbContext.Entry(user).State = EntityState.Modified;
                    await dbContext.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (JsonException jsonEx)
                {
                    logger.LogError(jsonEx, "Invalid JSON format for user settings");
                    return Results.BadRequest("Invalid JSON format for user settings");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to update user settings");
                    return Results.BadRequest();
                }
            }).RequireAuthorization();
        }
    }
}