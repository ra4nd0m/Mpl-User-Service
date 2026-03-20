namespace MplUserService.Models.Dtos
{
    public record CurrencyRatesSnapshotDto(
        DateOnly AcutalDate,
        IReadOnlyDictionary<string, decimal> Rates
    );
}