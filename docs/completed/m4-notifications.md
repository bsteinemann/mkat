# Implementation Plan: M4 Notifications

**Milestone:** 4 - Notifications
**Goal:** Alert system and Telegram integration
**Dependencies:** M3 Monitoring Engine

---

## 1. Notification Channel Interface

**File:** `src/Mkat.Application/Interfaces/INotificationChannel.cs`
```csharp
namespace Mkat.Application.Interfaces;

public interface INotificationChannel
{
    string ChannelType { get; }
    bool IsEnabled { get; }
    Task<bool> SendAlertAsync(Alert alert, Service service, CancellationToken ct = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken ct = default);
}
```

**File:** `src/Mkat.Application/Interfaces/INotificationDispatcher.cs`
```csharp
namespace Mkat.Application.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchAsync(Alert alert, CancellationToken ct = default);
}
```

---

## 2. Notification Dispatcher

**File:** `src/Mkat.Application/Services/NotificationDispatcher.cs`
```csharp
namespace Mkat.Application.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly IServiceRepository _serviceRepo;
    private readonly IAlertRepository _alertRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEnumerable<INotificationChannel> channels,
        IServiceRepository serviceRepo,
        IAlertRepository alertRepo,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDispatcher> logger)
    {
        _channels = channels;
        _serviceRepo = serviceRepo;
        _alertRepo = alertRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task DispatchAsync(Alert alert, CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(alert.ServiceId, ct);
        if (service == null)
        {
            _logger.LogWarning("Cannot dispatch alert {AlertId}: service not found", alert.Id);
            return;
        }

        var enabledChannels = _channels.Where(c => c.IsEnabled).ToList();
        if (!enabledChannels.Any())
        {
            _logger.LogWarning("No enabled notification channels, alert {AlertId} not sent", alert.Id);
            return;
        }

        var allSucceeded = true;
        foreach (var channel in enabledChannels)
        {
            try
            {
                var success = await channel.SendAlertAsync(alert, service, ct);
                if (!success)
                {
                    _logger.LogWarning(
                        "Channel {ChannelType} failed to send alert {AlertId}",
                        channel.ChannelType, alert.Id);
                    allSucceeded = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending alert {AlertId} via channel {ChannelType}",
                    alert.Id, channel.ChannelType);
                allSucceeded = false;
            }
        }

        if (allSucceeded)
        {
            alert.DispatchedAt = DateTime.UtcNow;
            await _alertRepo.UpdateAsync(alert, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
```

---

## 3. Telegram Channel Implementation

### 3.1 NuGet Package

```bash
cd src/Mkat.Infrastructure
dotnet add package Telegram.Bot
```

### 3.2 Telegram Configuration

**File:** `src/Mkat.Infrastructure/Channels/TelegramOptions.cs`
```csharp
namespace Mkat.Infrastructure.Channels;

public class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}
```

### 3.3 Telegram Channel

