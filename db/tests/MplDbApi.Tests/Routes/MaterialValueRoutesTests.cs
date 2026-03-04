using System.Net;
using System.Net.Http.Json;
using Moq;
using MplDbApi.Models;
using MplDbApi.Models.Dtos;
using MplDbApi.Routes;
using MplDbApi.Tests.Routes.Helpers;

namespace MplDbApi.Tests.Routes;

public class MaterialValueRoutesTests : IAsyncDisposable
{
    private readonly RouteTestHost _host = new();
    private HttpClient _client = null!;

    private async Task SetupAsync()
    {
        await _host.BuildAsync(app => app.MapMaterialValueRoutes());
        _client = _host.Client;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // --- Helpers ---

    private static MaterialValueResponseDto MakeValueDto(int id = 1) => new(
        Id: id, Uid: 10, PropertyId: 1,
        ValueDecimal: 99.5m, ValueStr: "",
        CreatedOn: new DateOnly(2025, 6, 1),
        LastUpdated: null);

    private static MaterialDateMetricReq MakeMetricReq(int materialId = 1) => new(
        MaterialId: materialId,
        PropertyIds: [1, 2, 3],
        StartDate: new DateOnly(2025, 1, 1),
        EndDate: new DateOnly(2025, 6, 30));

    // ---------- GET /materialvalues/{id} ----------

    [Fact]
    public async Task GetMaterialValueById_Returns200_WhenFound()
    {
        await SetupAsync();
        _host.MaterialValueServiceMock
            .Setup(s => s.GetMaterialValueById(1, It.IsAny<string>()))
            .ReturnsAsync(MakeValueDto());

        var response = await _client.GetAsync("/materialvalues/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MaterialValueResponseDto>();
        Assert.Equal(1, body!.Id);
    }

    [Fact]
    public async Task GetMaterialValueById_Returns404_WhenNotFound()
    {
        await SetupAsync();
        _host.MaterialValueServiceMock
            .Setup(s => s.GetMaterialValueById(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((MaterialValueResponseDto?)null);

        var response = await _client.GetAsync("/materialvalues/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMaterialValueById_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.MaterialValueServiceMock
            .Setup(s => s.GetMaterialValueById(It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/materialvalues/1");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ---------- POST /materialvalues/overview ----------

    [Fact]
    public async Task GetOverviewTableData_Returns200_WithData()
    {
        await SetupAsync();
        _host.MaterialValueServiceMock
            .Setup(s => s.GetOverviewTableData(It.IsAny<List<MaterialDateMetricReq>>(), It.IsAny<string>()))
            .ReturnsAsync([new DateGroupedMaterialValues(new DateOnly(2025, 1, 1), [])]);

        var response = await _client.PostAsJsonAsync("/materialvalues/overview", new List<MaterialDateMetricReq> { MakeMetricReq() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<DateGroupedMaterialValues>>();
        Assert.Single(body!);
    }

    [Fact]
    public async Task GetOverviewTableData_Returns400_WhenBodyIsEmpty()
    {
        await SetupAsync();

        var response = await _client.PostAsJsonAsync("/materialvalues/overview", new List<MaterialDateMetricReq>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOverviewTableData_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.MaterialValueServiceMock
            .Setup(s => s.GetOverviewTableData(It.IsAny<List<MaterialDateMetricReq>>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.PostAsJsonAsync("/materialvalues/overview", new List<MaterialDateMetricReq> { MakeMetricReq() });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ---------- POST /materialvalues/daterange ----------

    [Fact]
    public async Task GetMaterialMetricsByDateRange_Returns200_WithData()
    {
        await SetupAsync();
        _host.MaterialValueServiceMock
            .Setup(s => s.GetMaterialMetricsByDateRange(It.IsAny<MaterialDateMetricReq>(), It.IsAny<string>()))
            .ReturnsAsync([new MaterialDateMetrics(1, new DateOnly(2025, 3, 1), [1], "100", "", "", "", "", "")]);

        var response = await _client.PostAsJsonAsync("/materialvalues/daterange", MakeMetricReq());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<MaterialDateMetrics>>();
        Assert.Single(body!);
    }

    [Fact]
    public async Task GetMaterialMetricsByDateRange_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.MaterialValueServiceMock
            .Setup(s => s.GetMaterialMetricsByDateRange(It.IsAny<MaterialDateMetricReq>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.PostAsJsonAsync("/materialvalues/daterange", MakeMetricReq());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
