using MplAuthService.Models.Enums;

namespace MplAuthService.Models.Dtos
{
    public record CreateUserDto(
        string Email,
        string Password,
        OrganizationDto? Organization = null,
        SubscriptionDataDto? Sub = null
    );
}