using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using MplAuthService.Models;
using MplAuthService.Models.Enums;
using MplAuthService.Services;

namespace MplAuthService.Tests.Services;

public class JwtServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ILogger<JwtService>> _loggerMock;
    private readonly JwtService _sut;

    private const string SecretKey = "supersecretkey_that_is_long_enough_for_hmac256";
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";
    private const string ExpiryMinutes = "60";

    public JwtServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns(SecretKey);
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns(Issuer);
        _configMock.Setup(c => c["Jwt:Audience"]).Returns(Audience);
        _configMock.Setup(c => c["Jwt:TokenExpiryMinutes"]).Returns(ExpiryMinutes);

        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _loggerMock = new Mock<ILogger<JwtService>>();

        _sut = new JwtService(_configMock.Object, _userManagerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateJwtToken_ReturnsValidJwtToken()
    {
        var user = new User { Id = "user-1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["User"]);

        var token = await _sut.GenerateJwtToken(user);

        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
    }

    [Fact]
    public async Task GenerateJwtToken_ContainsExpectedClaims()
    {
        var user = new User { Id = "user-1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["User"]);

        var token = await _sut.GenerateJwtToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(user.Id, jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public async Task GenerateJwtToken_WithOrganization_IncludesOrganizationClaims()
    {
        var org = new Organization
        {
            Id = 1,
            Name = "Test Org",
            Inn = "123456789",
            SubscriptionType = SubscriptionType.Basic,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-30),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(30)
        };
        var user = new User
        {
            Id = "user-1",
            Email = "test@example.com",
            UserName = "test@example.com",
            Organization = org,
            OrganizationId = org.Id
        };
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["User"]);

        var token = await _sut.GenerateJwtToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == "OrganizationId" && c.Value == "1");
        Assert.Contains(jwt.Claims, c => c.Type == "SubscriptionType" && c.Value == "Basic");
        Assert.Contains(jwt.Claims, c => c.Type == "SubscriptionEnd");
    }

    [Fact]
    public async Task GenerateJwtToken_WithIndividualSubscription_IncludesSubscriptionClaims()
    {
        var user = new User { Id = "user-1", Email = "test@example.com", UserName = "test@example.com" };
        user.IndividualSubscription = new IndividualSubscription
        {
            Id = 1,
            UserId = user.Id,
            User = user,
            SubscriptionType = SubscriptionType.Premium,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-10),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(20)
        };
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["User"]);

        var token = await _sut.GenerateJwtToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == "SubscriptionType" && c.Value == "Premium");
        Assert.Contains(jwt.Claims, c => c.Type == "SubscriptionEnd");
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "OrganizationId");
    }

    [Fact]
    public async Task GenerateJwtToken_AdminRole_DoesNotIncludeSubscriptionClaims()
    {
        var user = new User { Id = "admin-1", Email = "admin@example.com", UserName = "admin@example.com" };
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var token = await _sut.GenerateJwtToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == "SubscriptionType");
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "SubscriptionEnd");
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "OrganizationId");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public async Task GenerateJwtToken_TokenIsSignedWithCorrectKey()
    {
        var user = new User { Id = "user-1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["User"]);

        var token = await _sut.GenerateJwtToken(user);

        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = handler.ValidateToken(token, validationParams, out _);
        Assert.NotNull(principal);
    }

    [Fact]
    public async Task GenerateJwtToken_ThrowsWhenJwtKeyNotConfigured()
    {
        _configMock.Setup(c => c["Jwt:Key"]).Returns((string?)null);

        var user = new User { Id = "user-1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["User"]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GenerateJwtToken(user));
    }

    [Fact]
    public void GenerateInternalToken_ReturnsValidToken()
    {
        var token = _sut.GenerateInternalToken();

        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
    }

    [Fact]
    public void GenerateInternalToken_ContainsInternalRole()
    {
        var token = _sut.GenerateInternalToken();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "internal");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == "internal");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "internal");
    }

    [Fact]
    public void GenerateInternalToken_ThrowsWhenJwtKeyNotConfigured()
    {
        _configMock.Setup(c => c["Jwt:Key"]).Returns((string?)null);

        Assert.Throws<InvalidOperationException>(() => _sut.GenerateInternalToken());
    }
}
