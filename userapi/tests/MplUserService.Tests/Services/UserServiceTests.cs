using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MplUserService.Data;
using MplUserService.Models;
using MplUserService.Services;

namespace MplUserService.Tests.Services;

public class UserServiceTests
{
    private UserContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new UserContext(options);
    }

    private ClaimsPrincipal CreateClaimsPrincipal(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WhenUserDoesNotExist_CreatesNewUser()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new UserService(context);
        var userId = "test-user-123";
        var claims = CreateClaimsPrincipal(userId);

        // Act
        var result = await service.GetOrCreateUserAsync(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        
        // Verify user was saved to database
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None);
        Assert.NotNull(savedUser);
        Assert.Equal(userId, savedUser.Id);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WhenUserExists_ReturnsExistingUser()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new UserService(context);
        var userId = "existing-user-456";
        
        var existingUser = new User
        {
            Id = userId,
            FavouriteIds = new List<int> { 1, 2, 3 },
            SettingsJson = "{\"theme\":\"dark\"}"
        };
        
        context.Users.Add(existingUser);
        await context.SaveChangesAsync(CancellationToken.None);

        var claims = CreateClaimsPrincipal(userId);

        // Act
        var result = await service.GetOrCreateUserAsync(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(3, result.FavouriteIds.Count);
        Assert.Equal("{\"theme\":\"dark\"}", result.SettingsJson);
        
        // Verify no duplicate was created
        var userCount = await context.Users.CountAsync(CancellationToken.None);
        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WhenUserIdClaimMissing_ThrowsUnauthorizedException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new UserService(context);
        var claims = new ClaimsPrincipal(new ClaimsIdentity());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await service.GetOrCreateUserAsync(claims)
        );
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserExists_DeletesUserAndReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new UserService(context);
        var userId = "user-to-delete";
        
        var user = new User { Id = userId };
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await service.DeleteUserAsync(userId);

        // Assert
        Assert.True(result);
        var deletedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new UserService(context);
        var userId = "non-existent-user";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.DeleteUserAsync(userId)
        );
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserIdIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new UserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.DeleteUserAsync(null!)
        );
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserIdIsEmpty_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new UserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.DeleteUserAsync(string.Empty)
        );
    }
}
