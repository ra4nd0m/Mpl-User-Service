using System.Net;
using System.Net.Http.Json;
using Moq;
using MplDbApi.Models.Dtos;
using MplDbApi.Routes;
using MplDbApi.Tests.Routes.Helpers;

namespace MplDbApi.Tests.Routes;

public class SourceRoutesTests : IAsyncDisposable
{
    private readonly RouteTestHost _host = new();
    private HttpClient _client = null!;

    private async Task SetupAsync()
    {
        await _host.BuildAsync(app => app.MapSourceRoutes());
        _client = _host.Client;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // ---------- GET /sources ----------

    [Fact]
    public async Task GetSources_Returns200_WithList()
    {
        await SetupAsync();
        _host.SourceServiceMock
            .Setup(s => s.GetSources())
            .ReturnsAsync([new IdValuePair(1, "LME"), new IdValuePair(2, "Reuters")]);

        var response = await _client.GetAsync("/sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<IdValuePair>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetSources_Returns200_WhenEmpty()
    {
        await SetupAsync();
        _host.SourceServiceMock
            .Setup(s => s.GetSources())
            .ReturnsAsync([]);

        var response = await _client.GetAsync("/sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<IdValuePair>>();
        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetSources_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.SourceServiceMock
            .Setup(s => s.GetSources())
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/sources");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
