using Microsoft.AspNetCore.Identity;

namespace MplAuthService.Models
{
    public class User : IdentityUser
    {
        public int OrganisationId { get; set; }
        public required Organisation Organisation { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    }
}