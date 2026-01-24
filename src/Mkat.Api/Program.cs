using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mkat.Api.Middleware;
using Mkat.Application.DTOs;
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
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console());

    var databasePath = builder.Configuration["MKAT_DATABASE_PATH"] ?? "mkat.db";

    builder.Services.AddDbContext<MkatDbContext>(options =>
        options.UseSqlite($"Data Source={databasePath}"));

    builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
    builder.Services.AddScoped<IMonitorRepository, MonitorRepository>();
    builder.Services.AddScoped<IAlertRepository, AlertRepository>();
    builder.Services.AddScoped<IMuteWindowRepository, MuteWindowRepository>();
    builder.Services.AddScoped<IMetricReadingRepository, MetricReadingRepository>();
    builder.Services.AddScoped<IPeerRepository, PeerRepository>();
    builder.Services.AddScoped<IContactRepository, ContactRepository>();
    builder.Services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
    builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MkatDbContext>());
    builder.Services.AddScoped<IStateService, StateService>();
    builder.Services.AddScoped<IMetricEvaluator, MetricEvaluator>();
    builder.Services.AddSingleton<IPairingService, PairingService>();
    builder.Services.AddSingleton<IEventBroadcaster, EventBroadcaster>();

    builder.Services.AddControllers();
    builder.Services.AddRouting(options => options.LowercaseUrls = true);

    builder.Services.AddScoped<IValidator<CreateServiceRequest>, CreateServiceValidator>();
    builder.Services.AddScoped<IValidator<UpdateServiceRequest>, UpdateServiceValidator>();
    builder.Services.AddScoped<IValidator<AddMonitorRequest>, AddMonitorValidator>();
    builder.Services.AddScoped<IValidator<UpdateMonitorRequest>, UpdateMonitorValidator>();
    builder.Services.AddScoped<IValidator<CreateContactRequest>, CreateContactValidator>();
    builder.Services.AddScoped<IValidator<UpdateContactRequest>, UpdateContactValidator>();
    builder.Services.AddScoped<IValidator<AddChannelRequest>, AddChannelValidator>();
    builder.Services.AddScoped<IValidator<UpdateChannelRequest>, UpdateChannelValidator>();
    builder.Services.AddScoped<IValidator<SetServiceContactsRequest>, SetServiceContactsValidator>();
    builder.Services.AddScoped<IValidator<PeerInitiateRequest>, PeerInitiateValidator>();
    builder.Services.AddScoped<IValidator<PeerAcceptRequest>, PeerAcceptValidator>();
    builder.Services.AddScoped<IValidator<PeerCompleteRequest>, PeerCompleteValidator>();

    builder.Services.Configure<TelegramOptions>(options =>
    {
        options.BotToken = Environment.GetEnvironmentVariable("MKAT_TELEGRAM_BOT_TOKEN") ?? "";
        options.ChatId = Environment.GetEnvironmentVariable("MKAT_TELEGRAM_CHAT_ID") ?? "";
    });

    builder.Services.AddSingleton<INotificationChannel, TelegramChannel>();
    builder.Services.AddScoped<IContactChannelSender, ContactChannelSender>();
    builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

    builder.Services.AddHostedService<HeartbeatMonitorWorker>();
    builder.Services.AddHostedService<MaintenanceResumeWorker>();
    builder.Services.AddHostedService<AlertDispatchWorker>();
    builder.Services.AddHostedService<TelegramBotService>();
    builder.Services.AddHostedService<MetricRetentionWorker>();
    builder.Services.AddHttpClient();
    builder.Services.AddHostedService<PeerHeartbeatWorker>();

    var app = builder.Build();

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

    var basePath = Environment.GetEnvironmentVariable("MKAT_BASE_PATH") ?? "";
    if (!string.IsNullOrEmpty(basePath))
    {
        if (!basePath.StartsWith('/')) basePath = "/" + basePath;
        basePath = basePath.TrimEnd('/');
        app.UsePathBase(basePath);
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
    // Pre-compute the fallback HTML with injected base path config
    string? cachedFallbackHtml = null;
    app.MapFallback(async context =>
    {
        if (cachedFallbackHtml == null)
        {
            var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
            var indexPath = Path.Combine(webRootPath, "index.html");
            if (!File.Exists(indexPath))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var html = await File.ReadAllTextAsync(indexPath, context.RequestAborted);

            // Escape base path for safe injection into JavaScript string
            var escapedBasePath = basePath
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("<", "\\u003c")
                .Replace(">", "\\u003e");
            var configScript = $"<script>window.__MKAT_BASE_PATH__=\"{escapedBasePath}\";</script>";
            cachedFallbackHtml = html.Replace("</head>", $"{configScript}\n</head>");
        }

        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(cachedFallbackHtml, context.RequestAborted);
    });

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
