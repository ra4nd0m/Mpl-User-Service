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
    List<int> AvalibleProps
);
