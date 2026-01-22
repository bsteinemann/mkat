using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Channels;
using Xunit;

namespace Mkat.Api.Tests.Channels;

public class TelegramChannelTests
{
    [Fact]
    public void IsEnabled_ReturnsFalse_WhenBotTokenEmpty()
    {
        var options = Options.Create(new TelegramOptions { BotToken = "", ChatId = "12345" });
        var logger = new Mock<ILogger<TelegramChannel>>();
        var channel = new TelegramChannel(options, logger.Object);

        Assert.False(channel.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenChatIdEmpty()
    {
        var options = Options.Create(new TelegramOptions { BotToken = "fake-token", ChatId = "" });
        var logger = new Mock<ILogger<TelegramChannel>>();
        var channel = new TelegramChannel(options, logger.Object);

        Assert.False(channel.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ReturnsTrue_WhenBothConfigured()
    {
        var options = Options.Create(new TelegramOptions { BotToken = "fake-token", ChatId = "12345" });
        var logger = new Mock<ILogger<TelegramChannel>>();
        var channel = new TelegramChannel(options, logger.Object);

        Assert.True(channel.IsEnabled);
    }

    [Fact]
    public void ChannelType_ReturnsTelegram()
    {
        var options = Options.Create(new TelegramOptions());
        var logger = new Mock<ILogger<TelegramChannel>>();
        var channel = new TelegramChannel(options, logger.Object);

        Assert.Equal("telegram", channel.ChannelType);
    }

    [Fact]
    public async Task SendAlertAsync_ReturnsFalse_WhenDisabled()
    {
        var options = Options.Create(new TelegramOptions { BotToken = "", ChatId = "" });
        var logger = new Mock<ILogger<TelegramChannel>>();
        var channel = new TelegramChannel(options, logger.Object);

        var alert = new Alert { Id = Guid.NewGuid(), Type = AlertType.Failure, Message = "test" };
        var service = new Service { Id = Guid.NewGuid(), Name = "Test" };

        var result = await channel.SendAlertAsync(alert, service);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_ReturnsFalse_WhenDisabled()
    {
        var options = Options.Create(new TelegramOptions { BotToken = "", ChatId = "" });
        var logger = new Mock<ILogger<TelegramChannel>>();
        var channel = new TelegramChannel(options, logger.Object);

        var result = await channel.ValidateConfigurationAsync();

        Assert.False(result);
    }

    [Theory]
    [InlineData("hello_world", "hello\\_world")]
    [InlineData("test.name", "test\\.name")]
    [InlineData("a*b*c", "a\\*b\\*c")]
    [InlineData("plain", "plain")]
    [InlineData("", "")]
    public void EscapeMarkdown_EscapesSpecialCharacters(string input, string expected)
    {
        var result = TelegramChannel.EscapeMarkdown(input);

        Assert.Equal(expected, result);
    }
}
