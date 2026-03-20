using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MplUserService.Config;
using MplUserService.Interfaces;
using MplUserService.Models.Dtos;

namespace MplUserService.Services
{
    public class CurrencyRatesService(
        IHttpClientFactory clientFactory,
        IMemoryCache cache,
        IOptions<CurrencyApiOptions> options
    ) : ICurrencyRatesService
    {
        private static readonly HashSet<string> RequiredCurrencies =
            ["USD", "EUR", "CNY", "INR"];

        static CurrencyRatesService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private readonly CurrencyApiOptions _options = options.Value;

        public async Task<CurrencyRatesSnapshotDto> GetLatestRatesAsync(CancellationToken ct = default)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            var cacheKey = $"currency-rates:{today:yyyy-MM-dd}";
            return await cache.GetOrCreateAsync(cacheKey, async x =>
            {
                x.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_options.CacheHours);
                var client = clientFactory.CreateClient("CurrencyApiClient");
                var relativeUrl = BuildRelativeUrl(today);
                var xml = await client.GetStringAsync(relativeUrl, ct);
                return ParseXmlToCurrencyDto(xml);
            }) ?? throw new InvalidOperationException("Failed to load currency rates");
        }

        private static CurrencyRatesSnapshotDto ParseXmlToCurrencyDto(string xml)
        {
            var document = XDocument.Parse(xml);
            var root = document.Root ?? throw new InvalidOperationException("XML root is missing");

            var dateAttr = root.Attribute("Date")?.Value
                ?? throw new InvalidOperationException("ValCurs/@Date is missing");

            if (!DateOnly.TryParseExact(
                dateAttr,
                "dd.MM.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var actualDate
            ))
            {
                throw new InvalidOperationException($"Unable to parse rates date: {dateAttr}");
            }

            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["RUB"] = 1m
            };

            foreach (var valute in root.Elements("Valute"))
            {
                var charCode = valute.Element("CharCode")?.Value?.Trim();
                var vunitRateRaw = valute.Element("VunitRate")?.Value?.Trim();

                if (string.IsNullOrWhiteSpace(charCode) || string.IsNullOrWhiteSpace(vunitRateRaw))
                {
                    continue;
                }

                if (!RequiredCurrencies.Contains(charCode))
                {
                    continue;
                }

                var normalized = vunitRateRaw.Replace(',', '.');

                if (!decimal.TryParse(
                    normalized,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value
                ))
                {
                    throw new InvalidOperationException($"Unable to parse rate value: {vunitRateRaw} for currency {charCode}");
                }
                rates[charCode] = value;
            }
            return new CurrencyRatesSnapshotDto(
                AcutalDate: actualDate,
                Rates: rates
            );
        }

        private static string BuildRelativeUrl(DateOnly date)
        {
            var formatted = date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            return $"scripts/XML_daily.asp?date_req={formatted}";
        }
    }
}