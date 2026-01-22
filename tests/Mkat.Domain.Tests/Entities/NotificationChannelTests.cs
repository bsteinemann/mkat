using Mkat.Domain.Entities;

namespace Mkat.Domain.Tests.Entities;

public class NotificationChannelTests
{
    [Fact]
    public void NewNotificationChannel_HasEmptyType()
    {
        var channel = new NotificationChannel();
        Assert.Equal(string.Empty, channel.Type);
    }

    [Fact]
    public void NewNotificationChannel_HasDefaultConfigJson()
    {
        var channel = new NotificationChannel();
        Assert.Equal("{}", channel.ConfigJson);
    }

    [Fact]
    public void NewNotificationChannel_IsEnabled_ByDefault()
    {
        var channel = new NotificationChannel();
        Assert.True(channel.Enabled);
    }

    [Fact]
    public void NewNotificationChannel_HasCreatedAt_SetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var channel = new NotificationChannel();
        var after = DateTime.UtcNow;

        Assert.InRange(channel.CreatedAt, before, after);
    }

    [Fact]
    public void NewNotificationChannel_HasUpdatedAt_SetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var channel = new NotificationChannel();
        var after = DateTime.UtcNow;

        Assert.InRange(channel.UpdatedAt, before, after);
    }

    [Fact]
    public void NotificationChannel_CanSetProperties()
    {
        var id = Guid.NewGuid();
        var channel = new NotificationChannel
        {
            Id = id,
            Type = "telegram",
            ConfigJson = "{\"bot_token\":\"xxx\",\"chat_id\":\"123\"}",
            Enabled = false
        };

        Assert.Equal(id, channel.Id);
        Assert.Equal("telegram", channel.Type);
        Assert.Equal("{\"bot_token\":\"xxx\",\"chat_id\":\"123\"}", channel.ConfigJson);
        Assert.False(channel.Enabled);
    }
}
