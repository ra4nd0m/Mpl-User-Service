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

namespace MplAuthService.Tests.Routes.Helpers;

/// <summary>
/// Builds a fully-configured in-process WebApplication for route integration tests.
/// Services that are not supplied are replaced with no-op mocks automatically.
/// </summary>
public sealed class RouteTestHost : IAsyncDisposable
{
    private WebApplication? _app;
    public HttpClient Client { get; private set; } = null!;
    public AuthContext DbContext { get; private set; } = null!;

    // Mocks exposed for test setup / verification
    public Mock<UserManager<User>> UserManagerMock { get; } = CreateUserManagerMock();
    public Mock<IJwtService> JwtServiceMock { get; } = new();
    public Mock<IRefreshTokenService> RefreshTokenServiceMock { get; } = new();
    public Mock<IUserService> UserServiceMock { get; } = new();
    public Mock<IOrgService> OrgServiceMock { get; } = new();

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    /// <summary>
    /// Builds the host, calls the provided map delegate to register routes, then starts the server.
    /// </summary>
    public async Task<RouteTestHost> BuildAsync(Action<WebApplication> mapRoutes)
    {
        var dbName = Guid.NewGuid().ToString();
        var dbOptions = new DbContextOptionsBuilder<AuthContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        // Keep a public reference so tests can seed data
        DbContext = new AuthContext(dbOptions);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        // Use the shared InMemory context instance
        builder.Services.AddSingleton(DbContext);
        builder.Services.AddSingleton<AuthContext>(sp => sp.GetRequiredService<AuthContext>());

        // Identity (needed because routes inject UserManager directly)
        builder.Services.AddIdentityCore<User>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AuthContext>();

        // Replace Identity's UserManager with our mock so we control it from tests
        builder.Services.AddSingleton(UserManagerMock.Object);

        // Mocked services
        builder.Services.AddSingleton(JwtServiceMock.Object);
        builder.Services.AddSingleton(RefreshTokenServiceMock.Object);
        builder.Services.AddSingleton(UserServiceMock.Object);
        builder.Services.AddSingleton(OrgServiceMock.Object);

        // HttpClientFactory (needed by UserService internally but not by routes directly)
        builder.Services.AddHttpClient();

        // Test auth – every request is auto-authenticated as Admin
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));

        builder.Logging.ClearProviders();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();

        mapRoutes(_app);

        await _app.StartAsync();
        Client = _app.GetTestClient();

        return this;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
        DbContext.Dispose();
    }
}
