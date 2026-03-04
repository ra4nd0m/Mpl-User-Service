using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.Logging;
using Moq;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;
using MplAuthService.Models.Enums;
using MplAuthService.Services;

namespace MplAuthService.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly AuthContext _context;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IOrgService> _orgServiceMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new AuthContext(options);

        var userStore = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object, null!, null!, null!, null!);

        _loggerMock = new Mock<ILogger<UserService>>();
        _jwtServiceMock = new Mock<IJwtService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _orgServiceMock = new Mock<IOrgService>();

        _sut = new UserService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _context,
            _loggerMock.Object,
            _jwtServiceMock.Object,
            _httpClientFactoryMock.Object,
            _orgServiceMock.Object
        );
    }

    // --- Helpers ---

    private static Organization MakeOrg(int id = 1) => new()
    {
        Id = id,
        Name = "Test Org",
        Inn = "1234567890",
        SubscriptionType = SubscriptionType.Basic,
        SubscriptionStartDate = DateTime.UtcNow.AddDays(-30),
        SubscriptionEndDate = DateTime.UtcNow.AddDays(30)
    };

    private static OrganizationDto MakeOrgDto(string inn = "1234567890") => new(
        Name: "Test Org",
        Inn: inn,
        SubscriptionType: SubscriptionType.Basic,
        SubscriptionStartDate: DateTime.UtcNow.AddDays(-30),
        SubscriptionEndDate: DateTime.UtcNow.AddDays(30)
    );

    private static SubscriptionDataDto MakeSubDto() => new(
        SubscriptionType: SubscriptionType.Premium,
        SubscriptionStartDate: DateTime.UtcNow.AddDays(-10),
        SubscriptionEndDate: DateTime.UtcNow.AddDays(20)
    );

    private void SetupUserManagerCreate(IdentityResult result)
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(result);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
    }

    // --- CreateUser ---

    [Fact]
    public async Task CreateUser_Throws_WhenUserAlreadyExists()
    {
        var existingUser = new User { Id = "existing-id", Email = "existing@example.com", UserName = "existing@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("existing@example.com"))
            .ReturnsAsync(existingUser);

        var dto = new CreateUserDto("existing@example.com", "Password1!", MakeOrgDto());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateUser(dto));
    }

    [Fact]
    public async Task CreateUser_Throws_WhenNeitherOrgNorSubProvided()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var dto = new CreateUserDto("new@example.com", "Password1!");

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateUser(dto));
    }

    [Fact]
    public async Task CreateUser_Throws_WhenBothOrgAndSubProvided()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var dto = new CreateUserDto("new@example.com", "Password1!", MakeOrgDto(), MakeSubDto());

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateUser(dto));
    }

    [Fact]
    public async Task CreateUser_WithOrganization_ReturnsUser()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        SetupUserManagerCreate(IdentityResult.Success);
        _roleManagerMock.Setup(m => m.RoleExistsAsync("User")).ReturnsAsync(true);

        var org = MakeOrg();
        _orgServiceMock.Setup(m => m.GetOrganization("1234567890")).ReturnsAsync((Organization?)null);
        _orgServiceMock.Setup(m => m.CreateOrganization(It.IsAny<OrganizationDto>())).ReturnsAsync(org);

        var dto = new CreateUserDto("new@example.com", "Password1!", MakeOrgDto());

        var result = await _sut.CreateUser(dto);

        Assert.NotNull(result);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal(org, result.Organization);
    }

    [Fact]
    public async Task CreateUser_UsesExistingOrg_WhenInnAlreadyExists()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        SetupUserManagerCreate(IdentityResult.Success);
        _roleManagerMock.Setup(m => m.RoleExistsAsync("User")).ReturnsAsync(true);

        var existingOrg = MakeOrg();
        _orgServiceMock.Setup(m => m.GetOrganization("1234567890")).ReturnsAsync(existingOrg);

        var dto = new CreateUserDto("new@example.com", "Password1!", MakeOrgDto());

        var result = await _sut.CreateUser(dto);

        Assert.Equal(existingOrg, result.Organization);
        _orgServiceMock.Verify(m => m.CreateOrganization(It.IsAny<OrganizationDto>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_WithIndividualSubscription_SetsSubscription()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        SetupUserManagerCreate(IdentityResult.Success);
        _roleManagerMock.Setup(m => m.RoleExistsAsync("User")).ReturnsAsync(true);

        var subDto = MakeSubDto();
        var dto = new CreateUserDto("new@example.com", "Password1!", Sub: subDto);

        var result = await _sut.CreateUser(dto);

        Assert.NotNull(result.IndividualSubscription);
        Assert.Equal(SubscriptionType.Premium, result.IndividualSubscription.SubscriptionType);
    }

    [Fact]
    public async Task CreateUser_CreatesRoleIfMissing()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        SetupUserManagerCreate(IdentityResult.Success);
        _roleManagerMock.Setup(m => m.RoleExistsAsync("User")).ReturnsAsync(false);
        _roleManagerMock.Setup(m => m.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        var org = MakeOrg();
        _orgServiceMock.Setup(m => m.GetOrganization(It.IsAny<string>())).ReturnsAsync((Organization?)null);
        _orgServiceMock.Setup(m => m.CreateOrganization(It.IsAny<OrganizationDto>())).ReturnsAsync(org);

        var dto = new CreateUserDto("new@example.com", "Password1!", MakeOrgDto());

        await _sut.CreateUser(dto);

        _roleManagerMock.Verify(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == "User")), Times.Once);
    }

    [Fact]
    public async Task CreateUser_ThrowsAndRollsBack_WhenCreateFails()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));
        _roleManagerMock.Setup(m => m.RoleExistsAsync("User")).ReturnsAsync(true);

        var org = MakeOrg();
        _orgServiceMock.Setup(m => m.GetOrganization(It.IsAny<string>())).ReturnsAsync((Organization?)null);
        _orgServiceMock.Setup(m => m.CreateOrganization(It.IsAny<OrganizationDto>())).ReturnsAsync(org);

        var dto = new CreateUserDto("new@example.com", "weak", MakeOrgDto());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateUser(dto));
    }

    // --- CreateAdmin ---

    [Fact]
    public async Task CreateAdmin_ReturnsAdmin()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("admin@example.com"))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), "AdminPass1!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Admin"))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(m => m.RoleExistsAsync("Admin")).ReturnsAsync(true);

        var result = await _sut.CreateAdmin("admin@example.com", "AdminPass1!");

        Assert.NotNull(result);
        Assert.Equal("admin@example.com", result.Email);
    }

    [Fact]
    public async Task CreateAdmin_Throws_WhenUserAlreadyExists()
    {
        var existing = new User { Id = "a1", Email = "admin@example.com", UserName = "admin@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("admin@example.com")).ReturnsAsync(existing);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAdmin("admin@example.com", "AdminPass1!"));
    }

    [Fact]
    public async Task CreateAdmin_CreatesAdminRole_WhenMissing()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(m => m.RoleExistsAsync("Admin")).ReturnsAsync(false);
        _roleManagerMock.Setup(m => m.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        await _sut.CreateAdmin("admin@example.com", "AdminPass1!");

        _roleManagerMock.Verify(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == "Admin")), Times.Once);
    }

    // --- GetUserByEmail ---

    [Fact]
    public async Task GetUserByEmail_Throws_WhenEmailIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetUserByEmail(""));
    }

    [Fact]
    public async Task GetUserByEmail_Throws_WhenUserNotFoundInIdentity()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetUserByEmail("missing@example.com"));
    }

    [Fact]
    public async Task GetUserByEmail_ReturnsUser_WhenFound()
    {
        var user = new User { Id = "u1", Email = "found@example.com", UserName = "found@example.com" };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _userManagerMock.Setup(m => m.FindByEmailAsync("found@example.com")).ReturnsAsync(user);

        var result = await _sut.GetUserByEmail("found@example.com");

        Assert.NotNull(result);
        Assert.Equal("found@example.com", result.Email);
    }

    // --- GetUsers ---

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        var users = new[]
        {
            new User { Id = "u1", Email = "a@example.com", UserName = "a@example.com" },
            new User { Id = "u2", Email = "b@example.com", UserName = "b@example.com" }
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var result = await _sut.GetUsers();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUsers_ReturnsEmpty_WhenNoUsers()
    {
        var result = await _sut.GetUsers();

        Assert.Empty(result);
    }

    // --- DeleteUser ---

    [Fact]
    public async Task DeleteUser_DeletesUser_WhenNoRefreshTokensOrSubscriptions()
    {
        var user = new User { Id = "u1", Email = "delete@example.com", UserName = "delete@example.com" };

        _jwtServiceMock.Setup(j => j.GenerateInternalToken()).Returns("internal-jwt");

        var httpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new System.Net.Http.HttpClient(httpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        _httpClientFactoryMock.Setup(f => f.CreateClient("ExternalUserApi")).Returns(httpClient);

        _userManagerMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _sut.DeleteUser(user);

        _userManagerMock.Verify(m => m.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_RemovesRefreshTokensBeforeDelete()
    {
        var user = new User { Id = "u-del", Email = "del@example.com", UserName = "del@example.com" };
        await _context.Users.AddAsync(user);
        var rt = new RefreshToken
        {
            Token = "rt-token",
            UserId = user.Id,
            User = user,
            Expires = DateTime.UtcNow.AddDays(1)
        };
        await _context.RefreshTokens.AddAsync(rt);
        await _context.SaveChangesAsync();

        _jwtServiceMock.Setup(j => j.GenerateInternalToken()).Returns("internal-jwt");
        var httpClient = new System.Net.Http.HttpClient(new Mock<HttpMessageHandler>().Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        _httpClientFactoryMock.Setup(f => f.CreateClient("ExternalUserApi")).Returns(httpClient);
        _userManagerMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _sut.DeleteUser(user);

        Assert.Equal(0, await _context.RefreshTokens.CountAsync(t => t.UserId == user.Id));
    }

    [Fact]
    public async Task DeleteUser_Throws_WhenIdentityDeleteFails()
    {
        var user = new User { Id = "u-fail", Email = "fail@example.com", UserName = "fail@example.com" };

        _jwtServiceMock.Setup(j => j.GenerateInternalToken()).Returns("internal-jwt");
        var httpClient = new System.Net.Http.HttpClient(new Mock<HttpMessageHandler>().Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        _httpClientFactoryMock.Setup(f => f.CreateClient("ExternalUserApi")).Returns(httpClient);
        _userManagerMock.Setup(m => m.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Cannot delete" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteUser(user));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
