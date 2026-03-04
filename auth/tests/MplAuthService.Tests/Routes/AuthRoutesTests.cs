using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;
using MplAuthService.Models.Enums;
using MplAuthService.Routes;
using MplAuthService.Tests.Routes.Helpers;

namespace MplAuthService.Tests.Routes;

public class AuthRoutesTests : IAsyncDisposable
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private AuthContext _db = null!;

    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();

    public AuthRoutesTests()
    {
        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private async Task SetupAsync()
    {
        var dbName = Guid.NewGuid().ToString();

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddDbContext<AuthContext>(opts =>
            opts.UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        builder.Services.AddSingleton(_userManagerMock.Object);
        builder.Services.AddSingleton(_jwtServiceMock.Object);
        builder.Services.AddSingleton(_refreshTokenServiceMock.Object);

        // Auth/Authz not required by AuthRoutes endpoints themselves, but the pipeline must be wired
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        builder.Logging.ClearProviders();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapAuthRoutes();

        await _app.StartAsync();
        _client = _app.GetTestClient();

        // Seed via scoped context
        using var scope = _app.Services.CreateScope();
        _db = scope.ServiceProvider.GetRequiredService<AuthContext>();
    }

    // ---------- /login ----------

    [Fact]
    public async Task Login_Returns200WithToken_WhenCredentialsValid()
    {
        await SetupAsync();

        var user = new User { Id = "u1", Email = "test@example.com", UserName = "test@example.com" };
        await SeedUserAsync(user);

        _userManagerMock.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "Pass1!")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(["User"]);
        _jwtServiceMock.Setup(j => j.GenerateJwtToken(It.IsAny<User>())).ReturnsAsync("jwt-token");
        _refreshTokenServiceMock.Setup(r => r.GenerateRefreshToken(It.IsAny<User>()))
            .ReturnsAsync(new RefreshToken
            {
                Token = "rt-token", UserId = user.Id, User = user,
                Expires = DateTime.UtcNow.AddDays(7)
            });

        var response = await _client.PostAsJsonAsync("/login", new LoginDto("test@example.com", "Pass1!", false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.Equal("jwt-token", body!.Token);
        // Verify the refresh-token cookie was set
        Assert.Contains(response.Headers, h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_Returns400_WhenUserNotFound()
    {
        await SetupAsync();
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var response = await _client.PostAsJsonAsync("/login", new LoginDto("nobody@example.com", "X", false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns400_WhenWrongPassword()
    {
        await SetupAsync();
        var user = new User { Id = "u2", Email = "user@example.com", UserName = "user@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("user@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var response = await _client.PostAsJsonAsync("/login", new LoginDto("user@example.com", "wrong", false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns403_WhenOrgSubscriptionExpired()
    {
        await SetupAsync();

        var org = new Organization
        {
            Id = 1, Name = "TestOrg", Inn = "111",
            SubscriptionType = SubscriptionType.Basic,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-60),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(-1) // expired
        };
        var user = new User
        {
            Id = "u3", Email = "expired@example.com", UserName = "expired@example.com",
            Organization = org, OrganizationId = org.Id
        };
        await SeedUserAsync(user, org);

        _userManagerMock.Setup(m => m.FindByEmailAsync("expired@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "Pass1!")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(["User"]);

        var response = await _client.PostAsJsonAsync("/login", new LoginDto("expired@example.com", "Pass1!", false));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns403_WhenIndividualSubscriptionExpired()
    {
        await SetupAsync();

        var user = new User { Id = "u4", Email = "ind@example.com", UserName = "ind@example.com" };
        user.IndividualSubscription = new IndividualSubscription
        {
            Id = 1, UserId = user.Id, User = user,
            SubscriptionType = SubscriptionType.Premium,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-10),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(-1)
        };
        await SeedUserAsync(user);

        _userManagerMock.Setup(m => m.FindByEmailAsync("ind@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "Pass1!")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(["User"]);

        var response = await _client.PostAsJsonAsync("/login", new LoginDto("ind@example.com", "Pass1!", false));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_SkipsSubscriptionCheck_ForAdmin()
    {
        await SetupAsync();

        var user = new User { Id = "admin1", Email = "admin@example.com", UserName = "admin@example.com" };
        await SeedUserAsync(user);

        _userManagerMock.Setup(m => m.FindByEmailAsync("admin@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "AdminPass1!")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(["Admin"]);
        _jwtServiceMock.Setup(j => j.GenerateJwtToken(It.IsAny<User>())).ReturnsAsync("admin-jwt");
        _refreshTokenServiceMock.Setup(r => r.GenerateRefreshToken(It.IsAny<User>()))
            .ReturnsAsync(new RefreshToken
            {
                Token = "rt-admin", UserId = user.Id, User = user,
                Expires = DateTime.UtcNow.AddDays(7)
            });

        var response = await _client.PostAsJsonAsync("/login", new LoginDto("admin@example.com", "AdminPass1!", false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- /refresh ----------

    [Fact]
    public async Task Refresh_Returns401_WhenNoCookie()
    {
        await SetupAsync();

        var response = await _client.PostAsync("/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_Returns401_WhenRefreshTokenInvalid()
    {
        await SetupAsync();
        _refreshTokenServiceMock.Setup(r => r.ValidateRefreshToken(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);

        var request = new HttpRequestMessage(HttpMethod.Post, "/refresh");
        request.Headers.Add("Cookie", "refreshToken=invalid-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_Returns200_WhenTokenValid()
    {
        await SetupAsync();

        var user = new User { Id = "u5", Email = "refresh@example.com", UserName = "refresh@example.com" };
        await SeedUserAsync(user);

        var rt = new RefreshToken
        {
            Token = "valid-rt", UserId = user.Id, User = user,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        _refreshTokenServiceMock.Setup(r => r.ValidateRefreshToken("valid-rt")).ReturnsAsync(rt);
        _userManagerMock.Setup(m => m.FindByIdAsync("u5")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(["User"]);
        _jwtServiceMock.Setup(j => j.GenerateJwtToken(It.IsAny<User>())).ReturnsAsync("new-jwt");

        var request = new HttpRequestMessage(HttpMethod.Post, "/refresh");
        request.Headers.Add("Cookie", "refreshToken=valid-rt");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.Equal("new-jwt", body!.Token);
    }

    [Fact]
    public async Task Refresh_Returns403_WhenActiveSubscriptionExpired()
    {
        await SetupAsync();

        var org = new Organization
        {
            Id = 2, Name = "OrgExp", Inn = "222",
            SubscriptionType = SubscriptionType.Basic,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-60),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(-1)
        };
        var user = new User
        {
            Id = "u6", Email = "refexp@example.com", UserName = "refexp@example.com",
            Organization = org, OrganizationId = org.Id
        };
        await SeedUserAsync(user, org);

        var rt = new RefreshToken
        {
            Token = "exp-rt", UserId = user.Id, User = user,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        _refreshTokenServiceMock.Setup(r => r.ValidateRefreshToken("exp-rt")).ReturnsAsync(rt);
        _userManagerMock.Setup(m => m.FindByIdAsync("u6")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(["User"]);

        var request = new HttpRequestMessage(HttpMethod.Post, "/refresh");
        request.Headers.Add("Cookie", "refreshToken=exp-rt");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- /logout ----------

    [Fact]
    public async Task Logout_Returns200_WithCookie()
    {
        await SetupAsync();
        _refreshTokenServiceMock.Setup(r => r.RecallRefreshToken("some-token")).Returns(Task.CompletedTask);

        var request = new HttpRequestMessage(HttpMethod.Post, "/logout");
        request.Headers.Add("Cookie", "refreshToken=some-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _refreshTokenServiceMock.Verify(r => r.RecallRefreshToken("some-token"), Times.Once);
    }

    [Fact]
    public async Task Logout_Returns200_WithoutCookie()
    {
        await SetupAsync();

        var response = await _client.PostAsync("/logout", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _refreshTokenServiceMock.Verify(r => r.RecallRefreshToken(It.IsAny<string>()), Times.Never);
    }

    // ---------- Helpers ----------

    private async Task SeedUserAsync(User user, Organization? org = null)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthContext>();

        if (org != null && !await db.Organizations.AnyAsync(o => o.Id == org.Id))
        {
            db.Organizations.Add(org);
        }

        if (!await db.Users.AnyAsync(u => u.Id == user.Id))
        {
            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    private record TokenResponse(string Token);
}
