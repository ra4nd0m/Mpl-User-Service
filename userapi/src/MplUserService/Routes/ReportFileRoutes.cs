using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MplUserService.Interfaces;
using MplUserService.Models.Enums;

namespace MplUserService.Routes
{
    public static class ReportFileRoutes
    {
        public static void MapReportFileRoutes(this WebApplication app)
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("ReportFileRoutes");

            app.MapGet("/reports", async (IReportFileService service, CancellationToken ct) =>
            {
                try
                {
                    var files = await service.ListAsync(ct);
                    return Results.Ok(files);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error listing files. See: ${ex}");
                    return Results.BadRequest();
                }
            })
            .RequireAuthorization()
            .RequireRateLimiting("DownloadPolicy");

            app.MapGet("/reports/{id:guid}", async (
                Guid id,
                IReportFileService service,
                ClaimsPrincipal user,
                CancellationToken ct
            ) =>
            {
                try
                {
                    var (stream, fileName) = await service.DownloadAsync(id, user, ct);

                    return Results.Stream(
                        stream,
                        "application/pdf",
                        fileName
                    );
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogWarning($"File access error. See: ${ex}");
                    return Results.NotFound();
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error downloading file. See: ${ex}");
                    return Results.BadRequest();
                }
            })
            .RequireAuthorization();

            app.MapPost("/reports/upload", async (
                HttpRequest request,
                IReportFileService service,
                CancellationToken ct
            ) =>
            {
                try
                {
                    if (!request.HasFormContentType)
                        return Results.BadRequest("Expected form data");

                    var form = await request.ReadFormAsync(ct);
                    var file = form.Files.GetFile("file");
                    var requiredSubscriptionStr = form["requiredSubscription"].ToString();

                    if (file == null || string.IsNullOrEmpty(requiredSubscriptionStr))
                        return Results.BadRequest("Missing file or requiredSubscription");

                    if (!Enum.TryParse<SubscriptionType>(
                        requiredSubscriptionStr,
                        out var requiredSubscription
                    ))
                    {
                        return Results.BadRequest("Invalid requiredSubscription value");
                    }

                    var id = await service.UploadAsync(file, requiredSubscription, ct);

                    return Results.Ok(new { Id = id });
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error uploading file. See: ${ex}");
                    return Results.BadRequest();
                }
            })
            .RequireAuthorization("RequireAdmin")
            .WithMetadata(new RequestSizeLimitAttribute(20 * 1024 * 1024)); 

            app.MapDelete("/reports/{id:guid}", async (
                Guid id,
                IReportFileService service,
                CancellationToken ct
            ) =>
            {
                try
                {
                    await service.DeleteAsync(id, ct);
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error deleting file. See: ${ex}");
                    return Results.BadRequest();
                }
            })
            .RequireAuthorization("RequireAdmin");
        }
    }
}
