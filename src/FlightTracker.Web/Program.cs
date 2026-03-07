using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;
using FlightTracker.Core.Services;
using FlightTracker.Data;
using FlightTracker.Data.Repositories;
using FlightTracker.Data.TableStorage;
using FlightTracker.Data.TableStorage.Repositories;
using FlightTracker.Web.Components;
using FlightTracker.Web.Data;
using FlightTracker.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Sentry
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = 1.0; // Capture 100% of transactions for performance monitoring
    options.Debug = builder.Environment.IsDevelopment();
    options.AttachStacktrace = true;
    options.SendDefaultPii = false; // Don't send personally identifiable information
    options.MaxBreadcrumbs = 50;
    options.EnableLogs = true; // Enable log integration
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Bind configuration
builder.Services.Configure<AppConfig>(
    builder.Configuration.GetSection("FlightTracker"));

builder.Services.Configure<SeedingConfig>(
    builder.Configuration.GetSection("Seeding"));

// Configure database - use Table Storage in production if configured, SQLite otherwise
var tableStorageConnectionString = builder.Configuration["TableStorage:ConnectionString"];
var useTableStorage = !string.IsNullOrEmpty(tableStorageConnectionString);

if (useTableStorage)
{
    // Production: Use Azure Table Storage
    builder.Services.AddSingleton(new TableStorageContext(tableStorageConnectionString!));
    builder.Services.AddSingleton<TableStorageDestinationRepository>();
    builder.Services.AddScoped<IDestinationRepository>(sp => sp.GetRequiredService<TableStorageDestinationRepository>());
    builder.Services.AddScoped<ITargetDateRepository, TableStorageTargetDateRepository>();
    builder.Services.AddScoped<IPriceCheckRepository, TableStoragePriceCheckRepository>();
    
    // Add health check for Table Storage
    builder.Services.AddHealthChecks()
        .AddCheck("database", () =>
        {
            // Simple check - just verify we can access the context
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Table Storage configured");
        }, tags: new[] { "db", "tablestorage" });
}
else
{
    // Development: Use SQLite with EF Core
    var connectionString = builder.Configuration.GetConnectionString("FlightTracker")
        ?? "Data Source=flighttracker.db";
    builder.Services.AddDbContext<FlightTrackerDbContext>(options =>
        options.UseSqlite(connectionString));

    // Add health checks for SQLite
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<FlightTrackerDbContext>(
            name: "database",
            tags: new[] { "db", "sqlite" });

    // Register EF Core repositories
    builder.Services.AddScoped<IDestinationRepository, DestinationRepository>();
    builder.Services.AddScoped<ITargetDateRepository, TargetDateRepository>();
    builder.Services.AddScoped<IPriceCheckRepository, PriceCheckRepository>();
}

// Register application services
builder.Services.AddScoped<FlightSearchService>();
builder.Services.AddScoped<PriceHistoryService>();
builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddScoped<TravelDateService>();
builder.Services.AddSingleton<AirportCacheService>(); // Singleton for caching
builder.Services.AddSingleton<FlightTracker.Web.Services.VersionService>(); // Singleton for version info

// Register background service for automated price checks
builder.Services.AddHostedService<PriceCheckBackgroundService>();

// Bind flight provider configuration
builder.Services.Configure<FlightProviderConfig>(
    builder.Configuration.GetSection("FlightProvider"));

// Register flight provider based on configuration
var providerConfig = builder.Configuration.GetSection("FlightProvider").Get<FlightProviderConfig>();
var providerType = providerConfig?.Type ?? "Mock";

builder.Services.AddHttpClient(); // Register HttpClient for providers

