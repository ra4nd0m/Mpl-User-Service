using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MplAuthService.Data;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;
using MplAuthService.Models.Enums;
using MplAuthService.Services;

namespace MplAuthService.Tests.Services;

public class OrgServiceTests : IDisposable
{
    private readonly AuthContext _context;
    private readonly Mock<ILogger<OrgService>> _loggerMock;
    private readonly OrgService _sut;

    public OrgServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AuthContext(options);
        _loggerMock = new Mock<ILogger<OrgService>>();
        _sut = new OrgService(_context, _loggerMock.Object);
    }

    private static OrganizationDto MakeOrgDto(string inn = "1234567890") => new(
        Name: "Test Org",
        Inn: inn,
        SubscriptionType: SubscriptionType.Basic,
        SubscriptionStartDate: DateTime.UtcNow.AddDays(-30),
        SubscriptionEndDate: DateTime.UtcNow.AddDays(30)
    );

    private static Organization MakeOrg(string inn = "1234567890", int id = 1) => new()
    {
        Id = id,
        Name = "Test Org",
        Inn = inn,
        SubscriptionType = SubscriptionType.Basic,
        SubscriptionStartDate = DateTime.UtcNow.AddDays(-30),
        SubscriptionEndDate = DateTime.UtcNow.AddDays(30)
    };

    // --- GetOrganization ---

    [Fact]
    public async Task GetOrganization_ReturnsOrganization_WhenExists()
    {
        var org = MakeOrg();
        await _context.Organizations.AddAsync(org);
        await _context.SaveChangesAsync();

        var result = await _sut.GetOrganization(org.Inn);

        Assert.NotNull(result);
        Assert.Equal(org.Inn, result.Inn);
    }

    [Fact]
    public async Task GetOrganization_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetOrganization("nonexistent-inn");

        Assert.Null(result);
    }

    // --- GetOrganizations ---

    [Fact]
    public async Task GetOrganizations_ReturnsAllOrganizations()
    {
        await _context.Organizations.AddRangeAsync(MakeOrg("111"), MakeOrg("222", 2));
        await _context.SaveChangesAsync();

        var result = await _sut.GetOrganizations();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetOrganizations_ReturnsEmpty_WhenNone()
    {
        var result = await _sut.GetOrganizations();

        Assert.Empty(result);
    }

    // --- CreateOrganization ---

    [Fact]
    public async Task CreateOrganization_CreatesAndReturnsOrganization()
    {
        var dto = MakeOrgDto();

        var result = await _sut.CreateOrganization(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.Inn, result.Inn);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(1, await _context.Organizations.CountAsync());
    }

    [Fact]
    public async Task CreateOrganization_Throws_WhenInnAlreadyExists()
    {
        var org = MakeOrg("duplicate-inn");
        await _context.Organizations.AddAsync(org);
        await _context.SaveChangesAsync();

        var dto = MakeOrgDto("duplicate-inn");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateOrganization(dto));
    }

    // --- UpdateOrganization ---

    [Fact]
    public async Task UpdateOrganization_UpdatesAndReturnsDto_WhenFound()
    {
        var org = MakeOrg();
        await _context.Organizations.AddAsync(org);
        await _context.SaveChangesAsync();

        var updateDto = new OrganizationDto(
            Name: "Updated Org",
            Inn: "9999999999",
            SubscriptionType: SubscriptionType.Premium,
            SubscriptionStartDate: DateTime.UtcNow,
            SubscriptionEndDate: DateTime.UtcNow.AddYears(1)
        );

        var result = await _sut.UpdateOrganization(org.Id, updateDto);

        Assert.NotNull(result);
        Assert.Equal("Updated Org", result.Name);
        Assert.Equal("9999999999", result.Inn);
        Assert.Equal(SubscriptionType.Premium, result.SubscriptionType);
    }

    [Fact]
    public async Task UpdateOrganization_ReturnsNull_WhenNotFound()
    {
        var dto = MakeOrgDto();

        var result = await _sut.UpdateOrganization(9999, dto);

        Assert.Null(result);
    }

    // --- DeleteOrganization ---

    [Fact]
    public async Task DeleteOrganization_ReturnsTrue_WhenDeleted()
    {
        var org = MakeOrg();
        await _context.Organizations.AddAsync(org);
        await _context.SaveChangesAsync();

        var result = await _sut.DeleteOrganization(org.Id);

        Assert.True(result);
        Assert.Equal(0, await _context.Organizations.CountAsync());
    }

    [Fact]
    public async Task DeleteOrganization_ReturnsFalse_WhenNotFound()
    {
        var result = await _sut.DeleteOrganization(9999);

        Assert.False(result);
    }

    // --- GetUsersByOrganization ---

    [Fact]
    public async Task GetUsersByOrganization_ReturnsUsersForOrg()
    {
        var org = MakeOrg();
        await _context.Organizations.AddAsync(org);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Id = "user-1",
            Email = "user@example.com",
            UserName = "user@example.com",
            Organization = org,
            OrganizationId = org.Id
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _sut.GetUsersByOrganization(org.Id);

        Assert.Single(result);
        Assert.Equal("user@example.com", result[0].Email);
    }

    [Fact]
    public async Task GetUsersByOrganization_ReturnsEmpty_WhenNoUsersInOrg()
    {
        var org = MakeOrg();
        await _context.Organizations.AddAsync(org);
        await _context.SaveChangesAsync();

        var result = await _sut.GetUsersByOrganization(org.Id);

        Assert.Empty(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
