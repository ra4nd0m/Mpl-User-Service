using System.Net;
using System.Net.Http.Json;
using Moq;
using MplDbApi.Models.Dtos;
using MplDbApi.Routes;
using MplDbApi.Tests.Routes.Helpers;

namespace MplDbApi.Tests.Routes;

public class UnitRoutesTests : IAsyncDisposable
{
    private readonly RouteTestHost _host = new();
    private HttpClient _client = null!;

    private async Task SetupAsync()
    {
        await _host.BuildAsync(app => app.MapUnitRoutes());
        _client = _host.Client;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // ---------- GET /units ----------

    [Fact]
    public async Task GetUnits_Returns200_WithList()
    {
        await SetupAsync();
        _host.UnitServiceMock
            .Setup(s => s.GetUnits())
            .ReturnsAsync([new IdValuePair(1, "ton"), new IdValuePair(2, "kg")]);

        var response = await _client.GetAsync("/units");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<IdValuePair>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetUnits_Returns200_WhenEmpty()
    {
        await SetupAsync();
        _host.UnitServiceMock
            .Setup(s => s.GetUnits())
            .ReturnsAsync([]);

        var response = await _client.GetAsync("/units");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<IdValuePair>>();
        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetUnits_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.UnitServiceMock
            .Setup(s => s.GetUnits())
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/units");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
