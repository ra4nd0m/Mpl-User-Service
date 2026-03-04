using System.Net;
using System.Net.Http.Json;
using Moq;
using MplDbApi.Models.Dtos;
using MplDbApi.Routes;
using MplDbApi.Tests.Routes.Helpers;

namespace MplDbApi.Tests.Routes;

public class MaterialPropertyRoutesTests : IAsyncDisposable
{
    private readonly RouteTestHost _host = new();
    private HttpClient _client = null!;

    private async Task SetupAsync()
    {
        await _host.BuildAsync(app => app.MapMaterialPropertyRoutes());
        _client = _host.Client;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // ---------- GET /material/{materialId}/properties ----------

    [Fact]
    public async Task GetMaterialProperties_Returns200_WithList()
    {
        await SetupAsync();
        _host.MaterialPropServiceMock
            .Setup(s => s.GetMaterialProperties(5))
            .ReturnsAsync([new MaterialPropertyResp(1, "Price Avg"), new MaterialPropertyResp(2, "Price Min")]);

        var response = await _client.GetAsync("/material/5/properties");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<MaterialPropertyResp>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetMaterialProperties_Returns200_WhenEmpty()
    {
        await SetupAsync();
        _host.MaterialPropServiceMock
            .Setup(s => s.GetMaterialProperties(It.IsAny<int>()))
            .ReturnsAsync([]);

        var response = await _client.GetAsync("/material/1/properties");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<MaterialPropertyResp>>();
        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetMaterialProperties_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.MaterialPropServiceMock
            .Setup(s => s.GetMaterialProperties(It.IsAny<int>()))
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/material/1/properties");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ---------- GET /properties/dropdown ----------

    [Fact]
    public async Task GetPropertiesForDropdown_Returns200_WithList()
    {
        await SetupAsync();
        _host.MaterialPropServiceMock
            .Setup(s => s.GetMaterialPropertiesForDropdown())
            .ReturnsAsync([new IdValuePair(1, "Price Avg"), new IdValuePair(2, "Supply")]);

        var response = await _client.GetAsync("/properties/dropdown");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<IdValuePair>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetPropertiesForDropdown_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.MaterialPropServiceMock
            .Setup(s => s.GetMaterialPropertiesForDropdown())
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/properties/dropdown");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
