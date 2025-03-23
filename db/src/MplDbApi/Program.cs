using Microsoft.EntityFrameworkCore;
using MplDbApi.Interfaces;
using MplDbApi.Services;
using MplDbApi.Data;
using MplDbApi.Routes;
using Microsoft.AspNetCore.Http.Json;

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

builder.Services.AddScoped<IDeliveryTypeService, DeliveryTypeService>();
builder.Services.AddScoped<IMaterialSourceService, MaterialSourceService>();
builder.Services.AddScoped<IMaterialValueService, MaterialValueService>();
builder.Services.AddScoped<IMaterialPropService, MaterialPropService>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

var urls = builder.Configuration["Hosting:Urls"];
if (!string.IsNullOrEmpty(urls))
{
    builder.WebHost.UseUrls(urls);
}
var app = builder.Build();

app.UseCors();

app.MapDeliveryTypeRoutes();
app.MapMaterialSourceRoutes();
app.MapMaterialValueRoutes();
app.MapMaterialPropertyRoutes();

app.Run();