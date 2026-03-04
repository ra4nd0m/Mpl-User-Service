using System.Net;
using System.Net.Http.Json;
using Moq;
using MplDbApi.Models;
using MplDbApi.Routes;
using MplDbApi.Tests.Routes.Helpers;

namespace MplDbApi.Tests.Routes;

public class MaterialSourceRoutesTests : IAsyncDisposable
{
    private readonly RouteTestHost _host = new();
    private HttpClient _client = null!;

    private async Task SetupAsync()
    {
        await _host.BuildAsync(app => app.MapMaterialSourceRoutes());
        _client = _host.Client;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // --- Helpers ---

    private static MaterialSourceResponseDto MakeDto(int id = 1) => new(
        Id: id, MaterialName: "Copper", Source: "LME",
        DeliveryType: "EXW", Group: "Metals",
        Market: "RU", Unit: "ton",
        LastCreatedDate: null, ChangePercent: null,
        LatestAvgValue: null, LatestMinValue: null,
        LatestMaxValue: null, LatestSupplyValue: null,
        AvalibleProps: []);

    // ---------- GET /materials ----------

    [Fact]
    public async Task GetAllMaterials_Returns200_WithList()
    {
        await SetupAsync();
        _host.MaterialSourceServiceMock
            .Setup(s => s.GetAllMaterials(It.IsAny<string>()))
            .ReturnsAsync([MakeDto(1), MakeDto(2)]);

        var response = await _client.GetAsync("/materials");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<MaterialSourceResponseDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetAllMaterials_PassesAdminRole_WhenUserIsAdmin()
    {
        await SetupAsync();
        string? capturedRole = null;
        _host.MaterialSourceServiceMock
            .Setup(s => s.GetAllMaterials(It.IsAny<string>()))
            .Callback<string>(r => capturedRole = r)
            .ReturnsAsync([]);

        await _client.GetAsync("/materials");

        Assert.Equal("Admin", capturedRole);
    }

    [Fact]
    public async Task GetAllMaterials_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.MaterialSourceServiceMock
            .Setup(s => s.GetAllMaterials(It.IsAny<string>()))
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/materials");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ---------- GET /materials/{id} ----------

    [Fact]
    public async Task GetMaterialById_Returns200_WhenFound()
    {
        await SetupAsync();
        _host.MaterialSourceServiceMock
            .Setup(s => s.GetMaterialById(1, It.IsAny<string>()))
            .ReturnsAsync(MakeDto(1));

        var response = await _client.GetAsync("/materials/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MaterialSourceResponseDto>();
        Assert.Equal("Copper", body!.MaterialName);
    }

    [Fact]
    public async Task GetMaterialById_Returns404_WhenNotFound()
    {
        await SetupAsync();
        _host.MaterialSourceServiceMock
            .Setup(s => s.GetMaterialById(It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException("not found"));

        var response = await _client.GetAsync("/materials/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMaterialById_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.MaterialSourceServiceMock
            .Setup(s => s.GetMaterialById(It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/materials/1");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ---------- GET /materials/bygroup/{groupId} ----------

    [Fact]
    public async Task GetMaterialsByGroup_Returns200_WithList()
    {
        await SetupAsync();
        _host.MaterialSourceServiceMock
            .Setup(s => s.GetMaterialsByGroup(2, It.IsAny<string>()))
            .ReturnsAsync([MakeDto(1), MakeDto(3)]);

        var response = await _client.GetAsync("/materials/bygroup/2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<MaterialSourceResponseDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetMaterialsByGroup_Returns404_WhenGroupNotFound()
    {
        await SetupAsync();
        _host.MaterialSourceServiceMock
            .Setup(s => s.GetMaterialsByGroup(It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException("group not found"));

        var response = await _client.GetAsync("/materials/bygroup/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMaterialsByGroup_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.MaterialSourceServiceMock
            .Setup(s => s.GetMaterialsByGroup(It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/materials/bygroup/1");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
