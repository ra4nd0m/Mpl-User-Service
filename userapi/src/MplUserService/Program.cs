using System.Text;
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MplUserService.Auth;
using MplUserService.Data;
using MplUserService.Interfaces;
using MplUserService.Models.Enums;
using MplUserService.Routes;
using MplUserService.Services;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Primitives;
using MplUserService.Config;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var builder = WebApplication.CreateBuilder(args);

var connectionString = configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("DefaultConnection is missing");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

var storageRootString = configuration["Storage:RootPath"] ?? throw new InvalidOperationException("Storage:RootPath is missing");
var storageRoot = Path.GetFullPath(storageRootString);
Directory.CreateDirectory(storageRoot);
builder.Services.AddSingleton<IObjectStore>(_ => new DiskObjectStoreService(storageRoot));

var urls = builder.Configuration["Hosting:Urls"];
if (!string.IsNullOrEmpty(urls))
{
    builder.WebHost.UseUrls(urls);
}

builder.Services.Configure<StorageQuotaOptions>(
    configuration.GetSection(StorageQuotaOptions.SectionName)
);

builder.Services.AddHttpClient("DbClient", client =>
{
    client.BaseAddress = new Uri(configuration["DBApi:BaseUrl"] ?? throw new InvalidOperationException("DBApi:BaseUrl is missing"));
});

builder.Services.AddHttpClient("SpreadsheetClient", client =>
{
    client.BaseAddress = new Uri(configuration["SpreadsheetApi:BaseUrl"] ?? throw new InvalidOperationException("SpreadsheetApi:BaseUrl is missing"));
});

builder.Services.AddDbContext<UserContext>(options =>
    options.UseNpgsql(dataSource));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing"))
            ),
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireFree", policy =>
        policy.Requirements.Add(new SubscriptionRequirement(SubscriptionType.Free)));
    options.AddPolicy("RequireBasic", policy =>
        policy.Requirements.Add(new SubscriptionRequirement(SubscriptionType.Basic)));
    options.AddPolicy("RequirePremium", policy =>
        policy.Requirements.Add(new SubscriptionRequirement(SubscriptionType.Premium)));
    options.AddPolicy("internal", policy =>
        policy.Requirements.Add(new RoleRequirement("internal")));
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanExportData", policy =>
        policy.Requirements.Add(new CanExportDataRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, SubscriptionHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanExportDataHandler>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReportFileService, ReportFileService>();
builder.Services.AddScoped<IAuthorizationHandler, SubscriptionHandler>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder
            .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://127.0.0.1:5173", "http://localhost:5173"])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Token-Expired");
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("UploadPolicy", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: httpContext.User?.Identity?.Name ??
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 30,
                TokensPerPeriod = 30,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
                QueueLimit = 0
            }
        )
    );
    options.AddPolicy("DownloadPolicy", context =>
    {
        var key = context.User?.Identity?.Name ??
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: key,
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 30,
                TokensPerPeriod = 30,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapUserDataRoutes();
app.MapMaterialRoutes();
app.MapGeneratorRoutes();
app.MapInternalRoutes();
app.MapReportFileRoutes();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<UserContext>();
        await context.Database.MigrateAsync();
        app.Logger.LogInformation("Database migrated");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database");
    }
}

app.Run();