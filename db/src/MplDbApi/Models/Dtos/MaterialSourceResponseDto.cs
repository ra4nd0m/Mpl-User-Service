namespace MplDbApi.Models;

public record MaterialSourceResponseDto(
    int Id,
    string MaterialName,
    string Source,
    string DeliveryType,
    string Group,
    string Market,
    string Unit,
    DateOnly? LastCreatedDate,
    string? ChangePercent,
    decimal? LatestAvgValue,
    decimal? LatestMinValue,
    decimal? LatestMaxValue,
    decimal? LatestSupplyValue,
    List<int> AvalibleProps
);
