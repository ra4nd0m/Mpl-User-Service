namespace MplDbApi.Models.Dtos
{
    public record CompactMaterialInfo(
        int Id,
        string MaterialName,
        string DeliveryType,
        string Market,
        string Unit,
        string? Description,
        int? RoundTo
    );
}