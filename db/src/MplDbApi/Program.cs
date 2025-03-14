using Microsoft.EntityFrameworkCore;
using MplDbApi.Interfaces;
using MplDbApi.Services;
using MplDbApi.Data;
using MplDbApi.Routes;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BMplbaseContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDeliveryTypeService, DeliveryTypeService>();
builder.Services.AddScoped<IMaterialSourceService, MaterialSourceService>();
builder.Services.AddScoped<IMaterialValueService, MaterialValueService>();

var app = builder.Build();

app.MapDeliveryTypeRoutes();
app.MapMaterialSourceRoutes();
app.MapMaterialValueRoutes();

app.Run();