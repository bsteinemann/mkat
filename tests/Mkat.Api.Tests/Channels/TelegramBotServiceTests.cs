using Mkat.Infrastructure.Channels;
using Xunit;

namespace Mkat.Api.Tests.Channels;

public class TelegramBotServiceTests
{
    [Theory]
    [InlineData("15m", 15)]
    [InlineData("30m", 30)]
    [InlineData("1h", 60)]
    [InlineData("2h", 120)]
    [InlineData("24h", 1440)]
    [InlineData("1d", 1440)]
    [InlineData("7d", 10080)]
    public void ParseDuration_ValidInput_ReturnsMinutes(string input, int expectedMinutes)
    {
        var result = TelegramBotService.ParseDuration(input);

        Assert.Equal(expectedMinutes, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("m")]
    [InlineData("h")]
    [InlineData("123")]
    [InlineData("0m")]
    [InlineData("-5m")]
    public void ParseDuration_InvalidInput_ReturnsNegative(string input)
    {
        var result = TelegramBotService.ParseDuration(input);

        Assert.True(result <= 0);
    }

    [Theory]
    [InlineData("15M", 15)]
    [InlineData("1H", 60)]
    [InlineData("1D", 1440)]
    public void ParseDuration_CaseInsensitive(string input, int expectedMinutes)
    {
        var result = TelegramBotService.ParseDuration(input);

        Assert.Equal(expectedMinutes, result);
    }
}
