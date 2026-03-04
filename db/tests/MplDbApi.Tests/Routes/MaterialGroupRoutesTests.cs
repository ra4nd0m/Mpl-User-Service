using System.Net;
using System.Net.Http.Json;
using Moq;
using MplDbApi.Models.Dtos;
using MplDbApi.Routes;
using MplDbApi.Tests.Routes.Helpers;

namespace MplDbApi.Tests.Routes;

public class MaterialGroupRoutesTests : IAsyncDisposable
{
    private readonly RouteTestHost _host = new();
    private HttpClient _client = null!;

    private async Task SetupAsync()
    {
        await _host.BuildAsync(app => app.MapMaterialGroupRoutes());
        _client = _host.Client;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // ---------- GET /materialgroups ----------

    [Fact]
    public async Task GetMaterialGroups_Returns200_WithList()
    {
        await SetupAsync();
        _host.MaterialGroupServiceMock
            .Setup(s => s.GetMaterialGroupAsync(It.IsAny<string>()))
            .ReturnsAsync([new MaterialGroupDto(1, "Metals"), new MaterialGroupDto(2, "Polymers")]);

        var response = await _client.GetAsync("/materialgroups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<MaterialGroupDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetMaterialGroups_Returns200_WhenEmpty()
    {
        await SetupAsync();
        _host.MaterialGroupServiceMock
            .Setup(s => s.GetMaterialGroupAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var response = await _client.GetAsync("/materialgroups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<MaterialGroupDto>>();
        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetMaterialGroups_PassesAdminRole_WhenUserIsAdmin()
    {
        await SetupAsync();
        string? capturedRole = null;
        _host.MaterialGroupServiceMock
            .Setup(s => s.GetMaterialGroupAsync(It.IsAny<string>()))
            .Callback<string?>(role => capturedRole = role)
            .ReturnsAsync([]);

        await _client.GetAsync("/materialgroups");

        // TestAuthHandler sets ClaimTypes.Role = "Admin" → extractedRole = "Admin"
        Assert.Equal("Admin", capturedRole);
    }

    [Fact]
    public async Task GetMaterialGroups_Returns500_WhenServiceThrows()
    {
        await SetupAsync();
        _host.MaterialGroupServiceMock
            .Setup(s => s.GetMaterialGroupAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("db error"));

        var response = await _client.GetAsync("/materialgroups");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
