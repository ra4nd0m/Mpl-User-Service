using MplAuthService.Models;

namespace MplAuthService.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> GenerateRefreshToken(User user);
        Task<RefreshToken?> ValidateRefreshToken(string token);
        Task RecallRefreshToken(string token);
    }
}