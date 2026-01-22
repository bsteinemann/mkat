using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Mkat.Infrastructure.Channels;

public class TelegramBotService : BackgroundService
{
    private readonly TelegramBotClient? _client;
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
            try
            {
                _client = new TelegramBotClient(_options.BotToken);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid Telegram bot token format");
            }
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

        int offset = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _client.GetUpdates(
                    offset,
                    allowedUpdates: [UpdateType.Message, UpdateType.CallbackQuery],
                    cancellationToken: stoppingToken);

                foreach (var update in updates)
                {
                    offset = update.Id + 1;
                    await HandleUpdateAsync(update, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram polling error");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("Telegram bot polling stopped");
    }

    private async Task HandleUpdateAsync(Update update, CancellationToken ct)
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

    private async Task HandleCommandAsync(Message message, CancellationToken ct)
    {
        var text = message.Text ?? "";
        var chatId = message.Chat.Id.ToString();

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

    private async Task HandleStatusCommandAsync(long chatId, IServiceScope scope, CancellationToken ct)
    {
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var services = await serviceRepo.GetAllAsync(0, 1000, ct);

        var up = services.Count(s => s.State == ServiceState.Up);
        var down = services.Count(s => s.State == ServiceState.Down);
        var paused = services.Count(s => s.State == ServiceState.Paused);
        var unknown = services.Count(s => s.State == ServiceState.Unknown);

        var msg = $"\U0001f4ca *Status Overview*\n\n\u2705 Up: {up}\n\u274c Down: {down}\n\u23f8 Paused: {paused}\n\u2753 Unknown: {unknown}\n\nTotal: {services.Count} services";

        await _client!.SendMessage(chatId, msg, parseMode: ParseMode.MarkdownV2, cancellationToken: ct);
    }

    private async Task HandleListCommandAsync(long chatId, IServiceScope scope, CancellationToken ct)
    {
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var services = await serviceRepo.GetAllAsync(0, 50, ct);

        if (!services.Any())
        {
            await _client!.SendMessage(chatId, "No services configured\\.", cancellationToken: ct);
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
            return $"{emoji} {TelegramChannel.EscapeMarkdown(s.Name)}";
        });

        var msg = "*Services:*\n\n" + string.Join("\n", lines);
        await _client!.SendMessage(chatId, msg, parseMode: ParseMode.MarkdownV2, cancellationToken: ct);
    }

    private async Task HandleMuteCommandAsync(long chatId, string text, IServiceScope scope, CancellationToken ct)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            await _client!.SendMessage(
                chatId,
                "Usage: /mute <service\\-name> <duration>\nDuration: 15m, 1h, 24h, etc\\.",
                parseMode: ParseMode.MarkdownV2,
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
            await _client!.SendMessage(chatId, $"Service '{TelegramChannel.EscapeMarkdown(serviceName)}' not found\\.", parseMode: ParseMode.MarkdownV2, cancellationToken: ct);
            return;
        }

        var minutes = ParseDuration(durationStr);
        if (minutes <= 0)
        {
            await _client!.SendMessage(chatId, "Invalid duration format\\.", parseMode: ParseMode.MarkdownV2, cancellationToken: ct);
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

        await _client!.SendMessage(
            chatId,
            $"\u2705 Service '{TelegramChannel.EscapeMarkdown(service.Name)}' muted for {TelegramChannel.EscapeMarkdown(durationStr)}\\.",
            parseMode: ParseMode.MarkdownV2,
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
            await HandleMuteCallbackAsync(callback, serviceId, minutes, scope, ct);
        }

        await _client!.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
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
            await _client!.AnswerCallbackQuery(callback.Id, "Alert not found", cancellationToken: ct);
            return;
        }

        if (alert.AcknowledgedAt.HasValue)
        {
            await _client!.AnswerCallbackQuery(callback.Id, "Already acknowledged", cancellationToken: ct);
            return;
        }

        alert.AcknowledgedAt = DateTime.UtcNow;
        await alertRepo.UpdateAsync(alert, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await _client!.EditMessageReplyMarkup(
            callback.Message!.Chat.Id,
            callback.Message.Id,
            replyMarkup: null,
            cancellationToken: ct);

        _logger.LogInformation("Alert {AlertId} acknowledged via Telegram", alertId);
    }

    private async Task HandleMuteCallbackAsync(
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

        await _client!.AnswerCallbackQuery(
            callback.Id,
            $"Service muted for {minutes} minutes",
            cancellationToken: ct);

        _logger.LogInformation("Service {ServiceId} muted for {Minutes}m via Telegram", serviceId, minutes);
    }

    public static int ParseDuration(string duration)
    {
        duration = duration.ToLowerInvariant().Trim();

        if (duration.EndsWith("m") && int.TryParse(duration[..^1], out var minutes) && minutes > 0)
            return minutes;
        if (duration.EndsWith("h") && int.TryParse(duration[..^1], out var hours) && hours > 0)
            return hours * 60;
        if (duration.EndsWith("d") && int.TryParse(duration[..^1], out var days) && days > 0)
            return days * 24 * 60;

        return -1;
    }
}
