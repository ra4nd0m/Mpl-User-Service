namespace MplDbApi.Models;

public record MaterialValueResponseDto(
    int Id,
    int Uid,
    int PropertyId,
    decimal ValueDecimal,
    string ValueStr,
    DateOnly CreatedOn,
    DateTime? LastUpdated
);
