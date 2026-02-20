using MplUserService.Interfaces;

namespace MplUserService.Routes
{
    public static class InternalRoutes
    {
        public static void MapInternalRoutes(this WebApplication app)
        {
            app.MapDelete("/user/{userId}", async (string userId, IUserService service, ILogger<Program> logger) =>
            {
                try
                {
                    var success = await service.DeleteUserAsync(userId);
                    return success ? Results.Ok() : Results.NotFound();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deleting user {UserId}", userId);
                    return Results.BadRequest();
                }
            }).RequireAuthorization("internal");
        }
    }
}