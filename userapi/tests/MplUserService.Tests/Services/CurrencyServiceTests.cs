using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MplUserService.Config;
using MplUserService.Interfaces;
using MplUserService.Models.Dtos;
using MplUserService.Routes;
using MplUserService.Services;
using MplUserService.Tests.Routes.Helpers;

namespace MplUserService.Tests.Services;

public class CurrencyServiceTests : IAsyncDisposable
{
	private WebApplication _app = null!;
	private HttpClient _client = null!;
	private StubCurrencyApiHandler _currencyApiHandler = null!;

	private async Task SetupAsync()
	{
		_currencyApiHandler = new StubCurrencyApiHandler();

		var builder = WebApplication.CreateSlimBuilder();
		builder.WebHost.UseTestServer();

		builder.Services
			.AddAuthentication(TestAuthHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
		builder.Services.AddAuthorization();

		builder.Services.Configure<CurrencyApiOptions>(x =>
		{
			x.BaseUrl = "https://www.cbr.ru/";
			x.CacheHours = 6;
		});
		builder.Services.AddMemoryCache();
		builder.Services.AddHttpClient("CurrencyApiClient", (serviceProvider, client) =>
			{
				var currencyApiOptions = serviceProvider.GetRequiredService<IOptions<CurrencyApiOptions>>().Value;
				client.BaseAddress = new Uri(currencyApiOptions.BaseUrl);
			})
			.ConfigurePrimaryHttpMessageHandler(() => _currencyApiHandler);
		builder.Services.AddScoped<ICurrencyRatesService, CurrencyRatesService>();

		builder.Logging.ClearProviders();

		_app = builder.Build();
		_app.UseAuthentication();
		_app.UseAuthorization();
		_app.MapCurrencyApiRoutes();

		await _app.StartAsync();
		_client = _app.GetTestClient();
	}

	[Fact]
	public async Task CurrencyLatest_ReturnsRates_AndUsesCache()
	{
		await SetupAsync();

		var firstResponse = await _client.GetAsync("/currency/latest");
		Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

		var firstBody = await firstResponse.Content.ReadFromJsonAsync<CurrencyRatesSnapshotDto>();
		Assert.NotNull(firstBody);
		Assert.Equal(new DateOnly(2026, 3, 20), firstBody!.AcutalDate);
		Assert.Equal(1m, firstBody.Rates["RUB"]);
		Assert.Equal(84.5m, firstBody.Rates["USD"]);
		Assert.Equal(92.125m, firstBody.Rates["EUR"]);
		Assert.Equal(11.635m, firstBody.Rates["CNY"]);
		Assert.Equal(1.0225m, firstBody.Rates["INR"]);
		Assert.False(firstBody.Rates.ContainsKey("IRR"));

		var secondResponse = await _client.GetAsync("/currency/latest");
		Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
		Assert.Equal(1, _currencyApiHandler.CallCount);
	}

	public async ValueTask DisposeAsync()
	{
		_client?.Dispose();
		_currencyApiHandler?.Dispose();
		if (_app is not null)
		{
			await _app.DisposeAsync();
		}
	}

	private sealed class StubCurrencyApiHandler : HttpMessageHandler
	{
		private int _callCount;

		public int CallCount => _callCount;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref _callCount);

			var xml = """
				<?xml version="1.0" encoding="windows-1251"?>
				<ValCurs Date="20.03.2026" name="Foreign Currency Market">
					<Valute ID="R01235">
						<CharCode>USD</CharCode>
						<VunitRate>84,5000</VunitRate>
					</Valute>
					<Valute ID="R01239">
						<CharCode>EUR</CharCode>
						<VunitRate>92,1250</VunitRate>
					</Valute>
					<Valute ID="R01375">
						<CharCode>CNY</CharCode>
						<VunitRate>11,6350</VunitRate>
					</Valute>
					<Valute ID="R01270">
						<CharCode>INR</CharCode>
						<VunitRate>1,0225</VunitRate>
					</Valute>
					<Valute ID="R01000">
						<CharCode>IRR</CharCode>
						<VunitRate>5,88139E-05</VunitRate>
					</Valute>
				</ValCurs>
				""";

			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ByteArrayContent(Encoding.GetEncoding("windows-1251").GetBytes(xml))
			};
			response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml")
			{
				CharSet = "windows-1251"
			};

			return Task.FromResult(response);
		}
	}
}

