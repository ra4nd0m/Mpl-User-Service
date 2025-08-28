using Microsoft.EntityFrameworkCore;
using MplDbApi.Interfaces;
using MplDbApi.Services;
using MplDbApi.Data;
using MplDbApi.Routes;
using MplDbApi.Utils;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BMplbaseContext>(options =>
        {
            var connectionString = configuration["ConnectionStrings:DefaultConnection"];

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("BMplbaseConnection string is not configured.");
            }
            options.UseNpgsql(connectionString);
        }
    );

builder.Services.AddDbContext<FilterContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("FilterConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("FilterConnection string is not configured.");
            }
            options.UseSqlite(connectionString);
        }
    );

//This service is internal so no need for restrictive cors 
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IDeliveryTypeService, DeliveryTypeService>();
builder.Services.AddScoped<IMaterialSourceService, MaterialSourceService>();
builder.Services.AddScoped<IMaterialValueService, MaterialValueService>();
builder.Services.AddScoped<IMaterialPropService, MaterialPropService>();
builder.Services.AddScoped<IMaterialGroupService, MaterialGroupService>();
builder.Services.AddScoped<ISourceService, SourceService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<FilterService>();
builder.Services.AddScoped<CacheManagementService>();

var app = builder.Build();

app.UseCors();

app.MapDeliveryTypeRoutes();
app.MapMaterialSourceRoutes();
app.MapMaterialValueRoutes();
app.MapMaterialPropertyRoutes();
app.MapMaterialGroupRoutes();
app.MapSourceRoutes();
app.MapUnitRoutes();
app.MapFilterConfigRoutes();
app.MapCacheManagementRoutes();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseInitializer.InitializeFilterDatabase(scope.ServiceProvider, logger);
}

app.Run();