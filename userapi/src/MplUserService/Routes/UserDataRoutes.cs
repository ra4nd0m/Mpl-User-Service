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

            app.MapPost("/favorites", async (int itemId, IUserService userService, HttpContext context,
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
        }
    }
}