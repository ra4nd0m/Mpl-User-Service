namespace MplAuthService.Models.Dtos
{
    public record UpdateUserDto(string? NewEmail, string? Password, OrganizationDto? Organization, SubscriptionDataDto? Sub);
}