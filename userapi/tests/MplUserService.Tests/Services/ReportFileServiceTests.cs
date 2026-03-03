using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using MplUserService.Auth;
using MplUserService.Config;
using MplUserService.Data;
using MplUserService.Interfaces;
using MplUserService.Models;
using MplUserService.Models.Enums;
using MplUserService.Services;

namespace MplUserService.Tests.Services;

public class ReportFileServiceTests
{
    private UserContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new UserContext(options);
    }

    private Mock<IFormFile> CreateMockFormFile(string fileName, long fileSize, string content = "test content")
    {
        var mockFile = new Mock<IFormFile>();
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var ms = new MemoryStream(contentBytes);

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(fileSize);
        mockFile.Setup(f => f.OpenReadStream()).Returns(ms);

        return mockFile;
    }

    private ClaimsPrincipal CreateClaimsPrincipal(string userId, string role = null!, SubscriptionType? subscription = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (subscription.HasValue)
        {
            claims.Add(new Claim("SubscriptionType", subscription.Value.ToString()));
            claims.Add(new Claim("SubscriptionEnd", DateTime.UtcNow.AddYears(1).ToString("O")));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    [Fact]
    public async Task UploadAsync_WhenFileIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000000 });

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);
        var mockFile = CreateMockFormFile("test.pdf", 0);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UploadAsync(
                mockFile.Object, 
                SubscriptionType.Free, 
                "reports", 
                CancellationToken.None)
        );
    }

    [Fact]
    public async Task UploadAsync_WhenStorageQuotaExceeded_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000 });

        // Add existing file that uses most of the quota
        var existingFile = new ReportFile
        {
            Id = Guid.NewGuid(),
            FileName = "existing.pdf",
            StoredName = "stored.pdf",
            FileGroup = "reports",
            RequiredSubscription = SubscriptionType.Free,
            UploadedAt = DateTime.UtcNow,
            SizeBytes = 900
        };
        context.ReportFiles.Add(existingFile);
        await context.SaveChangesAsync(CancellationToken.None);

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);
        var mockFile = CreateMockFormFile("test.pdf", 200);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UploadAsync(
                mockFile.Object, 
                SubscriptionType.Free, 
                "reports", 
                CancellationToken.None)
        );
    }

    [Fact]
    public async Task UploadAsync_WhenValid_CreatesFileAndReturnsId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000000 });

        mockStore.Setup(s => s.PutAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).Returns(Task.CompletedTask);

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);
        var mockFile = CreateMockFormFile("test.pdf", 1024);

        // Act
        var result = await service.UploadAsync(
            mockFile.Object, 
            SubscriptionType.Premium, 
            "reports", 
            CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        
        var savedFile = await context.ReportFiles.FirstOrDefaultAsync(f => f.Id == result, CancellationToken.None);
        Assert.NotNull(savedFile);
        Assert.Equal("test.pdf", savedFile.FileName);
        Assert.Equal("reports", savedFile.FileGroup);
        Assert.Equal(SubscriptionType.Premium, savedFile.RequiredSubscription);
        Assert.Equal(1024, savedFile.SizeBytes);
        
        mockStore.Verify(s => s.PutAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            "application/pdf",
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllFiles()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000000 });

        var file1 = new ReportFile
        {
            Id = Guid.NewGuid(),
            FileName = "file1.pdf",
            StoredName = "stored1.pdf",
            FileGroup = "reports",
            RequiredSubscription = SubscriptionType.Free,
            UploadedAt = DateTime.UtcNow.AddDays(-1),
            SizeBytes = 100
        };

        var file2 = new ReportFile
        {
            Id = Guid.NewGuid(),
            FileName = "file2.pdf",
            StoredName = "stored2.pdf",
            FileGroup = "analysis",
            RequiredSubscription = SubscriptionType.Premium,
            UploadedAt = DateTime.UtcNow,
            SizeBytes = 200
        };

        context.ReportFiles.AddRange(file1, file2);
        await context.SaveChangesAsync(CancellationToken.None);

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);

        // Act
        var result = await service.ListAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.FileName == "file1.pdf");
        Assert.Contains(result, f => f.FileName == "file2.pdf");
    }

    [Fact]
    public async Task ListAsync_WhenNoFiles_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000000 });

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);

        // Act
        var result = await service.ListAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DownloadAsync_WhenFileDoesNotExist_ThrowsUnauthorizedException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000000 });

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);
        var user = CreateClaimsPrincipal("user123");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await service.DownloadAsync(Guid.NewGuid(), user, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DownloadAsync_WhenAuthorizationFails_ThrowsUnauthorizedException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000000 });

        var file = new ReportFile
        {
            Id = Guid.NewGuid(),
            FileName = "premium.pdf",
            StoredName = "stored-premium.pdf",
            FileGroup = "reports",
            RequiredSubscription = SubscriptionType.Premium,
            UploadedAt = DateTime.UtcNow,
            SizeBytes = 100
        };

        context.ReportFiles.Add(file);
        await context.SaveChangesAsync(CancellationToken.None);

        mockAuthService.Setup(s => s.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<object>(),
            It.IsAny<IEnumerable<IAuthorizationRequirement>>()
        )).ReturnsAsync(AuthorizationResult.Failed());

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);
        var user = CreateClaimsPrincipal("user123", subscription: SubscriptionType.Free);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await service.DownloadAsync(file.Id, user, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DownloadAsync_WhenAuthorized_ReturnsStreamAndFileName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000000 });

        var file = new ReportFile
        {
            Id = Guid.NewGuid(),
            FileName = "report.pdf",
            StoredName = "stored-report.pdf",
            FileGroup = "reports",
            RequiredSubscription = SubscriptionType.Free,
            UploadedAt = DateTime.UtcNow,
            SizeBytes = 100
        };

        context.ReportFiles.Add(file);
        await context.SaveChangesAsync(CancellationToken.None);

        var mockStream = new MemoryStream(Encoding.UTF8.GetBytes("file content"));
        
        mockStore.Setup(s => s.GetAsync(
            "stored-report.pdf",
            It.IsAny<CancellationToken>()
        )).ReturnsAsync((mockStream, "application/pdf"));

        mockAuthService.Setup(s => s.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<object>(),
            It.IsAny<IEnumerable<IAuthorizationRequirement>>()
        )).ReturnsAsync(AuthorizationResult.Success());

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);
        var user = CreateClaimsPrincipal("user123", subscription: SubscriptionType.Free);

        // Act
        var (stream, fileName) = await service.DownloadAsync(file.Id, user, CancellationToken.None);

        // Assert
        Assert.NotNull(stream);
        Assert.Equal("report.pdf", fileName);
        mockStore.Verify(s => s.GetAsync("stored-report.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenFileExists_DeletesFileFromStoreAndDatabase()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000000 });

        var file = new ReportFile
        {
            Id = Guid.NewGuid(),
            FileName = "to-delete.pdf",
            StoredName = "stored-delete.pdf",
            FileGroup = "reports",
            RequiredSubscription = SubscriptionType.Free,
            UploadedAt = DateTime.UtcNow,
            SizeBytes = 100
        };

        context.ReportFiles.Add(file);
        await context.SaveChangesAsync(CancellationToken.None);

        mockStore.Setup(s => s.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).Returns(Task.CompletedTask);

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);

        // Act
        await service.DeleteAsync(file.Id, CancellationToken.None);

        // Assert
        var deletedFile = await context.ReportFiles.FirstOrDefaultAsync(f => f.Id == file.Id, CancellationToken.None);
        Assert.Null(deletedFile);
        
        mockStore.Verify(s => s.DeleteAsync("stored-delete.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenFileDoesNotExist_DoesNotThrow()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = 1000000 });

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);

        // Act & Assert - should not throw
        await service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);
        
        mockStore.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetReportStorageUsageAsync_ReturnsCorrectUsageAndMax()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var maxBytes = 10000L;
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = maxBytes });

        var file1 = new ReportFile
        {
            Id = Guid.NewGuid(),
            FileName = "file1.pdf",
            StoredName = "stored1.pdf",
            FileGroup = "reports",
            RequiredSubscription = SubscriptionType.Free,
            UploadedAt = DateTime.UtcNow,
            SizeBytes = 1500
        };

        var file2 = new ReportFile
        {
            Id = Guid.NewGuid(),
            FileName = "file2.pdf",
            StoredName = "stored2.pdf",
            FileGroup = "reports",
            RequiredSubscription = SubscriptionType.Free,
            UploadedAt = DateTime.UtcNow,
            SizeBytes = 2500
        };

        context.ReportFiles.AddRange(file1, file2);
        await context.SaveChangesAsync(CancellationToken.None);

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);

        // Act
        var result = await service.GetReportStorageUsageAsync(CancellationToken.None);

        // Assert
        Assert.Equal(4000L, result.UsedBytes);
        Assert.Equal(maxBytes, result.MaxBytes);
    }

    [Fact]
    public async Task GetReportStorageUsageAsync_WhenNoFiles_ReturnsZeroUsage()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockStore = new Mock<IObjectStore>();
        var mockAuthService = new Mock<IAuthorizationService>();
        var maxBytes = 10000L;
        var options = Options.Create(new StorageQuotaOptions { MaxBytes = maxBytes });

        var service = new ReportFileService(context, mockStore.Object, mockAuthService.Object, options);

        // Act
        var result = await service.GetReportStorageUsageAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0L, result.UsedBytes);
        Assert.Equal(maxBytes, result.MaxBytes);
    }
}
