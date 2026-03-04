using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MplDbApi.Data;
using MplDbApi.Models.Dtos;
using MplDbApi.Models.Filters;
using MplDbApi.Services;

namespace MplDbApi.Tests.Services;

public class FilterServiceTests : IDisposable
{
    private readonly FilterContext _context;
    private readonly Mock<ILogger<FilterService>> _loggerMock;
    private readonly IMemoryCache _memoryCache;
    private readonly FilterService _sut;

    public FilterServiceTests()
    {
        var options = new DbContextOptionsBuilder<FilterContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FilterContext(options);
        _loggerMock = new Mock<ILogger<FilterService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _sut = new FilterService(_context, _loggerMock.Object, _memoryCache);
    }

    public void Dispose()
    {
        _context.Dispose();
        _memoryCache.Dispose();
    }

    // --- Helpers ---

    private static FilterCreateReqDto MakeFilterDto(
        string role = "viewer",
        List<int>? groups = null,
        List<int>? sources = null,
        List<int>? units = null,
        List<int>? materialIds = null,
        List<int>? properties = null) =>
        new(role, groups, sources, units, materialIds, properties);

    private static DataFilter MakeFilter(string role = "viewer", int id = 1) => new()
    {
        Id = id,
        AffectedRole = role
    };

    // --- GetFilterByRole ---

    [Fact]
    public async Task GetFilterByRole_ReturnsEmptyFilter_WhenRoleIsNull()
    {
        var result = await _sut.GetFilterByRole(null);

        Assert.NotNull(result);
        Assert.Equal("Default", result.AffectedRole);
        Assert.Null(result.Groups);
        Assert.Null(result.MaterialIds);
    }

    [Fact]
    public async Task GetFilterByRole_ReturnsEmptyFilter_WhenRoleIsEmpty()
    {
        var result = await _sut.GetFilterByRole(string.Empty);

        Assert.NotNull(result);
        Assert.Equal("Default", result.AffectedRole);
    }

    [Fact]
    public async Task GetFilterByRole_ReturnsFilter_WhenExists()
    {
        var filter = MakeFilter("analyst");
        filter.MaterialIds = [1, 2];
        await _context.Filters.AddAsync(filter);
        await _context.SaveChangesAsync();

        var result = await _sut.GetFilterByRole("analyst");

        Assert.NotNull(result);
        Assert.Equal("analyst", result.AffectedRole);
        Assert.Equal([1, 2], result.MaterialIds);
    }

    [Fact]
    public async Task GetFilterByRole_ReturnsEmptyDataFilter_WhenRoleNotFound()
    {
        var result = await _sut.GetFilterByRole("unknown-role");

        Assert.NotNull(result);
        Assert.Null(result.Groups);
        Assert.Null(result.MaterialIds);
    }

    // --- ModifyFilter ---

    [Fact]
    public async Task ModifyFilter_CreatesNewFilter_WhenNoneExists()
    {
        var dto = MakeFilterDto("trader", groups: [1, 2]);

        await _sut.ModifyFilter(dto);

        var saved = await _context.Filters.FirstOrDefaultAsync(f => f.AffectedRole == "trader");
        Assert.NotNull(saved);
        Assert.Equal([1, 2], saved.Groups);
    }

    [Fact]
    public async Task ModifyFilter_UpdatesExistingFilter_WhenAlreadyExists()
    {
        var existing = MakeFilter("editor");
        existing.Groups = [1];
        await _context.Filters.AddAsync(existing);
        await _context.SaveChangesAsync();

        var dto = MakeFilterDto("editor", groups: [3, 4], materialIds: [10]);
        await _sut.ModifyFilter(dto);

        var updated = await _context.Filters.FirstOrDefaultAsync(f => f.AffectedRole == "editor");
        Assert.NotNull(updated);
        Assert.Equal([3, 4], updated.Groups);
        Assert.Equal([10], updated.MaterialIds);
    }

    [Fact]
    public async Task ModifyFilter_Throws_WhenAffectedRoleIsEmpty()
    {
        var dto = MakeFilterDto(role: string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ModifyFilter(dto));
    }

    // --- GetAllFilters ---

    [Fact]
    public async Task GetAllFilters_ReturnsAllFilters()
    {
        await _context.Filters.AddRangeAsync(MakeFilter("role1", 1), MakeFilter("role2", 2));
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllFilters();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllFilters_ReturnsEmpty_WhenNone()
    {
        var result = await _sut.GetAllFilters();

        Assert.Empty(result);
    }
}
