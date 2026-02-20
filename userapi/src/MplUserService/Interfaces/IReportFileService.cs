using System.Security.Claims;
using MplUserService.Models;
using MplUserService.Models.Dtos;
using MplUserService.Models.Enums;

namespace MplUserService.Interfaces
{
    public interface IReportFileService
    {
        Task<Guid> UploadAsync(IFormFile file,
            SubscriptionType requiredSubscription,
            CancellationToken ct);

        Task<List<ReportFilesListDto>> ListAsync(CancellationToken ct);
        
        Task<(Stream Stream, string FileName)> DownloadAsync(
            Guid id,
            ClaimsPrincipal user,
            CancellationToken ct
        );
    }
}