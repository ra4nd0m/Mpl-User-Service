using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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

public class UserManagementRoutesTests : IAsyncDisposable
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<UserManager<User>> _userManagerMock;

    public UserManagementRoutesTests()
    {
        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private async Task SetupAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_userServiceMock.Object);
        builder.Services.AddSingleton(_userManagerMock.Object);

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));

        builder.Logging.ClearProviders();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapUserManagementRoutes();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    // ---------- helpers ----------

    private static Organization MakeOrg(int id = 1) => new()
    {
        Id = id,
        Name = "Org",
        Inn = "1234567890",
        SubscriptionType = SubscriptionType.Basic,
        SubscriptionStartDate = DateTime.UtcNow.AddDays(-30),
        SubscriptionEndDate = DateTime.UtcNow.AddDays(30)
    };

    private static User UserWithOrg(string id = "u1") => new()
    {
        Id = id,
        Email = "user@example.com",
        UserName = "user@example.com",
        Organization = new Organization
        {
            Id = 1,
            Name = "Org",
            Inn = "1234567890",
            SubscriptionType = SubscriptionType.Basic,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-30),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(30)
        }
    };

    private static User UserWithSub(string id = "u2") => new()
    {
        Id = id,
        Email = "sub@example.com",
        UserName = "sub@example.com",
        IndividualSubscription = null // will be assigned below
    };

    // ---------- POST /register ----------

    [Fact]
    public async Task Register_Returns200_WhenUserCreatedWithOrg()
    {
        await SetupAsync();
        var user = UserWithOrg();
        _userServiceMock.Setup(s => s.CreateUser(It.IsAny<CreateUserDto>())).ReturnsAsync(user);

        var dto = new CreateUserDto("user@example.com", "Pass1!", new OrganizationDto(
            "Org", "1234567890", SubscriptionType.Basic,
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(30)));

        var response = await _client.PostAsJsonAsync("/register", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("user@example.com", body.Email);
        Assert.NotNull(body.Org);
    }

    [Fact]
    public async Task Register_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.CreateUser(It.IsAny<CreateUserDto>()))
            .ThrowsAsync(new InvalidOperationException("User already exists"));

        var dto = new CreateUserDto("dup@example.com", "Pass1!", new OrganizationDto(
            "Org", "1234567890", SubscriptionType.Basic,
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(30)));

        var response = await _client.PostAsJsonAsync("/register", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT /users/{email} ----------

    [Fact]
    public async Task UpdateUser_Returns200WithOrgDto_WhenUserHasOrg()
    {
        await SetupAsync();
        var user = UserWithOrg();
        var updated = UserWithOrg();

        _userServiceMock.Setup(s => s.GetUserByEmail("user@example.com")).ReturnsAsync(user);
        _userServiceMock.Setup(s => s.UpdateUser(user, It.IsAny<UpdateUserDto>())).ReturnsAsync(updated);

        var updateDto = new UpdateUserDto(null, null, new OrganizationDto(
            "Org", "1234567890", SubscriptionType.Basic,
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(30)), null);

        var response = await _client.PutAsJsonAsync("/users/user@example.com", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.NotNull(body!.Org);
    }

    [Fact]
    public async Task UpdateUser_Returns200WithSubDto_WhenUserHasIndividualSub()
    {
        await SetupAsync();
        var user = new User { Id = "u3", Email = "ind@example.com", UserName = "ind@example.com" };
        var sub = new IndividualSubscription
        {
            Id = 1,
            UserId = user.Id,
            User = user,
            SubscriptionType = SubscriptionType.Premium,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-5),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(25)
        };
        user.IndividualSubscription = sub;

        _userServiceMock.Setup(s => s.GetUserByEmail("ind@example.com")).ReturnsAsync(user);
        _userServiceMock.Setup(s => s.UpdateUser(user, It.IsAny<UpdateUserDto>())).ReturnsAsync(user);

        var updateDto = new UpdateUserDto(null, null, null,
            new SubscriptionDataDto(SubscriptionType.Premium,
                DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(25)));

        var response = await _client.PutAsJsonAsync("/users/ind@example.com", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.NotNull(body!.Sub);
    }

    [Fact]
    public async Task UpdateUser_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.GetUserByEmail(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("User not found"));

        var updateDto = new UpdateUserDto(null, null, null, null);

        var response = await _client.PutAsJsonAsync("/users/missing@example.com", updateDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /users ----------

    [Fact]
    public async Task GetUsers_Returns200WithMappedList()
    {
        await SetupAsync();
        var users = new List<User>
        {
            UserWithOrg("u1"),
            new() { Id = "u2", Email = "plain@example.com", UserName = "plain@example.com" }
        };
        _userServiceMock.Setup(s => s.GetUsers()).ReturnsAsync(users);

        var response = await _client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetUsers_Returns200_WhenListEmpty()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.GetUsers()).ReturnsAsync([]);

        var response = await _client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();
        Assert.Empty(body!);
    }

    // ---------- GET /users/{email} ----------

    [Fact]
    public async Task GetUserByEmail_Returns200_WhenFound()
    {
        await SetupAsync();
        var user = UserWithOrg();
        _userServiceMock.Setup(s => s.GetUserByEmail("user@example.com")).ReturnsAsync(user);

        var response = await _client.GetAsync("/users/user@example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.Equal("user@example.com", body!.Email);
    }

    [Fact]
    public async Task GetUserByEmail_Returns404_WhenServiceReturnsNull()
    {
        await SetupAsync();
        _userServiceMock.Setup(s => s.GetUserByEmail(It.IsAny<string>()))
            .ReturnsAsync((User)null!);

        var response = await _client.GetAsync("/users/missing@example.com");

        // The route checks `if (user == null) return Results.NotFound()`
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- DELETE /users/{email} ----------

    [Fact]
    public async Task DeleteUser_Returns200_WhenDeleted()
    {
        await SetupAsync();
        var user = new User { Id = "del1", Email = "del@example.com", UserName = "del@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("del@example.com")).ReturnsAsync(user);
        _userServiceMock.Setup(s => s.DeleteUser(user)).Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync("/users/del@example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _userServiceMock.Verify(s => s.DeleteUser(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Returns404_WhenUserNotFound()
    {
        await SetupAsync();
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var response = await _client.DeleteAsync("/users/ghost@example.com");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        var user = new User { Id = "ex1", Email = "ex@example.com", UserName = "ex@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("ex@example.com")).ReturnsAsync(user);
        _userServiceMock.Setup(s => s.DeleteUser(user)).ThrowsAsync(new InvalidOperationException("fail"));

        var response = await _client.DeleteAsync("/users/ex@example.com");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }
}
