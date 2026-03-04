using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Services;

namespace MplDbApi.Tests.Services;

public class DeliveryTypeServiceTests : IDisposable
{
    private readonly BMplbaseContext _context;
    private readonly DeliveryTypeService _sut;

    public DeliveryTypeServiceTests()
    {
        var options = new DbContextOptionsBuilder<BMplbaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new BMplbaseContext(options);
        _sut = new DeliveryTypeService(_context);
    }

    public void Dispose() => _context.Dispose();

    // --- Helpers ---

    private static DeliveryType MakeDeliveryType(int id, string name) => new()
    {
        Id = id,
        Name = name
    };

    // --- GetDeliveryTypesAsync ---

    [Fact]
    public async Task GetDeliveryTypesAsync_ReturnsAllDeliveryTypes()
    {
        await _context.DeliveryTypes.AddRangeAsync(
            MakeDeliveryType(1, "Type A"),
            MakeDeliveryType(2, "Type B"));
        await _context.SaveChangesAsync();

        var result = await _sut.GetDeliveryTypesAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetDeliveryTypesAsync_ReturnsEmpty_WhenNone()
    {
        var result = await _sut.GetDeliveryTypesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDeliveryTypesAsync_ReturnsDtosWithCorrectFields()
    {
        await _context.DeliveryTypes.AddAsync(MakeDeliveryType(1, "EXW"));
        await _context.SaveChangesAsync();

        var result = (await _sut.GetDeliveryTypesAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("EXW", result[0].Name);
    }

    // --- GetDeliveryTypeByIdAsync ---

    [Fact]
    public async Task GetDeliveryTypeByIdAsync_ReturnsDto_WhenExists()
    {
        await _context.DeliveryTypes.AddAsync(MakeDeliveryType(1, "CIF"));
        await _context.SaveChangesAsync();

        var result = await _sut.GetDeliveryTypeByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("CIF", result.Name);
    }

    [Fact]
    public async Task GetDeliveryTypeByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetDeliveryTypeByIdAsync(99);

        Assert.Null(result);
    }
}
