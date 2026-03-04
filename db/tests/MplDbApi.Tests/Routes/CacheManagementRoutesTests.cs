using System.Net;
using System.Net.Http.Json;
using MplDbApi.Routes;
using MplDbApi.Tests.Routes.Helpers;

namespace MplDbApi.Tests.Routes;

public class CacheManagementRoutesTests : IAsyncDisposable
{
    private readonly RouteTestHost _host = new();
    private HttpClient _client = null!;

    private async Task SetupAsync()
    {
        await _host.BuildAsync(app => app.MapCacheManagementRoutes());
        _client = _host.Client;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // ---------- POST /cache/clear ----------

    [Fact]
    public async Task ClearCache_Returns200_WithSuccessMessage()
    {
        await SetupAsync();

        var response = await _client.PostAsync("/cache/clear", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CacheClearResponse>();
        Assert.Equal("Cache cleared successfully", body!.message);
    }

    [Fact]
    public async Task ClearCache_Returns200_WhenCalledMultipleTimes()
    {
        await SetupAsync();

        await _client.PostAsync("/cache/clear", null);
        var response = await _client.PostAsync("/cache/clear", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Helper record to deserialize the anonymous response object
    private record CacheClearResponse(string message);
}