**File:** `src/Mkat.Infrastructure/Channels/TelegramChannel.cs`
```csharp
namespace Mkat.Infrastructure.Channels;

public class TelegramChannel : INotificationChannel
{
    private readonly TelegramBotClient _client;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramChannel> _logger;

    public string ChannelType => "telegram";
    public bool IsEnabled => !string.IsNullOrEmpty(_options.BotToken) &&
                             !string.IsNullOrEmpty(_options.ChatId);

    public TelegramChannel(IOptions<TelegramOptions> options, ILogger<TelegramChannel> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (IsEnabled)
        {
            _client = new TelegramBotClient(_options.BotToken);
        }
    }

    public async Task<bool> SendAlertAsync(Alert alert, Service service, CancellationToken ct = default)
    {
        if (!IsEnabled) return false;

        var message = FormatAlertMessage(alert, service);
        var keyboard = CreateInlineKeyboard(alert, service);

        for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                await _client.SendTextMessageAsync(
                    chatId: _options.ChatId,
                    text: message,
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: keyboard,
                    cancellationToken: ct);

                _logger.LogInformation(
                    "Telegram alert sent for service {ServiceId}, alert {AlertId}",
                    service.Id, alert.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Telegram send attempt {Attempt}/{MaxRetries} failed",
                    attempt, _options.MaxRetries);

                if (attempt < _options.MaxRetries)
                {
                    await Task.Delay(_options.RetryDelayMs * attempt, ct);
                }
            }
        }

        return false;
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return false;

        try
        {
            var me = await _client.GetMeAsync(ct);
            _logger.LogInformation("Telegram bot validated: {BotName}", me.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram configuration validation failed");
            return false;
        }
    }

    private string FormatAlertMessage(Alert alert, Service service)
    {
        var emoji = alert.Type switch
        {
            AlertType.Failure => "\u26a0\ufe0f",      // Warning
            AlertType.MissedHeartbeat => "\U0001f494", // Broken heart
            AlertType.Recovery => "\u2705",           // Check mark
            _ => "\u2139\ufe0f"                        // Info
        };

        var severityText = service.Severity switch
        {
            Severity.Critical => "\U0001f534 CRITICAL",
            Severity.High => "\U0001f7e0 HIGH",
            Severity.Medium => "\U0001f7e1 MEDIUM",
            Severity.Low => "\U0001f7e2 LOW",
            _ => "UNKNOWN"
        };

        var stateText = alert.Type == AlertType.Recovery ? "RECOVERED" : "DOWN";

        // Escape markdown special characters
        var name = EscapeMarkdown(service.Name);
        var msg = EscapeMarkdown(alert.Message);
        var time = alert.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC");

        return $@"{emoji} *{stateText}*: {name}

{severityText}
{msg}

_{time}_";
    }

    private InlineKeyboardMarkup CreateInlineKeyboard(Alert alert, Service service)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Acknowledge", $"ack:{alert.Id}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Mute 15m", $"mute:{service.Id}:15"),
                InlineKeyboardButton.WithCallbackData("Mute 1h", $"mute:{service.Id}:60"),
                InlineKeyboardButton.WithCallbackData("Mute 24h", $"mute:{service.Id}:1440"),
            }
        });
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var specialChars = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        foreach (var c in specialChars)
        {
            text = text.Replace(c.ToString(), $"\\{c}");
        }
        return text;
    }
}
```

---

## 4. Telegram Bot Commands & Callbacks

### 4.1 Telegram Bot Service

