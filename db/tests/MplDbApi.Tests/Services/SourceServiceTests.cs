using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Models;
using MplDbApi.Services;

namespace MplDbApi.Tests.Services;

public class SourceServiceTests : IDisposable
{
    private readonly BMplbaseContext _context;
    private readonly SourceService _sut;

    public SourceServiceTests()
    {
        var options = new DbContextOptionsBuilder<BMplbaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new BMplbaseContext(options);
        _sut = new SourceService(_context);
    }

    public void Dispose() => _context.Dispose();

    // --- Helpers ---

    private static Source MakeSource(int id, string name) => new()
    {
        Id = id,
        Name = name
    };

    // --- GetSources ---

    [Fact]
    public async Task GetSources_ReturnsAllSources()
    {
        await _context.Sources.AddRangeAsync(
            MakeSource(1, "Source A"),
            MakeSource(2, "Source B"));
        await _context.SaveChangesAsync();

        var result = await _sut.GetSources();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetSources_ReturnsEmpty_WhenNone()
    {
        var result = await _sut.GetSources();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSources_ReturnsDtosWithCorrectFields()
    {
        await _context.Sources.AddAsync(MakeSource(1, "LME"));
        await _context.SaveChangesAsync();

        var result = (await _sut.GetSources()).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("LME", result[0].Name);
    }
}
