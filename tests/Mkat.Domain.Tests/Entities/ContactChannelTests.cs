using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class ContactChannelTests
{
    [Fact]
    public void ContactChannel_HasRequiredProperties()
    {
        var contactId = Guid.NewGuid();
        var channel = new ContactChannel
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            Type = ChannelType.Telegram,
            Configuration = "{\"botToken\":\"123:ABC\",\"chatId\":\"456\"}",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        Assert.NotEqual(Guid.Empty, channel.Id);
        Assert.Equal(contactId, channel.ContactId);
        Assert.Equal(ChannelType.Telegram, channel.Type);
        Assert.Contains("botToken", channel.Configuration);
        Assert.True(channel.IsEnabled);
    }

    [Fact]
    public void ContactChannel_DefaultConfiguration_IsEmpty()
    {
        var channel = new ContactChannel();
        Assert.Equal(string.Empty, channel.Configuration);
    }

    [Fact]
    public void ContactChannel_DefaultIsEnabled_IsTrue()
    {
        var channel = new ContactChannel();
        Assert.True(channel.IsEnabled);
    }

    [Fact]
    public void ContactChannel_ContactNavigation_IsNull()
    {
        var channel = new ContactChannel();
        Assert.Null(channel.Contact);
    }

    [Fact]
    public void ChannelType_HasExpectedValues()
    {
        Assert.Equal(0, (int)ChannelType.Telegram);
        Assert.Equal(1, (int)ChannelType.Email);
    }
}
