using MplUserService.Models.Dtos;

namespace MplUserService.Interfaces
{
    public interface ICurrencyRatesService
    {
        Task<CurrencyRatesSnapshotDto> GetLatestRatesAsync(CancellationToken cancellationToken = default);
    }
}