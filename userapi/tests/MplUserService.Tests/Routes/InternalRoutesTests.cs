using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MplUserService.Interfaces;
using MplUserService.Routes;
using MplUserService.Tests.Routes.Helpers;

namespace MplUserService.Tests.Routes;

public class InternalRoutesTests : IAsyncDisposable
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    private readonly Mock<IUserService> _userServiceMock = new();

    private async Task SetupAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_userServiceMock.Object);

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

        // Map the "internal" policy to the Admin role so the test user is authorised
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("internal", p => p.RequireRole("Admin")));

        builder.Logging.ClearProviders();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapInternalRoutes();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    // ---------- DELETE /user/{userId} ----------

    [Fact]
    public async Task DeleteUser_Returns200_WhenUserDeleted()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.DeleteUserAsync("user-123")).ReturnsAsync(true);

        var response = await _client.DeleteAsync("/user/user-123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _userServiceMock.Verify(s => s.DeleteUserAsync("user-123"), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Returns404_WhenUserNotFound()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.DeleteUserAsync(It.IsAny<string>())).ReturnsAsync(false);

        var response = await _client.DeleteAsync("/user/nonexistent-user");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.DeleteUserAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("DB failure"));

        var response = await _client.DeleteAsync("/user/user-xyz");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }
}
