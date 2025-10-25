namespace MplAuthService.Models.Enums
{
    public record SubscriptionDataDto(
        SubscriptionType SubscriptionType,
        DateTime SubscriptionStartDate,
        DateTime SubscriptionEndDate
    );
}