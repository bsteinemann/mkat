using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Mkat.Api.Middleware;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Application.Validators;
using Mkat.Infrastructure.Channels;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Mkat.Infrastructure.Services;
using Mkat.Infrastructure.Workers;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    var databasePath = builder.Configuration["MKAT_DATABASE_PATH"] ?? "mkat.db";

    builder.Services.AddDbContext<MkatDbContext>(options =>
        options.UseSqlite($"Data Source={databasePath}"));

    builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
    builder.Services.AddScoped<IMonitorRepository, MonitorRepository>();
    builder.Services.AddScoped<IAlertRepository, AlertRepository>();
    builder.Services.AddScoped<IMuteWindowRepository, MuteWindowRepository>();
    builder.Services.AddScoped<IPeerRepository, PeerRepository>();
    builder.Services.AddScoped<IContactRepository, ContactRepository>();
    builder.Services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
    builder.Services.AddScoped<IMonitorEventRepository, MonitorEventRepository>();
    builder.Services.AddScoped<IMonitorRollupRepository, MonitorRollupRepository>();
    builder.Services.AddScoped<IRollupCalculator, RollupCalculator>();
    builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MkatDbContext>());
    builder.Services.AddScoped<IStateService, StateService>();
    builder.Services.AddScoped<IMetricEvaluator, MetricEvaluator>();
    builder.Services.AddSingleton<IPairingService, PairingService>();
    builder.Services.AddSingleton<IEventBroadcaster, EventBroadcaster>();

    builder.Services.AddControllers();
    builder.Services.AddRouting(options => options.LowercaseUrls = true);

    builder.Services.AddValidatorsFromAssemblyContaining<CreateServiceValidator>();

    builder.Services.Configure<TelegramOptions>(options =>
    {
        options.BotToken = Environment.GetEnvironmentVariable("MKAT_TELEGRAM_BOT_TOKEN") ?? "";
        options.ChatId = Environment.GetEnvironmentVariable("MKAT_TELEGRAM_CHAT_ID") ?? "";
    });

    builder.Services.AddSingleton<INotificationChannel, TelegramChannel>();

    builder.Services.Configure<VapidOptions>(options =>
    {
        options.PublicKey = Environment.GetEnvironmentVariable("MKAT_VAPID_PUBLIC_KEY") ?? "";
        options.PrivateKey = Environment.GetEnvironmentVariable("MKAT_VAPID_PRIVATE_KEY") ?? "";
        options.Subject = Environment.GetEnvironmentVariable("MKAT_VAPID_SUBJECT") ?? "mailto:admin@localhost";
    });
    builder.Services.AddScoped<INotificationChannel, WebPushChannel>();

    builder.Services.AddScoped<IContactChannelSender, ContactChannelSender>();
    builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

    builder.Services.AddHostedService<HeartbeatMonitorWorker>();
    builder.Services.AddHostedService<MaintenanceResumeWorker>();
    builder.Services.AddHostedService<AlertDispatchWorker>();
    builder.Services.AddHostedService<TelegramBotService>();
    builder.Services.AddHostedService<EventRetentionWorker>();
    builder.Services.AddHostedService<HealthCheckWorker>();
    builder.Services.AddHostedService<RollupAggregationWorker>();
    builder.Services.AddHttpClient();
    builder.Services.AddHostedService<PeerHeartbeatWorker>();

    // Configure forwarded headers for reverse proxy (Traefik, nginx, etc.)
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        // Clear default limits to trust all proxies in containerized environments
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    var app = builder.Build();

    // Validate required configuration before starting
    var mkatPassword = Environment.GetEnvironmentVariable("MKAT_PASSWORD");
    if (string.IsNullOrEmpty(mkatPassword))
    {
        Log.Fatal("MKAT_PASSWORD environment variable is not set. Set this variable to enable authentication before starting the application");
        return;
    }

    // Forwarded headers must be first to ensure correct scheme/host in Request
    app.UseForwardedHeaders();

    // App is served at /mkat - must be FIRST before any routing
    app.UsePathBase("/mkat");
    app.UseRouting();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        try
        {
            if (db.Database.GetPendingMigrations().Any())
            {
                db.Database.Migrate();
            }
            else
            {
                db.Database.EnsureCreated();
            }
        }
        catch (InvalidOperationException)
        {
            // InMemory or other providers that don't support migrations
            db.Database.EnsureCreated();
        }
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        };
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseMiddleware<BasicAuthMiddleware>();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
    app.MapGet("/health/ready", (MkatDbContext db) =>
    {
        try
        {
            db.Database.CanConnect();
            return Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow });
        }
        catch
        {
            return Results.StatusCode(503);
        }
    });

    app.MapControllers();
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class accessible for integration tests
public partial class Program { }
