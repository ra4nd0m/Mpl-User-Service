using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Services;

namespace MplDbApi.Tests.Services;

public class MaterialPropServiceTests : IDisposable
{
    private readonly BMplbaseContext _context;
    private readonly Mock<ILogger<MaterialPropService>> _loggerMock;
    private readonly MaterialPropService _sut;

    public MaterialPropServiceTests()
    {
        var options = new DbContextOptionsBuilder<BMplbaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new BMplbaseContext(options);
        _loggerMock = new Mock<ILogger<MaterialPropService>>();
        _sut = new MaterialPropService(_context, _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // --- Helpers ---

    private static Property MakeProperty(int id, string name) => new()
    {
        Id = id,
        Name = name,
        Kind = "decimal"
    };

    private static MaterialProperty MakeMaterialProperty(int id, int materialSourceId, int propertyId) => new()
    {
        Id = id,
        Uid = materialSourceId,
        PropertyId = propertyId
    };

    // --- GetMaterialProperties ---

    [Fact]
    public async Task GetMaterialProperties_ReturnsProperties_ForGivenMaterial()
    {
        var property = MakeProperty(1, "Price Avg");
        await _context.Properties.AddAsync(property);
        await _context.MaterialProperties.AddAsync(MakeMaterialProperty(1, materialSourceId: 10, propertyId: 1));
        await _context.SaveChangesAsync();

        var result = await _sut.GetMaterialProperties(10);

        Assert.Single(result);
        Assert.Equal(1, result[0].PropertyId);
        Assert.Equal("Price Avg", result[0].PropertyName);
    }

    [Fact]
    public async Task GetMaterialProperties_ReturnsEmpty_WhenNoPropertiesForMaterial()
    {
        var result = await _sut.GetMaterialProperties(99);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMaterialProperties_ReturnsOnlyPropertiesForRequestedMaterial()
    {
        await _context.Properties.AddRangeAsync(
            MakeProperty(1, "Price Avg"),
            MakeProperty(2, "Price Min"));
        await _context.MaterialProperties.AddRangeAsync(
            MakeMaterialProperty(1, materialSourceId: 10, propertyId: 1),
            MakeMaterialProperty(2, materialSourceId: 20, propertyId: 2));
        await _context.SaveChangesAsync();

        var result = await _sut.GetMaterialProperties(10);

        Assert.Single(result);
        Assert.Equal(1, result[0].PropertyId);
    }

    [Fact]
    public async Task GetMaterialProperties_ReturnsMultipleProperties_WhenMaterialHasMany()
    {
        await _context.Properties.AddRangeAsync(
            MakeProperty(1, "Price Avg"),
            MakeProperty(2, "Price Min"),
            MakeProperty(3, "Price Max"));
        await _context.MaterialProperties.AddRangeAsync(
            MakeMaterialProperty(1, materialSourceId: 5, propertyId: 1),
            MakeMaterialProperty(2, materialSourceId: 5, propertyId: 2),
            MakeMaterialProperty(3, materialSourceId: 5, propertyId: 3));
        await _context.SaveChangesAsync();

        var result = await _sut.GetMaterialProperties(5);

        Assert.Equal(3, result.Count);
    }

    // --- GetMaterialPropertiesForDropdown ---

    [Fact]
    public async Task GetMaterialPropertiesForDropdown_ReturnsAllProperties()
    {
        await _context.Properties.AddRangeAsync(
            MakeProperty(1, "Price Avg"),
            MakeProperty(2, "Price Min"));
        await _context.SaveChangesAsync();

        var result = await _sut.GetMaterialPropertiesForDropdown();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetMaterialPropertiesForDropdown_ReturnsEmpty_WhenNone()
    {
        var result = await _sut.GetMaterialPropertiesForDropdown();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMaterialPropertiesForDropdown_ReturnsDtosWithCorrectFields()
    {
        await _context.Properties.AddAsync(MakeProperty(1, "Supply"));
        await _context.SaveChangesAsync();

        var result = await _sut.GetMaterialPropertiesForDropdown();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Supply", result[0].Name);
    }
}
