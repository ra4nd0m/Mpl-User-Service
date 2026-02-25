using MplUserService.Models.Enums;

namespace MplUserService.Models
{
    public class ReportFile
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = null!;
        public string StoredName { get; set; } = null!;
        public SubscriptionType RequiredSubscription { get; set; }
        public DateTime UploadedAt { get; set; }
        public long SizeBytes { get; set; }
    }
}