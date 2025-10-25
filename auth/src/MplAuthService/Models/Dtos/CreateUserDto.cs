using MplAuthService.Models.Enums;

namespace MplAuthService.Models.Dtos
{
    public record CreateUserDto(
        string Email,
        string Password,
        UserSubscriptionType Type,
        OrganizationDto? Organization = null,
        SubscriptionDataDto? SubscriptionData = null
    );
}