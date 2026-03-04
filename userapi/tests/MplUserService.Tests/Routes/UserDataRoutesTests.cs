using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MplUserService.Data;
using MplUserService.Interfaces;
using MplUserService.Models;
using MplUserService.Routes;
using MplUserService.Tests.Routes.Helpers;

namespace MplUserService.Tests.Routes;

public class UserDataRoutesTests : IAsyncDisposable
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private UserContext _db = null!;

    private readonly Mock<IUserService> _userServiceMock = new();

    private async Task SetupAsync()
    {
        var dbOptions = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new UserContext(dbOptions);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_db);
        builder.Services.AddSingleton(_userServiceMock.Object);

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        builder.Logging.ClearProviders();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapUserDataRoutes();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    // ---------- helpers ----------

    private User MakeTrackedUser(string id = "user-id", List<int>? favorites = null, string? settings = null)
    {
        var user = new User { Id = id, FavouriteIds = favorites ?? [], SettingsJson = settings };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    // ---------- GET /favorites ----------

    [Fact]
    public async Task GetFavorites_Returns200_WithFavoriteIds()
    {
        await SetupAsync();
        var user = MakeTrackedUser(favorites: [1, 2, 3]);
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var response = await _client.GetAsync("/favorites");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<int>>();
        Assert.Equal([1, 2, 3], body);
    }

    [Fact]
    public async Task GetFavorites_Returns200_WhenListIsEmpty()
    {
        await SetupAsync();
        var user = MakeTrackedUser(favorites: []);
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var response = await _client.GetAsync("/favorites");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<int>>();
        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetFavorites_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ThrowsAsync(new InvalidOperationException("Service failure"));

        var response = await _client.GetAsync("/favorites");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT /favorites/{itemId} ----------

    [Fact]
    public async Task AddFavorite_Returns200_WithUpdatedList()
    {
        await SetupAsync();
        var user = MakeTrackedUser(favorites: [1, 2]);
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var response = await _client.PutAsync("/favorites/3", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<int>>();
        Assert.Contains(3, body!);
    }

    [Fact]
    public async Task AddFavorite_Returns200_WhenItemAlreadyPresent_NoDuplicate()
    {
        await SetupAsync();
        var user = MakeTrackedUser(favorites: [1, 2]);
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var response = await _client.PutAsync("/favorites/2", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<int>>();
        Assert.NotNull(body);
        // The route guards with !Contains, so 2 appears exactly once
        Assert.Single(body, x => x == 2);
    }

    [Fact]
    public async Task AddFavorite_Returns400_WhenItemIdIsZero()
    {
        await SetupAsync();

        var response = await _client.PutAsync("/favorites/0", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddFavorite_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ThrowsAsync(new InvalidOperationException("Service failure"));

        var response = await _client.PutAsync("/favorites/5", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- DELETE /favorites/{itemId} ----------

    [Fact]
    public async Task RemoveFavorite_Returns200_WithUpdatedList()
    {
        await SetupAsync();
        var user = MakeTrackedUser(favorites: [1, 2, 3]);
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var response = await _client.DeleteAsync("/favorites/2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<int>>();
        Assert.DoesNotContain(2, body!);
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task RemoveFavorite_Returns200_WhenItemNotPresent()
    {
        await SetupAsync();
        var user = MakeTrackedUser(favorites: [1, 3]);
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var response = await _client.DeleteAsync("/favorites/99");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<int>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task RemoveFavorite_Returns400_WhenItemIdIsZero()
    {
        await SetupAsync();

        var response = await _client.DeleteAsync("/favorites/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveFavorite_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ThrowsAsync(new InvalidOperationException("Service failure"));

        var response = await _client.DeleteAsync("/favorites/1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST /favorites ----------

    [Fact]
    public async Task SetFavorites_Returns200_WithNewList()
    {
        await SetupAsync();
        var user = MakeTrackedUser(favorites: [1, 2]);
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var content = new StringContent("[10, 20, 30]", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/favorites", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<int>>();
        Assert.Equal([10, 20, 30], body);
    }

    [Fact]
    public async Task SetFavorites_Returns400_WhenEmptyList()
    {
        await SetupAsync();
        var user = MakeTrackedUser();
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var content = new StringContent("[]", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/favorites", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetFavorites_Returns400_WhenInvalidJson()
    {
        await SetupAsync();

        var content = new StringContent("not valid json", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/favorites", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetFavorites_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ThrowsAsync(new InvalidOperationException("Service failure"));

        var content = new StringContent("[1, 2]", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/favorites", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /settings ----------

    [Fact]
    public async Task GetSettings_Returns200_WhenSettingsExist()
    {
        await SetupAsync();
        var user = MakeTrackedUser(settings: "{\"theme\":\"dark\"}");
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var response = await _client.GetAsync("/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSettings_Returns404_WhenNoSettings()
    {
        await SetupAsync();
        var user = MakeTrackedUser(settings: null);
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var response = await _client.GetAsync("/settings");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSettings_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ThrowsAsync(new InvalidOperationException("Service failure"));

        var response = await _client.GetAsync("/settings");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT /settings ----------

    [Fact]
    public async Task UpdateSettings_Returns200_WhenValidJson()
    {
        await SetupAsync();
        var user = MakeTrackedUser();
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var content = new StringContent("{\"theme\":\"light\",\"lang\":\"en\"}", Encoding.UTF8, "application/json");
        var response = await _client.PutAsync("/settings", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_Returns400_WhenInvalidJson()
    {
        await SetupAsync();

        var content = new StringContent("not valid json", Encoding.UTF8, "application/json");
        var response = await _client.PutAsync("/settings", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ThrowsAsync(new InvalidOperationException("Service failure"));

        var content = new StringContent("{\"theme\":\"dark\"}", Encoding.UTF8, "application/json");
        var response = await _client.PutAsync("/settings", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
        _db?.Dispose();
    }
}
