using MplAuthService.Models.Enums;

namespace MplAuthService.Models.Dtos
{
    public record UserResponseDto(string Id, string Email, OrganizationDto? Org, SubscriptionDataDto? Sub = null, bool CanExportData = false);
}