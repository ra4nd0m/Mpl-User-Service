using System.Text;
using MplUserService.Services;

namespace MplUserService.Tests.Services;

public class DiskObjectStoreServiceTests : IDisposable
{
    private readonly string _testRootPath;

    public DiskObjectStoreServiceTests()
    {
        // Create a unique temporary directory for each test
        _testRootPath = Path.Combine(Path.GetTempPath(), $"TestStorage_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRootPath);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testRootPath))
        {
            Directory.Delete(_testRootPath, recursive: true);
        }
    }

    [Fact]
    public async Task PutAsync_CreatesFileWithCorrectContent()
    {
        // Arrange
        var service = new DiskObjectStoreService(_testRootPath);
        var key = "test-file.pdf";
        var content = "This is test content";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var contentStream = new MemoryStream(contentBytes);

        // Act
        await service.PutAsync(key, contentStream, "application/pdf", CancellationToken.None);

        // Assert
        var filePath = Path.Combine(_testRootPath, key);
        Assert.True(File.Exists(filePath));
        
        var savedContent = await File.ReadAllTextAsync(filePath, CancellationToken.None);
        Assert.Equal(content, savedContent);
    }

    [Fact]
    public async Task PutAsync_OverwritesExistingFile()
    {
        // Arrange
        var service = new DiskObjectStoreService(_testRootPath);
        var key = "overwrite-test.pdf";
        var filePath = Path.Combine(_testRootPath, key);

        // Create initial file
        await File.WriteAllTextAsync(filePath, "Old content", CancellationToken.None);

        var newContent = "New content";
        var contentBytes = Encoding.UTF8.GetBytes(newContent);
        var contentStream = new MemoryStream(contentBytes);

        // Act
        await service.PutAsync(key, contentStream, "application/pdf", CancellationToken.None);

        // Assert
        var savedContent = await File.ReadAllTextAsync(filePath, CancellationToken.None);
        Assert.Equal(newContent, savedContent);
    }

    [Fact]
    public async Task GetAsync_WhenFileExists_ReturnsStreamAndContentType()
    {
        // Arrange
        var service = new DiskObjectStoreService(_testRootPath);
        var key = "existing-file.pdf";
        var content = "File content to retrieve";
        var filePath = Path.Combine(_testRootPath, key);
        await File.WriteAllTextAsync(filePath, content, CancellationToken.None);

        // Act
        var (stream, contentType) = await service.GetAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(stream);
        Assert.Equal("application/pdf", contentType);

        using var reader = new StreamReader(stream);
        var retrievedContent = await reader.ReadToEndAsync(CancellationToken.None);
        Assert.Equal(content, retrievedContent);
    }

    [Fact]
    public async Task GetAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var service = new DiskObjectStoreService(_testRootPath);
        var key = "non-existent-file.pdf";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await service.GetAsync(key, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DeleteAsync_WhenFileExists_DeletesFile()
    {
        // Arrange
        var service = new DiskObjectStoreService(_testRootPath);
        var key = "file-to-delete.pdf";
        var filePath = Path.Combine(_testRootPath, key);
        await File.WriteAllTextAsync(filePath, "Content to delete", CancellationToken.None);

        // Verify file exists before deletion
        Assert.True(File.Exists(filePath));

        // Act
        await service.DeleteAsync(key, CancellationToken.None);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteAsync_WhenFileDoesNotExist_DoesNotThrow()
    {
        // Arrange
        var service = new DiskObjectStoreService(_testRootPath);
        var key = "non-existent-to-delete.pdf";

        // Act & Assert - should not throw
        await service.DeleteAsync(key, CancellationToken.None);
    }

    [Fact]
    public async Task PutAsync_WithEmptyStream_CreatesEmptyFile()
    {
        // Arrange
        var service = new DiskObjectStoreService(_testRootPath);
        var key = "empty-file.pdf";
        var emptyStream = new MemoryStream();

        // Act
        await service.PutAsync(key, emptyStream, "application/pdf", CancellationToken.None);

        // Assert
        var filePath = Path.Combine(_testRootPath, key);
        Assert.True(File.Exists(filePath));
        
        var fileInfo = new FileInfo(filePath);
        Assert.Equal(0, fileInfo.Length);
    }

    [Fact]
    public async Task PutAsync_WithLargeContent_SavesCorrectly()
    {
        // Arrange
        var service = new DiskObjectStoreService(_testRootPath);
        var key = "large-file.pdf";
        
        // Create a 1MB test content
        var largeContent = new byte[1024 * 1024];
        new Random().NextBytes(largeContent);
        var contentStream = new MemoryStream(largeContent);

        // Act
        await service.PutAsync(key, contentStream, "application/pdf", CancellationToken.None);

        // Assert
        var filePath = Path.Combine(_testRootPath, key);
        Assert.True(File.Exists(filePath));
        
        var savedBytes = await File.ReadAllBytesAsync(filePath, CancellationToken.None);
        Assert.Equal(largeContent.Length, savedBytes.Length);
        Assert.Equal(largeContent, savedBytes);
    }

    [Fact]
    public async Task GetAsync_ReturnsReadableStream()
    {
        // Arrange
        var service = new DiskObjectStoreService(_testRootPath);
        var key = "readable-test.pdf";
        var content = "Stream read test";
        var filePath = Path.Combine(_testRootPath, key);
        await File.WriteAllTextAsync(filePath, content, CancellationToken.None);

        // Act
        var (stream, _) = await service.GetAsync(key, CancellationToken.None);

        // Assert
        Assert.True(stream.CanRead);
        
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer, CancellationToken.None);
        var readContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        Assert.Equal(content, readContent);
        
        stream.Dispose();
    }
}
