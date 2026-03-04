using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;
using MplAuthService.Models.Enums;
using MplAuthService.Routes;
using MplAuthService.Tests.Routes.Helpers;

namespace MplAuthService.Tests.Routes;

public class OrgRoutesTests : IAsyncDisposable
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly Mock<IOrgService> _orgServiceMock = new();

    private async Task SetupAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_orgServiceMock.Object);

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));

        builder.Logging.ClearProviders();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapOrgRoutes();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    // ---------- helpers ----------

    private static Organization MakeOrg(int id = 1, string inn = "1234567890") => new()
    {
        Id = id, Name = "Test Org", Inn = inn,
        SubscriptionType = SubscriptionType.Basic,
        SubscriptionStartDate = DateTime.UtcNow.AddDays(-30),
        SubscriptionEndDate = DateTime.UtcNow.AddDays(30)
    };

    private static OrganizationDto MakeOrgDto(string inn = "1234567890") => new(
        Name: "Test Org", Inn: inn,
        SubscriptionType: SubscriptionType.Basic,
        SubscriptionStartDate: DateTime.UtcNow.AddDays(-30),
        SubscriptionEndDate: DateTime.UtcNow.AddDays(30)
    );

    // ---------- GET /organizations ----------

    [Fact]
    public async Task GetOrganizations_Returns200_WithMappedList()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.GetOrganizations())
            .ReturnsAsync([MakeOrg(1, "111"), MakeOrg(2, "222")]);

        var response = await _client.GetAsync("/organizations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<OrganizationDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetOrganizations_Returns200_WhenEmpty()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.GetOrganizations()).ReturnsAsync([]);

        var response = await _client.GetAsync("/organizations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<OrganizationDto>>();
        Assert.Empty(body!);
    }

    // ---------- GET /organizations/{inn} ----------

    [Fact]
    public async Task GetOrganizationByInn_Returns200_WhenFound()
    {
        await SetupAsync();
        var org = MakeOrg(inn: "9876543210");
        _orgServiceMock.Setup(s => s.GetOrganization("9876543210")).ReturnsAsync(org);

        var response = await _client.GetAsync("/organizations/9876543210");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.Equal("9876543210", body!.Inn);
    }

    [Fact]
    public async Task GetOrganizationByInn_Returns404_WhenNotFound()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.GetOrganization(It.IsAny<string>()))
            .ReturnsAsync((Organization?)null);

        var response = await _client.GetAsync("/organizations/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- GET /organizations/{orgId}/users ----------

    [Fact]
    public async Task GetUsersByOrg_Returns200_WithUserList()
    {
        await SetupAsync();
        var users = new List<UserResponseDto>
        {
            new("u1", "a@example.com", null),
            new("u2", "b@example.com", null)
        };
        _orgServiceMock.Setup(s => s.GetUsersByOrganization(1)).ReturnsAsync(users);

        var response = await _client.GetAsync("/organizations/1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetUsersByOrg_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.GetUsersByOrganization(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var response = await _client.GetAsync("/organizations/99/users");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT /organizations/{id} ----------

    [Fact]
    public async Task UpdateOrganization_Returns200_WhenFound()
    {
        await SetupAsync();
        var resultDto = MakeOrgDto("updated-inn");
        _orgServiceMock.Setup(s => s.UpdateOrganization(1, It.IsAny<OrganizationDto>()))
            .ReturnsAsync(resultDto);

        var response = await _client.PutAsJsonAsync("/organizations/1", MakeOrgDto());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.Equal("updated-inn", body!.Inn);
    }

    [Fact]
    public async Task UpdateOrganization_Returns404_WhenNotFound()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.UpdateOrganization(It.IsAny<int>(), It.IsAny<OrganizationDto>()))
            .ReturnsAsync((OrganizationDto?)null);

        var response = await _client.PutAsJsonAsync("/organizations/999", MakeOrgDto());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrganization_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.UpdateOrganization(It.IsAny<int>(), It.IsAny<OrganizationDto>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var response = await _client.PutAsJsonAsync("/organizations/1", MakeOrgDto());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST /organizations ----------

    [Fact]
    public async Task CreateOrganization_Returns200_WithCreatedDto()
    {
        await SetupAsync();
        var created = MakeOrg(id: 42, inn: "new-inn");
        _orgServiceMock.Setup(s => s.CreateOrganization(It.IsAny<OrganizationDto>()))
            .ReturnsAsync(created);

        var response = await _client.PostAsJsonAsync("/organizations", MakeOrgDto("new-inn"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.Equal("new-inn", body!.Inn);
        Assert.Equal(42, body.Id);
    }

    [Fact]
    public async Task CreateOrganization_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.CreateOrganization(It.IsAny<OrganizationDto>()))
            .ThrowsAsync(new InvalidOperationException("INN already exists"));

        var response = await _client.PostAsJsonAsync("/organizations", MakeOrgDto());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- DELETE /organizations/{id} ----------

    [Fact]
    public async Task DeleteOrganization_Returns200_WhenDeleted()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.DeleteOrganization(1)).ReturnsAsync(true);

        var response = await _client.DeleteAsync("/organizations/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOrganization_Returns404_WhenNotFound()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.DeleteOrganization(It.IsAny<int>())).ReturnsAsync(false);

        var response = await _client.DeleteAsync("/organizations/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOrganization_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _orgServiceMock.Setup(s => s.DeleteOrganization(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var response = await _client.DeleteAsync("/organizations/1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }
}
