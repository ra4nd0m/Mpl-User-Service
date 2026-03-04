using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Services;

namespace MplDbApi.Tests.Services;

public class UnitServiceTests : IDisposable
{
    private readonly BMplbaseContext _context;
    private readonly UnitService _sut;

    public UnitServiceTests()
    {
        var options = new DbContextOptionsBuilder<BMplbaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new BMplbaseContext(options);
        _sut = new UnitService(_context);
    }

    public void Dispose() => _context.Dispose();

    // --- Helpers ---

    private static Unit MakeUnit(int id, string name) => new()
    {
        Id = id,
        Name = name
    };

    // --- GetUnits ---

    [Fact]
    public async Task GetUnits_ReturnsAllUnits()
    {
        await _context.Units.AddRangeAsync(
            MakeUnit(1, "kg"),
            MakeUnit(2, "ton"));
        await _context.SaveChangesAsync();

        var result = await _sut.GetUnits();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetUnits_ReturnsEmpty_WhenNone()
    {
        var result = await _sut.GetUnits();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUnits_ReturnsDtosWithCorrectFields()
    {
        await _context.Units.AddAsync(MakeUnit(1, "ton"));
        await _context.SaveChangesAsync();

        var result = (await _sut.GetUnits()).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("ton", result[0].Name);
    }
}
