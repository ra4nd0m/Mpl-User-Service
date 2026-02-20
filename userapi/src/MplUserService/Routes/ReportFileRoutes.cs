namespace MplUserService.Routes
{
    public static class ReportFileRoutes
    {
        public static void MapReportFileRoutes(this WebApplication app)
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("ReportFileRoutes");

            app.MapGet("/reports", () =>
            {

            });

            app.MapGet("/reports/{id:guid}", () =>
            {
                
            });

            app.MapPost("/reports/upload", () =>
            {

            })
            .RequireAuthorization("RequireAdmin");
        }
    }
}
