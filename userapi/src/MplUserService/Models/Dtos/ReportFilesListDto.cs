using MplUserService.Models.Enums;

namespace MplUserService.Models.Dtos
{
    public sealed record ReportFilesListDto(
        Guid Id,
        string FileName,
        string Group,
        SubscriptionType RequiredSubscription,
        DateTime UploadedAt
    );
}