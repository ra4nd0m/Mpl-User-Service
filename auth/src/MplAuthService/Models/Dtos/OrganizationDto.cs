using MplAuthService.Models.Enums;

namespace MplAuthService.Models.Dtos
{
    public record OrganizationDto(
        string Name,
        string Inn,
        SubscriptionType SubscriptionType,
        DateTime SubscriptionStartDate,
        DateTime SubscriptionEndDate
    );
}