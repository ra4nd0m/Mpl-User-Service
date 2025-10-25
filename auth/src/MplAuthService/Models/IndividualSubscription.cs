using MplAuthService.Models.Enums;

namespace MplAuthService.Models
{
    public class IndividualSubscription
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public required User User { get; set; }
        public required SubscriptionType SubscriptionType { get; set; }
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
    }
}