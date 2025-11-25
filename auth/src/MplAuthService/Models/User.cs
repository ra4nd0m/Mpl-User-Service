using Microsoft.AspNetCore.Identity;

namespace MplAuthService.Models
{
    public class User : IdentityUser
    {
        public int? OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public int? IndividualSubscriptionId { get; set; }
        public IndividualSubscription? IndividualSubscription { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
        public bool CanExportData { get; set; } = false;
    }
}