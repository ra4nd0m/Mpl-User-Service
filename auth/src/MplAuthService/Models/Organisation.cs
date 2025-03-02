using MplAuthService.Models.Enums;

namespace MplAuthService.Models
{
    public class Organisation
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Inn { get; set; }
        public required SubscriptionType SubscriptionType { get; set; }
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
        public ICollection<User> Users { get; set; } = [];
    }
}