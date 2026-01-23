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
    builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MkatDbContext>());
    builder.Services.AddScoped<IStateService, StateService>();
    builder.Services.AddScoped<IMetricEvaluator, MetricEvaluator>();

    builder.Services.AddControllers();
    builder.Services.AddRouting(options => options.LowercaseUrls = true);

    builder.Services.AddScoped<IValidator<CreateServiceRequest>, CreateServiceValidator>();
    builder.Services.AddScoped<IValidator<UpdateServiceRequest>, UpdateServiceValidator>();
    builder.Services.AddScoped<IValidator<AddMonitorRequest>, AddMonitorValidator>();
    builder.Services.AddScoped<IValidator<UpdateMonitorRequest>, UpdateMonitorValidator>();

    builder.Services.Configure<TelegramOptions>(options =>
    {
        options.BotToken = Environment.GetEnvironmentVariable("MKAT_TELEGRAM_BOT_TOKEN") ?? "";
        options.ChatId = Environment.GetEnvironmentVariable("MKAT_TELEGRAM_CHAT_ID") ?? "";
    });

    builder.Services.AddSingleton<INotificationChannel, TelegramChannel>();
    builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

    builder.Services.AddHostedService<HeartbeatMonitorWorker>();
    builder.Services.AddHostedService<MaintenanceResumeWorker>();
    builder.Services.AddHostedService<AlertDispatchWorker>();
    builder.Services.AddHostedService<TelegramBotService>();

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
