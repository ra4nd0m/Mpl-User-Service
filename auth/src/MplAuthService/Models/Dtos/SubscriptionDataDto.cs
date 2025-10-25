using MplAuthService.Models.Enums;

namespace MplAuthService.Models.Dtos
{
    public record SubscriptionDataDto(
        SubscriptionType SubscriptionType,
        DateTime SubscriptionStartDate,
        DateTime SubscriptionEndDate
    );
}