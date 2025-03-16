namespace MplAuthService.Models.Dtos
{
    public record UserResponseDto(string Id, string Email, OrganizationDto? Org);
}