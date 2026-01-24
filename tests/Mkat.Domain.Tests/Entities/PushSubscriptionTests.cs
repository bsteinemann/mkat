using Mkat.Domain.Entities;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class PushSubscriptionTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var sub = new PushSubscription();

        Assert.NotEqual(Guid.Empty, sub.Id);
        Assert.True(sub.CreatedAtUtc <= DateTime.UtcNow);
        Assert.True(sub.CreatedAtUtc > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var sub = new PushSubscription
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/abc123",
            P256dhKey = "BNcRd...",
            AuthKey = "tBHI..."
        };

        Assert.Equal("https://fcm.googleapis.com/fcm/send/abc123", sub.Endpoint);
        Assert.Equal("BNcRd...", sub.P256dhKey);
        Assert.Equal("tBHI...", sub.AuthKey);
    }
}
