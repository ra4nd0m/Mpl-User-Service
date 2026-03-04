using System.Net;
using System.Net.Http.Json;
using MplDbApi.Models.Filters;
using MplDbApi.Models.Dtos;
using MplDbApi.Routes;
using MplDbApi.Tests.Routes.Helpers;

namespace MplDbApi.Tests.Routes;

public class FilterConfigRoutesTests : IAsyncDisposable
{
    private readonly RouteTestHost _host = new();
    private HttpClient _client = null!;

    private async Task SetupAsync()
    {
        await _host.BuildAsync(app => app.MapFilterConfigRoutes());
        _client = _host.Client;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    // --- Helpers ---

    private static FilterCreateReqDto MakeFilterDto(string role = "viewer") =>
        new(role, null, null, null, null, null);

    // ---------- POST /filter-config/filter ----------

    [Fact]
    public async Task ModifyFilter_Returns200_WhenDataIsValid()
    {
        await SetupAsync();

        var response = await _client.PostAsJsonAsync("/filter-config/filter", MakeFilterDto("trader"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ModifyFilter_Returns400_WhenBodyIsNull()
    {
        await SetupAsync();

        // Send an explicitly null body via plain content
        var response = await _client.PostAsync("/filter-config/filter",
            new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        // ASP.NET Core minimal APIs return 400 when required body is null
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ModifyFilter_Returns500_WhenServiceThrows()
    {
        await SetupAsync();

        // Empty AffectedRole triggers InvalidOperationException inside FilterService.ModifyFilter
        var response = await _client.PostAsJsonAsync("/filter-config/filter", MakeFilterDto(role: ""));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ---------- GET /filter-config/filter/{role} ----------

    [Fact]
    public async Task GetFilterByRole_Returns200_WhenRoleExists()
    {
        await SetupAsync();

        // Seed a filter first
        await _client.PostAsJsonAsync("/filter-config/filter", MakeFilterDto("analyst"));

        var response = await _client.GetAsync("/filter-config/filter/analyst");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataFilter>();
        Assert.Equal("analyst", body!.AffectedRole);
    }

    [Fact]
    public async Task GetFilterByRole_Returns200_WhenRoleDoesNotExist()
    {
        await SetupAsync();

        // GetFilterByRole returns an empty DataFilter (not null / not KeyNotFoundException) for unknown roles
        var response = await _client.GetAsync("/filter-config/filter/unknown-role");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- GET /filter-config/filters ----------

    [Fact]
    public async Task GetAllFilters_Returns200_WhenFiltersExist()
    {
        await SetupAsync();

        await _client.PostAsJsonAsync("/filter-config/filter", MakeFilterDto("role1"));
        await _client.PostAsJsonAsync("/filter-config/filter", MakeFilterDto("role2"));

        var response = await _client.GetAsync("/filter-config/filters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<DataFilter>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetAllFilters_Returns200_WhenEmpty()
    {
        await SetupAsync();

        var response = await _client.GetAsync("/filter-config/filters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<DataFilter>>();
        Assert.Empty(body!);
    }
}
