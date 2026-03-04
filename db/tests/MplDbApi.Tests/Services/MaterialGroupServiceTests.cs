using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Models.Filters;
using MplDbApi.Services;

namespace MplDbApi.Tests.Services;

public class MaterialGroupServiceTests : IDisposable
{
    private readonly BMplbaseContext _mainContext;
    private readonly FilterContext _filterContext;
    private readonly IMemoryCache _memoryCache;
    private readonly FilterService _filterService;
    private readonly MaterialGroupService _sut;

    public MaterialGroupServiceTests()
    {
        var mainOptions = new DbContextOptionsBuilder<BMplbaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _mainContext = new BMplbaseContext(mainOptions);

        var filterOptions = new DbContextOptionsBuilder<FilterContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _filterContext = new FilterContext(filterOptions);

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var filterLoggerMock = new Mock<ILogger<FilterService>>();
        _filterService = new FilterService(_filterContext, filterLoggerMock.Object, _memoryCache);

        _sut = new MaterialGroupService(_mainContext, _filterService);
    }

    public void Dispose()
    {
        _mainContext.Dispose();
        _filterContext.Dispose();
        _memoryCache.Dispose();
    }

    // --- Helpers ---

    private static MaterialGroup MakeMaterialGroup(int id, string name) => new()
    {
        Id = id,
        Name = name
    };

    private async Task AddFilterForRole(string role, List<int>? filteredGroups)
    {
        await _filterContext.Filters.AddAsync(new DataFilter
        {
            AffectedRole = role,
            Groups = filteredGroups
        });
        await _filterContext.SaveChangesAsync();
    }

    // --- GetMaterialGroupAsync ---

    [Fact]
    public async Task GetMaterialGroupAsync_ReturnsAllGroups_WhenNoFilter()
    {
        await _mainContext.MaterialGroups.AddRangeAsync(
            MakeMaterialGroup(1, "Metals"),
            MakeMaterialGroup(2, "Polymers"));
        await _mainContext.SaveChangesAsync();

        var result = await _sut.GetMaterialGroupAsync(null);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetMaterialGroupAsync_ReturnsEmpty_WhenNone()
    {
        var result = await _sut.GetMaterialGroupAsync(null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMaterialGroupAsync_ExcludesFilteredGroups_WhenRoleHasFilter()
    {
        await _mainContext.MaterialGroups.AddRangeAsync(
            MakeMaterialGroup(1, "Metals"),
            MakeMaterialGroup(2, "Polymers"),
            MakeMaterialGroup(3, "Chemicals"));
        await _mainContext.SaveChangesAsync();

        await AddFilterForRole("restricted", [2, 3]);

        var result = (await _sut.GetMaterialGroupAsync("restricted")).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Metals", result[0].Name);
    }

    [Fact]
    public async Task GetMaterialGroupAsync_ReturnsAllGroups_WhenFilterHasNullGroups()
    {
        await _mainContext.MaterialGroups.AddRangeAsync(
            MakeMaterialGroup(1, "Metals"),
            MakeMaterialGroup(2, "Polymers"));
        await _mainContext.SaveChangesAsync();

        await AddFilterForRole("allowed", null);

        var result = await _sut.GetMaterialGroupAsync("allowed");

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetMaterialGroupAsync_ReturnsDtosWithCorrectFields()
    {
        await _mainContext.MaterialGroups.AddAsync(MakeMaterialGroup(1, "Metals"));
        await _mainContext.SaveChangesAsync();

        var result = (await _sut.GetMaterialGroupAsync(null)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Metals", result[0].Name);
    }
}
