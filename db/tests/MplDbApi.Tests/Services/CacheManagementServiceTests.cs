using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MplDbApi.Services;

namespace MplDbApi.Tests.Services;

public class CacheManagementServiceTests : IDisposable
{
    private readonly Mock<ILogger<CacheManagementService>> _loggerMock;
    private readonly MemoryCache _memoryCache;
    private readonly CacheManagementService _sut;

    public CacheManagementServiceTests()
    {
        _loggerMock = new Mock<ILogger<CacheManagementService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _sut = new CacheManagementService(_loggerMock.Object, _memoryCache);
    }

    public void Dispose() => _memoryCache.Dispose();

    // --- ClearCache ---

    [Fact]
    public void ClearCache_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.ClearCache());

        Assert.Null(exception);
    }

    [Fact]
    public void ClearCache_RemovesEntriesFromCache()
    {
        _memoryCache.Set("key1", "value1");
        _memoryCache.Set("key2", "value2");

        _sut.ClearCache();

        Assert.False(_memoryCache.TryGetValue("key1", out _));
        Assert.False(_memoryCache.TryGetValue("key2", out _));
    }

    [Fact]
    public void ClearCache_SucceedsOnEmptyCache()
    {
        var exception = Record.Exception(() => _sut.ClearCache());

        Assert.Null(exception);
    }

    [Fact]
    public void ClearCache_LogsInformation()
    {
        _sut.ClearCache();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Cache cleared")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
