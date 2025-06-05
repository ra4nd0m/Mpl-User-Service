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

builder.Services.AddScoped<IDeliveryTypeService, DeliveryTypeService>();
builder.Services.AddScoped<IMaterialSourceService, MaterialSourceService>();
builder.Services.AddScoped<IMaterialValueService, MaterialValueService>();
builder.Services.AddScoped<IMaterialPropService, MaterialPropService>();
builder.Services.AddScoped<IMaterialGroupService, MaterialGroupService>();
builder.Services.AddScoped<FilterService>();

var app = builder.Build();

app.UseCors();

app.MapDeliveryTypeRoutes();
app.MapMaterialSourceRoutes();
app.MapMaterialValueRoutes();
app.MapMaterialPropertyRoutes();
app.MapMaterialGroupRoutes();
app.MapFilterConfigRoutes();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<FilterContext>();
        await context.Database.MigrateAsync(); // Ensure the database is created and migrations are applied
        app.Logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred during database migration.");
        throw; // Re-throw the exception to stop the application startup
    }
}

app.Run();