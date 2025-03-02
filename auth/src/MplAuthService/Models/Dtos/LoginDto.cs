namespace MplAuthService.Models.Dtos
{
    public record LoginDto(
        string Email,
        string Password,
        bool RememberMe
    );
}