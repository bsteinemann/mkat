using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Mkat.Infrastructure.Channels;

public class ContactChannelSender : IContactChannelSender
{
    private readonly ILogger<ContactChannelSender> _logger;

    public ContactChannelSender(ILogger<ContactChannelSender> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendAlertAsync(ContactChannel channel, Alert alert, Service service, CancellationToken ct = default)
    {
        return channel.Type switch
        {
            ChannelType.Telegram => await SendTelegramAsync(channel, alert, service, ct),
            _ => throw new NotSupportedException($"Channel type {channel.Type} is not supported")
        };
    }

    private async Task<bool> SendTelegramAsync(ContactChannel channel, Alert alert, Service service, CancellationToken ct)
    {
        TelegramConfig config;
        try
        {
            config = JsonSerializer.Deserialize<TelegramConfig>(channel.Configuration)
                ?? throw new InvalidOperationException("Failed to parse Telegram configuration");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid Telegram configuration for channel {ChannelId}", channel.Id);
            return false;
        }

        if (string.IsNullOrEmpty(config.BotToken) || string.IsNullOrEmpty(config.ChatId))
        {
            _logger.LogWarning("Telegram channel {ChannelId} missing botToken or chatId", channel.Id);
            return false;
        }

        try
        {
            var client = new TelegramBotClient(config.BotToken);
            var message = FormatAlertMessage(alert, service);

            await client.SendMessage(
                chatId: config.ChatId,
                text: message,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: ct);

            _logger.LogInformation(
                "Telegram alert sent via channel {ChannelId} for alert {AlertId}",
                channel.Id, alert.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram alert via channel {ChannelId}", channel.Id);
            return false;
        }
    }

    private static string FormatAlertMessage(Alert alert, Service service)
    {
        var stateText = alert.Type == AlertType.Recovery ? "RECOVERED" : "DOWN";
        var name = TelegramChannel.EscapeMarkdown(service.Name);
        var msg = TelegramChannel.EscapeMarkdown(alert.Message);
        var time = alert.CreatedAt.ToString("yyyy\\-MM\\-dd HH:mm:ss UTC");

        return $"*{stateText}*: {name}\n{msg}\n_{time}_";
    }

    private record TelegramConfig
    {
        public string BotToken { get; init; } = string.Empty;
        public string ChatId { get; init; } = string.Empty;
    }
}
