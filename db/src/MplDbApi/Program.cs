using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;


var enviroment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{enviroment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BMplbaseContext>(options =>
{
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
