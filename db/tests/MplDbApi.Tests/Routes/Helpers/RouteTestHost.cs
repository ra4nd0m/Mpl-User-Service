using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MplDbApi.Data;
using MplDbApi.Interfaces;
using MplDbApi.Services;

namespace MplDbApi.Tests.Routes.Helpers;

/// <summary>
/// Builds a fully-configured in-process WebApplication for db route integration tests.
/// Interfaces are replaced with Moq mocks; concrete services (FilterService, CacheManagementService)
/// use real instances backed by in-memory providers.
/// </summary>
public sealed class RouteTestHost : IAsyncDisposable
{
    private WebApplication? _app;
    public HttpClient Client { get; private set; } = null!;

    // Interface mocks exposed for test setup / verification
    public Mock<IDeliveryTypeService> DeliveryTypeServiceMock { get; } = new();
    public Mock<ISourceService> SourceServiceMock { get; } = new();
    public Mock<IUnitService> UnitServiceMock { get; } = new();
    public Mock<IMaterialGroupService> MaterialGroupServiceMock { get; } = new();
    public Mock<IMaterialPropService> MaterialPropServiceMock { get; } = new();
    public Mock<IMaterialValueService> MaterialValueServiceMock { get; } = new();
    public Mock<IMaterialSourceService> MaterialSourceServiceMock { get; } = new();

    public async Task<RouteTestHost> BuildAsync(
        Action<WebApplication> mapRoutes,
        Action<IServiceCollection>? configureServices = null)
    {
        var filterOptions = new DbContextOptionsBuilder<FilterContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        // Interface mocks
        builder.Services.AddSingleton(DeliveryTypeServiceMock.Object);
        builder.Services.AddSingleton(SourceServiceMock.Object);
        builder.Services.AddSingleton(UnitServiceMock.Object);
        builder.Services.AddSingleton(MaterialGroupServiceMock.Object);
        builder.Services.AddSingleton(MaterialPropServiceMock.Object);
        builder.Services.AddSingleton(MaterialValueServiceMock.Object);
        builder.Services.AddSingleton(MaterialSourceServiceMock.Object);

        // Concrete services with in-memory backing
        builder.Services.AddSingleton(new FilterContext(filterOptions));
        builder.Services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
        builder.Services.AddScoped<FilterService>();
        builder.Services.AddScoped<CacheManagementService>();

        // Auth / Authz
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("RequireAdmin", p => p.RequireRole("Admin")));

        builder.Logging.ClearProviders();

        configureServices?.Invoke(builder.Services);

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
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        Client?.Dispose();
    }
}
