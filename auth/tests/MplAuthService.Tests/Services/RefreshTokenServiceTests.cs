using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using MplAuthService.Data;
using MplAuthService.Models;
using MplAuthService.Services;

namespace MplAuthService.Tests.Services;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly AuthContext _context;
    private readonly Mock<IConfiguration> _configMock;
    private readonly RefreshTokenService _sut;

    public RefreshTokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AuthContext(options);

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");

        _sut = new RefreshTokenService(_configMock.Object, _context);
    }

    private static User MakeUser(string id = "user-1") =>
        new() { Id = id, Email = $"{id}@example.com", UserName = $"{id}@example.com" };

    [Fact]
    public async Task GenerateRefreshToken_ReturnsSavedToken()
    {
        var user = MakeUser();

        var token = await _sut.GenerateRefreshToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token.Token);
        Assert.Equal(user.Id, token.UserId);
    }

    [Fact]
    public async Task GenerateRefreshToken_TokenIsPersisted()
    {
        var user = MakeUser();

        var token = await _sut.GenerateRefreshToken(user);

        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token.Token);
        Assert.NotNull(stored);
        Assert.Equal(user.Id, stored.UserId);
    }

    [Fact]
    public async Task GenerateRefreshToken_ExpiresAfterConfiguredDays()
    {
        _configMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");
        var user = MakeUser();

        var token = await _sut.GenerateRefreshToken(user);

        var expectedExpiry = DateTime.UtcNow.AddDays(7);
        Assert.True(token.Expires > DateTime.UtcNow);
        Assert.True(token.Expires <= expectedExpiry.AddSeconds(5));
    }

    [Fact]
    public async Task ValidateRefreshToken_ReturnsToken_WhenValid()
    {
        var user = MakeUser();
        var generated = await _sut.GenerateRefreshToken(user);

        var result = await _sut.ValidateRefreshToken(generated.Token);

        Assert.NotNull(result);
        Assert.Equal(generated.Token, result.Token);
    }

    [Fact]
    public async Task ValidateRefreshToken_ReturnsNull_WhenTokenNotFound()
    {
        var result = await _sut.ValidateRefreshToken("nonexistent-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateRefreshToken_ReturnsNull_WhenTokenExpired()
    {
        var user = MakeUser();
        var expiredToken = new RefreshToken
        {
            Token = "expired-token",
            UserId = user.Id,
            User = user,
            Expires = DateTime.UtcNow.AddDays(-1)
        };
        await _context.RefreshTokens.AddAsync(expiredToken);
        await _context.SaveChangesAsync();

        var result = await _sut.ValidateRefreshToken("expired-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task RecallRefreshToken_ExpiresTheToken()
    {
        var user = MakeUser();
        var generated = await _sut.GenerateRefreshToken(user);

        await _sut.RecallRefreshToken(generated.Token);

        var stored = await _context.RefreshTokens.FirstAsync(t => t.Token == generated.Token);
        Assert.True(stored.IsExpired);
    }

    [Fact]
    public async Task RecallRefreshToken_DoesNotThrow_WhenTokenNotFound()
    {
        var exception = await Record.ExceptionAsync(() => _sut.RecallRefreshToken("does-not-exist"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ValidateRefreshToken_ReturnsNull_AfterRecall()
    {
        var user = MakeUser();
        var generated = await _sut.GenerateRefreshToken(user);

        await _sut.RecallRefreshToken(generated.Token);
        var result = await _sut.ValidateRefreshToken(generated.Token);

        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
