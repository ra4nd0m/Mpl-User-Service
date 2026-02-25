namespace MplUserService.Models.Dtos
{
    public sealed record StorageUsageDto(
        long UsedBytes,
        long MaxBytes
    );
}