using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Models.Dtos;
using MplDbApi.Models.Filters;
using MplDbApi.Services;

namespace MplDbApi.Tests.Services;

public class MaterialValueServiceTests : IDisposable
{
    private readonly BMplbaseContext _mainContext;
    private readonly FilterContext _filterContext;
    private readonly IMemoryCache _memoryCache;
    private readonly FilterService _filterService;
    private readonly Mock<ILogger<MaterialValueService>> _loggerMock;
    private readonly MaterialValueService _sut;

    public MaterialValueServiceTests()
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

        _loggerMock = new Mock<ILogger<MaterialValueService>>();
        _sut = new MaterialValueService(_mainContext, _filterService, _loggerMock.Object);
    }

    public void Dispose()
    {
        _mainContext.Dispose();
        _filterContext.Dispose();
        _memoryCache.Dispose();
    }

    // --- Helpers ---

    private static MaterialValue MakeMaterialValue(int id, int uid, int propertyId,
        decimal? valueDecimal = null, DateOnly? createdOn = null) => new()
    {
        Id = id,
        Uid = uid,
        PropertyId = propertyId,
        ValueDecimal = valueDecimal,
        CreatedOn = createdOn ?? DateOnly.FromDateTime(DateTime.UtcNow)
    };

    private async Task AddMaterialSourceWithDependencies(int id)
    {
        var material = new Material { Id = id, Name = $"Material {id}" };
        var source = new Source { Id = id, Name = $"Source {id}" };
        var deliveryType = new DeliveryType { Id = id, Name = $"DeliveryType {id}" };
        var materialGroup = new MaterialGroup { Id = id, Name = $"Group {id}" };
        var unit = new Unit { Id = id, Name = "ton" };

        await _mainContext.AddRangeAsync(material, source, deliveryType, materialGroup, unit);

        var materialSource = new MaterialSource
        {
            Id = id,
            Uid = id,
            MaterialId = id,
            SourceId = id,
            DeliveryTypeId = id,
            MaterialGroupId = id,
            UnitId = id,
            TargetMarket = "RU"
        };
        await _mainContext.MaterialSources.AddAsync(materialSource);
        await _mainContext.SaveChangesAsync();
    }

    private async Task AddFilterForRole(string role, List<int>? materialIds)
    {
        await _filterContext.Filters.AddAsync(new DataFilter
        {
            AffectedRole = role,
            MaterialIds = materialIds
        });
        await _filterContext.SaveChangesAsync();
    }

    // --- GetMaterialValueById ---

    [Fact]
    public async Task GetMaterialValueById_ReturnsDto_WhenExists()
    {
        var value = MakeMaterialValue(1, uid: 10, propertyId: 1, valueDecimal: 99.5m,
            createdOn: new DateOnly(2025, 1, 15));
        await _mainContext.MaterialValues.AddAsync(value);
        await _mainContext.SaveChangesAsync();

        var result = await _sut.GetMaterialValueById(1, "admin");

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.Uid);
        Assert.Equal(1, result.PropertyId);
        Assert.Equal(99.5m, result.ValueDecimal);
    }

    [Fact]
    public async Task GetMaterialValueById_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetMaterialValueById(999, "admin");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMaterialValueById_ReturnsNull_WhenMaterialIsFiltered()
    {
        var value = MakeMaterialValue(1, uid: 10, propertyId: 1, valueDecimal: 100m);
        await _mainContext.MaterialValues.AddAsync(value);
        await _mainContext.SaveChangesAsync();

        await AddFilterForRole("viewer", materialIds: [1]); // filter checks against the value's Id

        var result = await _sut.GetMaterialValueById(1, "viewer");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMaterialValueById_ReturnsDto_WhenRoleHasFilterButMaterialNotIncluded()
    {
        var value = MakeMaterialValue(1, uid: 10, propertyId: 1, valueDecimal: 50m);
        await _mainContext.MaterialValues.AddAsync(value);
        await _mainContext.SaveChangesAsync();

        await AddFilterForRole("viewer", materialIds: [20, 30]);

        var result = await _sut.GetMaterialValueById(1, "viewer");

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    // --- GetMaterialMetricsByDateRange ---

    [Fact]
    public async Task GetMaterialMetricsByDateRange_ReturnsEmpty_WhenMaterialIsFiltered()
    {
        await AddFilterForRole("restricted", materialIds: [5]);

        var req = new MaterialDateMetricReq(
            MaterialId: 5,
            PropertyIds: [1, 2, 3],
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 3, 31));

        var result = await _sut.GetMaterialMetricsByDateRange(req, "restricted");

        Assert.Empty(result);
    }

    // Note: GetMaterialMetricsByDateRange tests that exercise the database query are omitted
    // because EF Core's InMemory provider cannot translate the non-aggregate GroupBy used
    // in the production query. Those code paths require an integration test with a real database.
}
