using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MplUserService.Interfaces;
using MplUserService.Models.Dtos;
using MplUserService.Models.Enums;
using MplUserService.Routes;
using MplUserService.Tests.Routes.Helpers;

namespace MplUserService.Tests.Routes;

public class ReportFileRoutesTests : IAsyncDisposable
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    private readonly Mock<IReportFileService> _reportFileServiceMock = new();

    private async Task SetupAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_reportFileServiceMock.Object);

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(opts =>
        {
            opts.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
        });

        builder.Logging.ClearProviders();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapReportFileRoutes();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    // ---------- helpers ----------

    private static ReportFilesListDto MakeListDto(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        "report.pdf",
        "reports",
        SubscriptionType.Basic,
        DateTime.UtcNow
    );

    private static MultipartFormDataContent BuildUploadForm(
        string fileName = "report.pdf",
        string subscription = "Basic",
        string group = "reports",
        string fileContent = "PDF file content")
    {
        var form = new MultipartFormDataContent();
        var fileBytes = new ByteArrayContent(Encoding.UTF8.GetBytes(fileContent));
        fileBytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileBytes, "file", fileName);
        form.Add(new StringContent(subscription), "requiredSubscription");
        form.Add(new StringContent(group), "group");
        return form;
    }

    // ---------- GET /reports ----------

    [Fact]
    public async Task ListReports_Returns200_WithFileList()
    {
        await SetupAsync();
        _reportFileServiceMock
            .Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeListDto(), MakeListDto()]);

        var response = await _client.GetAsync("/reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ReportFilesListDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task ListReports_Returns200_WhenEmpty()
    {
        await SetupAsync();
        _reportFileServiceMock
            .Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var response = await _client.GetAsync("/reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ReportFilesListDto>>();
        Assert.Empty(body!);
    }

    [Fact]
    public async Task ListReports_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _reportFileServiceMock
            .Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB failure"));

        var response = await _client.GetAsync("/reports");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /reports/{id:guid} ----------

    [Fact]
    public async Task DownloadReport_Returns200_WithPdfContent()
    {
        await SetupAsync();
        var id = Guid.NewGuid();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("PDF binary data"));
        _reportFileServiceMock
            .Setup(s => s.DownloadAsync(id, It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, "report.pdf"));

        var response = await _client.GetAsync($"/reports/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DownloadReport_Returns404_WhenUnauthorizedAccess()
    {
        await SetupAsync();
        var id = Guid.NewGuid();
        _reportFileServiceMock
            .Setup(s => s.DownloadAsync(id, It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Subscription too low"));

        var response = await _client.GetAsync($"/reports/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadReport_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        var id = Guid.NewGuid();
        _reportFileServiceMock
            .Setup(s => s.DownloadAsync(id, It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage error"));

        var response = await _client.GetAsync($"/reports/{id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST /reports/upload ----------

    [Fact]
    public async Task UploadReport_Returns200_WithGeneratedId()
    {
        await SetupAsync();
        var expectedId = Guid.NewGuid();
        _reportFileServiceMock
            .Setup(s => s.UploadAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(),
                SubscriptionType.Basic, "reports", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        var response = await _client.PostAsync("/reports/upload", BuildUploadForm());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.Equal(expectedId, body!.Id);
    }

    [Fact]
    public async Task UploadReport_Returns400_WhenMissingFilePart()
    {
        await SetupAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Basic"), "requiredSubscription");
        form.Add(new StringContent("reports"), "group");
        // No file part

        var response = await _client.PostAsync("/reports/upload", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadReport_Returns400_WhenInvalidSubscriptionValue()
    {
        await SetupAsync();

        var response = await _client.PostAsync("/reports/upload",
            BuildUploadForm(subscription: "InvalidSubscriptionType"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadReport_Returns400_WhenNotFormContentType()
    {
        await SetupAsync();

        var content = new StringContent("{\"file\":\"data\"}", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/reports/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadReport_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _reportFileServiceMock
            .Setup(s => s.UploadAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(),
                It.IsAny<SubscriptionType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage full"));

        var response = await _client.PostAsync("/reports/upload", BuildUploadForm());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- DELETE /reports/{id:guid} ----------

    [Fact]
    public async Task DeleteReport_Returns200_WhenSuccessful()
    {
        await SetupAsync();
        var id = Guid.NewGuid();
        _reportFileServiceMock
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync($"/reports/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _reportFileServiceMock.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteReport_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        var id = Guid.NewGuid();
        _reportFileServiceMock
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("File not found in store"));

        var response = await _client.DeleteAsync($"/reports/{id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /reports/storage-usage ----------

    [Fact]
    public async Task GetStorageUsage_Returns200_WithUsageData()
    {
        await SetupAsync();
        var usage = new StorageUsageDto(UsedBytes: 1024 * 1024, MaxBytes: 100 * 1024 * 1024);
        _reportFileServiceMock
            .Setup(s => s.GetReportStorageUsageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usage);

        var response = await _client.GetAsync("/reports/storage-usage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StorageUsageDto>();
        Assert.Equal(usage.UsedBytes, body!.UsedBytes);
        Assert.Equal(usage.MaxBytes, body.MaxBytes);
    }

    [Fact]
    public async Task GetStorageUsage_Returns400_WhenQuotaNotConfigured()
    {
        await SetupAsync();
        _reportFileServiceMock
            .Setup(s => s.GetReportStorageUsageAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("StorageQuota not configured"));

        var response = await _client.GetAsync("/reports/storage-usage");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetStorageUsage_Returns400_WhenServiceThrows()
    {
        await SetupAsync();
        _reportFileServiceMock
            .Setup(s => s.GetReportStorageUsageAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var response = await _client.GetAsync("/reports/storage-usage");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    private record UploadResponse(Guid Id);
}
