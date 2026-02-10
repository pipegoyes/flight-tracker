using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;
using FlightTracker.Core.Services;
using FlightTracker.Data;
using FlightTracker.Data.Repositories;
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

// Configure database
var connectionString = builder.Configuration.GetConnectionString("FlightTracker")
    ?? "Data Source=flighttracker.db";
builder.Services.AddDbContext<FlightTrackerDbContext>(options =>
    options.UseSqlite(connectionString));

// Register repositories
builder.Services.AddScoped<IDestinationRepository, DestinationRepository>();
builder.Services.AddScoped<ITargetDateRepository, TargetDateRepository>();
builder.Services.AddScoped<IPriceCheckRepository, PriceCheckRepository>();

// Register application services
builder.Services.AddScoped<FlightSearchService>();
builder.Services.AddScoped<PriceHistoryService>();
builder.Services.AddScoped<ConfigurationService>();

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

// Initialize database and configuration at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<FlightTrackerDbContext>();
        var configService = services.GetRequiredService<ConfigurationService>();
        
        // Create database if it doesn't exist
        context.Database.EnsureCreated();
        
        // Sync configuration with database
        await configService.InitializeAllAsync();
        
        // Seed historical price data for testing (only if database is empty)
        var hasPriceData = await context.PriceChecks.AnyAsync();
        if (!hasPriceData)
        {
            await DataSeeder.SeedHistoricalPriceDataAsync(context);
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Seeded historical price data for testing");
        }
        
        var logger2 = services.GetRequiredService<ILogger<Program>>();
        logger2.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

app.Run();
