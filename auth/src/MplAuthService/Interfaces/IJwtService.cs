using MplAuthService.Models;

namespace MplAuthService.Interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateJwtToken(User user);
        string GenerateInternalToken();
        
    }
}