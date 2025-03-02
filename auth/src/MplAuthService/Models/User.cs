using Microsoft.AspNetCore.Identity;

namespace MplAuthService.Models
{
    public class User : IdentityUser
    {
        public int OrganizationId { get; set; }
        public required Organization? Organization { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    }
}