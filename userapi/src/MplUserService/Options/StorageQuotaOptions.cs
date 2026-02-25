namespace MplUserService.Options
{
    public sealed class StorageQuotaOptions
    {
        public const string SectionName = "StorageQuota";
        public long MaxBytes { get; init; }
    }
}