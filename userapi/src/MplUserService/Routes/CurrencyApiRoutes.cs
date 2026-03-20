using MplUserService.Interfaces;

namespace MplUserService.Routes
{
    public static class CurrencyApiRoutes
    {
        public static void MapCurrencyApiRoutes(this WebApplication app)
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("CurrencyApiRoutes");

            app.MapGet("/currency/latest", async (ICurrencyRatesService service, CancellationToken ct) =>
            {
                try
                {
                    var ratesSnapshot = await service.GetLatestRatesAsync(ct);
                    return Results.Ok(ratesSnapshot);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error fetching latest currency rates: ${ex}");
                    return Results.Problem("An error occurred while fetching the latest currency rates.");
                }
            }).RequireAuthorization();
        }
    }
}