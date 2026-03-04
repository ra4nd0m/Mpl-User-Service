using System.Net;
using System.Net.Http.Json;
using Moq;
using MplDbApi.Models.Dtos;
using MplDbApi.Routes;
using MplDbApi.Tests.Routes.Helpers;

namespace MplDbApi.Tests.Routes;

public class DeliveryTypeRoutesTests : IAsyncDisposable
{
    private readonly RouteTestHost _host = new();
    private HttpClient _client = null!;

    private async Task SetupAsync()
    {
        await _host.BuildAsync(app => app.MapDeliveryTypeRoutes());
        _client = _host.Client;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // ---------- GET /deliverytypes ----------

    [Fact]
    public async Task GetDeliveryTypes_Returns200_WithList()
    {
        await SetupAsync();
        _host.DeliveryTypeServiceMock
            .Setup(s => s.GetDeliveryTypesAsync())
            .ReturnsAsync([new DeliveryTypeDto(1, "EXW"), new DeliveryTypeDto(2, "CIF")]);

        var response = await _client.GetAsync("/deliverytypes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<DeliveryTypeDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetDeliveryTypes_Returns200_WhenEmpty()
    {
        await SetupAsync();
        _host.DeliveryTypeServiceMock
            .Setup(s => s.GetDeliveryTypesAsync())
            .ReturnsAsync([]);

        var response = await _client.GetAsync("/deliverytypes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<DeliveryTypeDto>>();
        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetDeliveryTypes_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.DeliveryTypeServiceMock
            .Setup(s => s.GetDeliveryTypesAsync())
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/deliverytypes");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ---------- GET /deliverytypes/{id} ----------

    [Fact]
    public async Task GetDeliveryTypeById_Returns200_WhenFound()
    {
        await SetupAsync();
        _host.DeliveryTypeServiceMock
            .Setup(s => s.GetDeliveryTypeByIdAsync(1))
            .ReturnsAsync(new DeliveryTypeDto(1, "EXW"));

        var response = await _client.GetAsync("/deliverytypes/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DeliveryTypeDto>();
        Assert.Equal("EXW", body!.Name);
    }

    [Fact]
    public async Task GetDeliveryTypeById_Returns404_WhenNotFound()
    {
        await SetupAsync();
        _host.DeliveryTypeServiceMock
            .Setup(s => s.GetDeliveryTypeByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((DeliveryTypeDto?)null);

        var response = await _client.GetAsync("/deliverytypes/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDeliveryTypeById_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.DeliveryTypeServiceMock
            .Setup(s => s.GetDeliveryTypeByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/deliverytypes/1");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
