using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MplUserService.Auth;
using MplUserService.Data;
using MplUserService.Interfaces;
using MplUserService.Models;
using MplUserService.Models.Dtos;
using MplUserService.Models.Enums;
using MplUserService.Config;

namespace MplUserService.Services
{
    public sealed class ReportFileService(
        UserContext context,
        IObjectStore store,
        IAuthorizationService authorizationService,
        IOptions<StorageQuotaOptions> quotaOptions
    ) : IReportFileService
    {
        private readonly long _maxBytes = quotaOptions.Value.MaxBytes;
        public async Task<Guid> UploadAsync(
            IFormFile file,
            SubscriptionType requiredSubscription,
            string group,
            CancellationToken ct
        )
        {
            var currentlyUsedBytes = await context.ReportFiles.SumAsync(f => f.SizeBytes, ct);
            var incomingFileSize = file.Length;

            if (incomingFileSize <= 0)
                throw new InvalidOperationException("File is empty");
            if (currentlyUsedBytes + incomingFileSize > _maxBytes)
                throw new InvalidOperationException("Storage quota exceeded");

            var key = $"{Guid.NewGuid()}.pdf";

            await using var stream = file.OpenReadStream();
            await store.PutAsync(key, stream, "application/pdf", ct);

            var entity = new ReportFile
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                StoredName = key,
                FileGroup = group,
                RequiredSubscription = requiredSubscription,
                UploadedAt = DateTime.UtcNow,
                SizeBytes = incomingFileSize
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
                    f.FileGroup,
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

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var file = await context.ReportFiles.FindAsync([id], ct);

            if (file == null)
                return;

            await store.DeleteAsync(file.StoredName, ct);

            context.ReportFiles.Remove(file);
            await context.SaveChangesAsync(ct);
        }

        public async Task<StorageUsageDto> GetReportStorageUsageAsync(CancellationToken ct)
        {
            var usedBytes = await context.ReportFiles.SumAsync(f => f.SizeBytes, ct);
            return new StorageUsageDto(usedBytes, _maxBytes);
        }
    }
}