using Microsoft.EntityFrameworkCore;
using MplDataReceiver.Data;
using MplDataReceiver.Routes;
using MplDataReceiver.Services;

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
});

builder.Services.AddHttpClient("DbApi", client =>
{
    var baseUrl = configuration["ApiSettings:BaseUrl"];
    if (string.IsNullOrEmpty(baseUrl))
    {
        throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");
    }
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddScoped<DataInsertService>();

var app = builder.Build();

app.UseCors();

app.MapDataInsertRoutes();

app.Run();