**File:** `src/Mkat.Infrastructure/Channels/TelegramBotService.cs`
```csharp
namespace Mkat.Infrastructure.Channels;

public class TelegramBotService : BackgroundService
{
    private readonly TelegramBotClient _client;
    private readonly TelegramOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        IOptions<TelegramOptions> options,
        IServiceProvider serviceProvider,
        ILogger<TelegramBotService> logger)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        if (!string.IsNullOrEmpty(_options.BotToken))
        {
            _client = new TelegramBotClient(_options.BotToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_client == null)
        {
            _logger.LogWarning("Telegram bot not configured, skipping");
            return;
        }

        _logger.LogInformation("Starting Telegram bot polling");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        };

        await _client.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient client,
        Update update,
        CancellationToken ct)
    {
        try
        {
            if (update.Message?.Text != null)
            {
                await HandleCommandAsync(update.Message, ct);
            }
            else if (update.CallbackQuery != null)
            {
                await HandleCallbackAsync(update.CallbackQuery, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Telegram update");
        }
    }

    private Task HandleErrorAsync(
        ITelegramBotClient client,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(exception, "Telegram polling error");
        return Task.CompletedTask;
    }

    private async Task HandleCommandAsync(Message message, CancellationToken ct)
    {
        var text = message.Text ?? "";
        var chatId = message.Chat.Id.ToString();

        // Only respond to configured chat
        if (chatId != _options.ChatId) return;

        using var scope = _serviceProvider.CreateScope();

        if (text.StartsWith("/status"))
        {
            await HandleStatusCommandAsync(message.Chat.Id, scope, ct);
        }
        else if (text.StartsWith("/list"))
        {
            await HandleListCommandAsync(message.Chat.Id, scope, ct);
        }
        else if (text.StartsWith("/mute"))
        {
            await HandleMuteCommandAsync(message.Chat.Id, text, scope, ct);
        }
    }

    private async Task HandleStatusCommandAsync(
        long chatId,
        IServiceScope scope,
        CancellationToken ct)
    {
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var services = await serviceRepo.GetAllAsync(0, 1000, ct);

        var up = services.Count(s => s.State == ServiceState.Up);
        var down = services.Count(s => s.State == ServiceState.Down);
        var paused = services.Count(s => s.State == ServiceState.Paused);
        var unknown = services.Count(s => s.State == ServiceState.Unknown);

        var message = $@"\U0001f4ca *Status Overview*

\u2705 Up: {up}
\u274c Down: {down}
\u23f8 Paused: {paused}
\u2753 Unknown: {unknown}

Total: {services.Count} services";

        await _client.SendTextMessageAsync(chatId, message, parseMode: ParseMode.MarkdownV2, cancellationToken: ct);
    }

    private async Task HandleListCommandAsync(
        long chatId,
        IServiceScope scope,
        CancellationToken ct)
    {
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var services = await serviceRepo.GetAllAsync(0, 50, ct);

        if (!services.Any())
        {
            await _client.SendTextMessageAsync(chatId, "No services configured.", cancellationToken: ct);
            return;
        }

        var lines = services.Select(s =>
        {
            var emoji = s.State switch
            {
                ServiceState.Up => "\u2705",
                ServiceState.Down => "\u274c",
                ServiceState.Paused => "\u23f8",
                _ => "\u2753"
            };
            return $"{emoji} {EscapeMarkdown(s.Name)}";
        });

        var message = "*Services:*\n\n" + string.Join("\n", lines);
        await _client.SendTextMessageAsync(chatId, message, parseMode: ParseMode.MarkdownV2, cancellationToken: ct);
    }

    private async Task HandleMuteCommandAsync(
        long chatId,
        string text,
        IServiceScope scope,
        CancellationToken ct)
    {
        // Parse: /mute <service-name> <duration>
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            await _client.SendTextMessageAsync(
                chatId,
                "Usage: /mute <service-name> <duration>\nDuration: 15m, 1h, 24h, etc.",
                cancellationToken: ct);
            return;
        }

        var serviceName = parts[1];
        var durationStr = parts[2];

        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var muteRepo = scope.ServiceProvider.GetRequiredService<IMuteWindowRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var services = await serviceRepo.GetAllAsync(0, 1000, ct);
        var service = services.FirstOrDefault(s =>
            s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

        if (service == null)
        {
            await _client.SendTextMessageAsync(chatId, $"Service '{serviceName}' not found.", cancellationToken: ct);
            return;
        }

        var minutes = ParseDuration(durationStr);
        if (minutes <= 0)
        {
            await _client.SendTextMessageAsync(chatId, "Invalid duration format.", cancellationToken: ct);
            return;
        }

        var mute = new MuteWindow
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddMinutes(minutes),
            Reason = "Muted via Telegram",
            CreatedAt = DateTime.UtcNow
        };

        await muteRepo.AddAsync(mute, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await _client.SendTextMessageAsync(
            chatId,
            $"\u2705 Service '{service.Name}' muted for {durationStr}.",
            cancellationToken: ct);
    }

    private async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken ct)
    {
        var data = callback.Data ?? "";
        using var scope = _serviceProvider.CreateScope();

        if (data.StartsWith("ack:"))
        {
            var alertId = Guid.Parse(data["ack:".Length..]);
            await HandleAcknowledgeAsync(callback, alertId, scope, ct);
        }
        else if (data.StartsWith("mute:"))
        {
            var parts = data["mute:".Length..].Split(':');
            var serviceId = Guid.Parse(parts[0]);
            var minutes = int.Parse(parts[1]);
            await HandleMuteAsync(callback, serviceId, minutes, scope, ct);
        }

        await _client.AnswerCallbackQueryAsync(callback.Id, cancellationToken: ct);
    }

    private async Task HandleAcknowledgeAsync(
        CallbackQuery callback,
        Guid alertId,
        IServiceScope scope,
        CancellationToken ct)
    {
        var alertRepo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var alert = await alertRepo.GetByIdAsync(alertId, ct);
        if (alert == null)
        {
            await _client.AnswerCallbackQueryAsync(callback.Id, "Alert not found", cancellationToken: ct);
            return;
        }

        if (alert.AcknowledgedAt.HasValue)
        {
            await _client.AnswerCallbackQueryAsync(callback.Id, "Already acknowledged", cancellationToken: ct);
            return;
        }

        alert.AcknowledgedAt = DateTime.UtcNow;
        await alertRepo.UpdateAsync(alert, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await _client.EditMessageReplyMarkupAsync(
            callback.Message!.Chat.Id,
            callback.Message.MessageId,
            replyMarkup: null,
            cancellationToken: ct);

        _logger.LogInformation("Alert {AlertId} acknowledged via Telegram", alertId);
    }

    private async Task HandleMuteAsync(
        CallbackQuery callback,
        Guid serviceId,
        int minutes,
        IServiceScope scope,
        CancellationToken ct)
    {
        var muteRepo = scope.ServiceProvider.GetRequiredService<IMuteWindowRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var mute = new MuteWindow
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddMinutes(minutes),
            Reason = "Muted via Telegram button",
            CreatedAt = DateTime.UtcNow
        };

        await muteRepo.AddAsync(mute, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await _client.AnswerCallbackQueryAsync(
            callback.Id,
            $"Service muted for {minutes} minutes",
            cancellationToken: ct);

        _logger.LogInformation("Service {ServiceId} muted for {Minutes}m via Telegram", serviceId, minutes);
    }

    private static int ParseDuration(string duration)
    {
        duration = duration.ToLowerInvariant().Trim();

        if (duration.EndsWith("m") && int.TryParse(duration[..^1], out var minutes))
            return minutes;
        if (duration.EndsWith("h") && int.TryParse(duration[..^1], out var hours))
            return hours * 60;
        if (duration.EndsWith("d") && int.TryParse(duration[..^1], out var days))
            return days * 24 * 60;

        return -1;
    }

    private static string EscapeMarkdown(string text)
    {
        var specialChars = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        foreach (var c in specialChars)
        {
            text = text.Replace(c.ToString(), $"\\{c}");
        }
        return text;
    }
}
```

