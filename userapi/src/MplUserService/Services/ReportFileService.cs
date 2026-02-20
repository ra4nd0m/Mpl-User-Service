using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MplUserService.Auth;
using MplUserService.Data;
using MplUserService.Interfaces;
using MplUserService.Models;
using MplUserService.Models.Dtos;
using MplUserService.Models.Enums;

namespace MplUserService.Services
{
    public sealed class ReportFileService(UserContext context, IObjectStore store, IAuthorizationService authorizationService)
        : IReportFileService
    {
        public async Task<Guid> UploadAsync(
            IFormFile file,
            SubscriptionType requiredSubscription,
            CancellationToken ct
        )
        {
            var key = $"{Guid.NewGuid()}.pdf";

            await using var stream = file.OpenReadStream();
            await store.PutAsync(key, stream, "application/pdf", ct);

            var entity = new ReportFile
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                StoredName = key,
                RequiredSubscription = requiredSubscription,
                UploadedAt = DateTime.UtcNow
            };

            context.ReportFiles.Add(entity);
            await context.SaveChangesAsync(ct);

            return entity.Id;
        }
        public async Task<List<ReportFilesListDto>> ListAsync(CancellationToken ct)
        {
            var files = await context.ReportFiles
                .Select(f => new ReportFilesListDto(
                    f.Id,
                    f.FileName,
                    f.RequiredSubscription,
                    f.UploadedAt
                ))
                .ToListAsync(ct);

            return files;
        }
        public async Task<(Stream Stream, string FileName)> DownloadAsync(
            Guid id,
            ClaimsPrincipal user,
            CancellationToken ct
        )
        {
            var file = await context.ReportFiles.FindAsync([id], ct);

            if (file == null)
            {
                throw new UnauthorizedAccessException();
            }

            var requirement = new SubscriptionRequirement(file.RequiredSubscription);

            var authResult = await authorizationService.AuthorizeAsync(
                user,
                null,
                requirement
            );

            if (!authResult.Succeeded)
                throw new UnauthorizedAccessException();

            var (stream, _) = await store.GetAsync(file.StoredName, ct);
            return (stream, file.FileName);
        }
    }
}