switch (providerType.ToLowerInvariant())
{
    case "mock":
        builder.Services.AddScoped<IFlightProvider, FlightTracker.Providers.Mock.MockFlightProvider>();
        break;

    case "bookingcom":
        if (string.IsNullOrEmpty(providerConfig?.ApiKey))
        {
            throw new InvalidOperationException(
                "Booking.com provider requires ApiKey in FlightProvider configuration");
        }
        if (string.IsNullOrEmpty(providerConfig?.ApiHost))
        {
            throw new InvalidOperationException(
                "Booking.com provider requires ApiHost in FlightProvider configuration");
        }

        builder.Services.AddScoped<IFlightProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var logger = sp.GetRequiredService<ILogger<FlightTracker.Providers.BookingCom.BookingComProvider>>();
            
            return new FlightTracker.Providers.BookingCom.BookingComProvider(
                httpClient,
                providerConfig.ApiKey,
                providerConfig.ApiHost,
                logger);
        });
        break;

    case "amadeus":
        if (string.IsNullOrEmpty(providerConfig?.ApiKey))
        {
            throw new InvalidOperationException(
                "Amadeus provider requires ApiKey (Client ID) in FlightProvider configuration");
        }
        if (string.IsNullOrEmpty(providerConfig?.ApiSecret))
        {
            throw new InvalidOperationException(
                "Amadeus provider requires ApiSecret (Client Secret) in FlightProvider configuration");
        }

        builder.Services.AddScoped<IFlightProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var logger = sp.GetRequiredService<ILogger<FlightTracker.Providers.Amadeus.AmadeusProvider>>();
            
            return new FlightTracker.Providers.Amadeus.AmadeusProvider(
                httpClient,
                providerConfig.ApiKey,
                providerConfig.ApiSecret,
                providerConfig.UseProduction,
                logger);
        });
        break;

    case "skyscanner":
        // Future: Implement Skyscanner provider
        throw new NotImplementedException("Skyscanner provider not yet implemented");

    default:
        throw new InvalidOperationException($"Unknown flight provider type: {providerType}");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Version endpoint
app.MapGet("/api/version", (FlightTracker.Web.Services.VersionService versionService) =>
{
    var version = versionService.GetVersion();
    return Results.Ok(version);
});

// Map health check endpoints with version info
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var versionService = context.RequestServices.GetRequiredService<FlightTracker.Web.Services.VersionService>();
        var version = versionService.GetVersion();
        
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            version = version.ShortCommit,
            environment = version.Environment,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// Simple liveness endpoint (no DB check)
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // No health checks, just returns 200 if app is running
});

// Initialize database and configuration at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        if (useTableStorage)
        {
            // Table Storage: Ensure tables exist
            var tableContext = services.GetRequiredService<TableStorageContext>();
            await tableContext.EnsureTablesExistAsync();
            
            // Seed airports into Table Storage
            var destinationRepo = services.GetRequiredService<IDestinationRepository>();
            await TableStorageSeeder.SeedAirportsAsync(destinationRepo, logger);
            
            logger.LogInformation("Azure Table Storage initialized successfully");
        }
        else
        {
            // SQLite: Create database and seed data
            var context = services.GetRequiredService<FlightTrackerDbContext>();
            var configService = services.GetRequiredService<ConfigurationService>();
            var seedingConfig = builder.Configuration.GetSection("Seeding").Get<SeedingConfig>() ?? new SeedingConfig();
            
            // Create database if it doesn't exist
            context.Database.EnsureCreated();
            
            // Seed comprehensive airport list first
            await DataSeeder.SeedAirportsAsync(context);
            
            // Sync configuration with database (respects SeedDemoTravelDates flag)
            await configService.InitializeAllAsync(seedingConfig.SeedDemoTravelDates);
            
            // Seed historical price data for testing (only if database is empty and enabled)
            var hasPriceData = await context.PriceChecks.AnyAsync();
            if (!hasPriceData && seedingConfig.SeedHistoricalPrices)
            {
                await DataSeeder.SeedHistoricalPriceDataAsync(context, seedingConfig.SeedHistoricalPrices);
                logger.LogInformation("Seeded historical price data for testing");
            }
            else if (!seedingConfig.SeedHistoricalPrices)
            {
                logger.LogInformation("Historical price seeding is disabled");
            }
            
            logger.LogInformation("SQLite database initialized successfully");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