---

## 5. Alert Dispatch Worker

**File:** `src/Mkat.Infrastructure/Workers/AlertDispatchWorker.cs`
```csharp
namespace Mkat.Infrastructure.Workers;

public class AlertDispatchWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertDispatchWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

    public AlertDispatchWorker(
        IServiceProvider serviceProvider,
        ILogger<AlertDispatchWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertDispatchWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AlertDispatchWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("AlertDispatchWorker stopping");
    }

    private async Task DispatchPendingAlertsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        var pendingAlerts = await alertRepo.GetPendingDispatchAsync(ct);

        foreach (var alert in pendingAlerts)
        {
            await dispatcher.DispatchAsync(alert, ct);
        }
    }
}
```

---

## 6. Alert API Endpoints

**File:** `src/Mkat.Api/Controllers/AlertsController.cs`
```csharp
namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertRepository _alertRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AlertsController(
        IAlertRepository alertRepo,
        IServiceRepository serviceRepo,
        IUnitOfWork unitOfWork)
    {
        _alertRepo = alertRepo;
        _serviceRepo = serviceRepo;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AlertResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var skip = (page - 1) * pageSize;
        var alerts = await _alertRepo.GetAllAsync(skip, pageSize, ct);
        var totalCount = await _alertRepo.GetCountAsync(ct);

        return Ok(new PagedResponse<AlertResponse>
        {
            Items = alerts.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlertResponse>> GetById(Guid id, CancellationToken ct = default)
    {
        var alert = await _alertRepo.GetByIdAsync(id, ct);
        if (alert == null)
        {
            return NotFound(new ErrorResponse { Error = "Alert not found", Code = "ALERT_NOT_FOUND" });
        }

        return Ok(MapToResponse(alert));
    }

    [HttpPost("{id:guid}/ack")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct = default)
    {
        var alert = await _alertRepo.GetByIdAsync(id, ct);
        if (alert == null)
        {
            return NotFound(new ErrorResponse { Error = "Alert not found", Code = "ALERT_NOT_FOUND" });
        }

        if (alert.AcknowledgedAt.HasValue)
        {
            return BadRequest(new ErrorResponse { Error = "Alert already acknowledged", Code = "ALREADY_ACKNOWLEDGED" });
        }

        alert.AcknowledgedAt = DateTime.UtcNow;
        await _alertRepo.UpdateAsync(alert, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { acknowledged = true });
    }

    private static AlertResponse MapToResponse(Alert alert) => new()
    {
        Id = alert.Id,
        ServiceId = alert.ServiceId,
        Type = alert.Type,
        Severity = alert.Severity,
        Message = alert.Message,
        CreatedAt = alert.CreatedAt,
        AcknowledgedAt = alert.AcknowledgedAt,
        DispatchedAt = alert.DispatchedAt
    };
}
```

