using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Mkat.Infrastructure.Channels;

public class TelegramChannel : INotificationChannel
{
    private readonly TelegramBotClient? _client;
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

    public async Task<bool> SendAlertAsync(Alert alert, Service service, CancellationToken ct = default)
    {
        if (!IsEnabled || _client == null) return false;

        var message = FormatAlertMessage(alert, service);
        var keyboard = CreateInlineKeyboard(alert, service);

        for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                await _client.SendMessage(
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
        if (!IsEnabled || _client == null) return false;

        try
        {
            var me = await _client.GetMe(ct);
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
            AlertType.Failure => "\u26a0\ufe0f",
            AlertType.MissedHeartbeat => "\U0001f494",
            AlertType.Recovery => "\u2705",
            _ => "\u2139\ufe0f"
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
        var name = EscapeMarkdown(service.Name);
        var msg = EscapeMarkdown(alert.Message);
        var time = alert.CreatedAt.ToString("yyyy\\-MM\\-dd HH:mm:ss UTC");

        return $"{emoji} *{stateText}*: {name}\n\n{severityText}\n{msg}\n\n_{time}_";
    }

    private static InlineKeyboardMarkup CreateInlineKeyboard(Alert alert, Service service)
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

    public static string EscapeMarkdown(string text)
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