---

## 7. Mute Endpoint

**Add to:** `src/Mkat.Api/Controllers/ServicesController.cs`
```csharp
[HttpPost("{id:guid}/mute")]
public async Task<IActionResult> Mute(
    Guid id,
    [FromBody] MuteRequest request,
    CancellationToken ct = default)
{
    var service = await _serviceRepo.GetByIdAsync(id, ct);
    if (service == null)
    {
        return NotFound(new ErrorResponse { Error = "Service not found", Code = "SERVICE_NOT_FOUND" });
    }

    var mute = new MuteWindow
    {
        Id = Guid.NewGuid(),
        ServiceId = id,
        StartsAt = DateTime.UtcNow,
        EndsAt = DateTime.UtcNow.AddMinutes(request.DurationMinutes),
        Reason = request.Reason,
        CreatedAt = DateTime.UtcNow
    };

    await _muteRepo.AddAsync(mute, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    return Ok(new { muted = true, until = mute.EndsAt });
}
```

**File:** `src/Mkat.Application/DTOs/MuteRequest.cs`
```csharp
namespace Mkat.Application.DTOs;

public record MuteRequest
{
    public int DurationMinutes { get; init; }
    public string? Reason { get; init; }
}
```

---

## 8. DI Registration

**Update:** `src/Mkat.Api/Program.cs`
```csharp
// Telegram configuration
builder.Services.Configure<TelegramOptions>(options =>
{
    options.BotToken = Environment.GetEnvironmentVariable("MKAT_TELEGRAM_BOT_TOKEN") ?? "";
    options.ChatId = Environment.GetEnvironmentVariable("MKAT_TELEGRAM_CHAT_ID") ?? "";
});

// Notification channels
builder.Services.AddSingleton<INotificationChannel, TelegramChannel>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

// Background workers
builder.Services.AddHostedService<AlertDispatchWorker>();
builder.Services.AddHostedService<TelegramBotService>();
```

---

## 9. Verification Checklist

- [ ] Alerts created on state transition are dispatched
- [ ] Telegram messages formatted correctly
- [ ] Inline buttons appear on messages
- [ ] Acknowledge button marks alert as acknowledged
- [ ] Mute buttons create mute windows
- [ ] `/status` command returns service counts
- [ ] `/list` command shows service list
- [ ] `/mute` command creates mute window
- [ ] Muted services don't generate alerts
- [ ] Retry logic works on Telegram failures
- [ ] `GET /api/v1/alerts` returns paginated list
- [ ] `POST /api/v1/alerts/{id}/ack` acknowledges alert
- [ ] `POST /api/v1/services/{id}/mute` mutes service

---

## 10. Files to Create/Update

| File | Action | Purpose |
|------|--------|---------|
| `src/Mkat.Application/Interfaces/INotificationChannel.cs` | Create | Channel interface |
| `src/Mkat.Application/Interfaces/INotificationDispatcher.cs` | Create | Dispatcher interface |
| `src/Mkat.Application/Services/NotificationDispatcher.cs` | Create | Dispatch logic |
| `src/Mkat.Infrastructure/Channels/TelegramOptions.cs` | Create | Config model |
| `src/Mkat.Infrastructure/Channels/TelegramChannel.cs` | Create | Telegram sending |
| `src/Mkat.Infrastructure/Channels/TelegramBotService.cs` | Create | Bot commands |
| `src/Mkat.Infrastructure/Workers/AlertDispatchWorker.cs` | Create | Background worker |
| `src/Mkat.Api/Controllers/AlertsController.cs` | Create | Alert endpoints |
| `src/Mkat.Api/Controllers/ServicesController.cs` | Update | Add mute endpoint |
| `src/Mkat.Application/DTOs/MuteRequest.cs` | Create | Mute request model |
| `src/Mkat.Application/DTOs/AlertResponse.cs` | Create | Alert response model |

---

**Status:** Ready for implementation
**Estimated complexity:** High
